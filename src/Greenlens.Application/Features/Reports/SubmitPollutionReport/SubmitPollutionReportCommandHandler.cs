using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Mappings;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Greenlens.Application.Features.Reports.SubmitPollutionReport;

/// <summary>
/// Submit a new pollution report — supports two image flows:
///   AI flow:     TempImageId provided → apply the pre-submit classification result.
///   Manual flow: Images[] provided   → persist URLs without post-submit classification.
///
/// Auto-routing: report is assigned directly to LocalOffice by WardCode.
/// Fallback: if ward has no onboarded LocalOffice → routes to Department queue (DEO handles manually).
/// </summary>
/// <remarks>
/// Implements: BR-REP-001 (≥1 photo), BR-REP-003 (GPS bounds — validator),
/// BR-REP-004 (description length + profanity), BR-REP-005 (category),
/// BR-REP-010 (submit rate limit), BR-REP-011 (EXIF GPS quality),
/// BR-REP-013 (initial Submitted state),
/// BR-AI-001 (optional pre-submit AI decision),
/// BR-ORG-004, BR-ORG-010, BR-ORG-011, BR-ORG-016 (WardCode xác định bằng
/// point-in-polygon từ GPS, không phụ thuộc client gửi WardCode/ProvinceCode).
/// </remarks>
public sealed class SubmitPollutionReportCommandHandler(
    IPollutionCategoryRepository categories,
    IReportRepository reports,
    IReportMediaRepository reportMedia,
    IReportStatusHistoryRepository statusHistory,
    IWasteTagRepository wasteTags,
    IReportWasteTagRepository reportWasteTags,
    IUserRepository users,
    IWardRepository wards,
    IDepartmentRepository departments,
    ILocalOfficeRepository localOffices,
    IWardBoundaryLookupService wardBoundaryLookup,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ITempImageStore tempStore,
    IFileStorageService fileStorage,
    IProfanityFilter profanityFilter,
    IReportSubmissionRateLimiter rateLimiter,
    IIdempotencyContext idempotencyContext,
    IImageExifAnalyzer exifAnalyzer,
    IImageBytesFetcher imageBytesFetcher,
    ISystemSettingsProvider systemSettings,
    ILogger<SubmitPollutionReportCommandHandler> logger)
    : IRequestHandler<SubmitPollutionReportCommand, Result<SubmitPollutionReportResponse>>
{
    public async Task<Result<SubmitPollutionReportResponse>> Handle(
        SubmitPollutionReportCommand request,
        CancellationToken cancellationToken)
    {
        var swTotal = System.Diagnostics.Stopwatch.StartNew();

        if (!currentUser.IsAuthenticated)
        {
            logger.LogWarning("User is not authenticated");
            return Errors.Reports.LoginRequired;
        }

        if (request.Images is { Count: > 0 })
        {
            foreach (var image in request.Images)
            {
                var owned = string.IsNullOrWhiteSpace(image.Key)
                    ? fileStorage.IsOwnedPublicUrl(image.Url)
                    : fileStorage.IsOwnedPublicUrl(image.Url, image.Key);
                if (!owned)
                {
                    logger.LogWarning("Invalid storage URL for image {Url}", image.Url);
                    return Errors.Media.InvalidStorageUrl;
                }
            }
        }

        // ── BR-REP-010: sliding-window submit quota (5/h, 20/24h) ─────────
        // Kiểm tra nếu không phải là replay
        if (!idempotencyContext.IsReplay)
        // Kiểm tra nếu rate limit vượt quá
        {
            var rateLimit = await rateLimiter.TryAcquireAsync(currentUser.UserId, cancellationToken)
                .ConfigureAwait(false);
            // Kiểm tra nếu rate limit vượt quá
            if (!rateLimit.IsAllowed)
            {
                logger.LogWarning("Rate limit exceeded for user {UserId}", currentUser.UserId);
                return Errors.Reports.RateLimitExceeded(rateLimit.RetryAfterMinutes);
            }
        }

        // ── BR-REP-004: profanity filter when description provided ─────────
        if (!string.IsNullOrWhiteSpace(request.Description)
            && profanityFilter.ContainsProfanity(request.Description))
        {
            logger.LogWarning("Inappropriate description for report {Description}", request.Description);
            return Errors.Reports.InappropriateDescription;
        }

        // ── BR-DAT-005: Consent check ────────────────────────────────────────
        var submitter = await users.GetByIdAsync(currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);
        if (submitter is not null && !submitter.HasDataConsent)
        {
            logger.LogWarning("User {UserId} does not have data consent", currentUser.UserId);
            return Errors.Users.DataConsentRequired;
        }

        // ── Validate category ───────────────────────────────────────────────
        var category = await categories.GetByIdAsync(request.CategoryId, cancellationToken)
            .ConfigureAwait(false);
        if (category is null || !category.IsActive)
        {
            logger.LogWarning("Category not found for ID {CategoryId}", request.CategoryId);
            return Errors.Reports.CategoryNotFound;
        }

        // ── BR-ORG-004, BR-ORG-010, BR-ORG-016: xác định WardCode bằng point-in-polygon từ GPS ──
        // GPS đã qua validator (BR-REP-003, khung lãnh thổ VN) nên luôn dùng làm nguồn xác định
        // WardCode/ProvinceCode — không tin WardCode/ProvinceCode do client tự gửi (legacy field,
        // vẫn được validator chấp nhận nhưng không còn quyết định business logic).
        string? provinceCode = null;
        string? wardCode = null;

        var detectedWardCode = await wardBoundaryLookup
            .FindWardCodeByPointAsync(request.Latitude, request.Longitude, cancellationToken)
            .ConfigureAwait(false);

        if (detectedWardCode is not null)
        {
            var detectedWard = await wards.GetByCodeAsync(detectedWardCode, cancellationToken)
                .ConfigureAwait(false);
            if (detectedWard is not null)
            {
                wardCode = detectedWard.Code;
                provinceCode = detectedWard.ProvinceCode;
            }
        }

        if (wardCode is null)
        {
            provinceCode = await wardBoundaryLookup
                .FindProvinceCodeByPointAsync(request.Latitude, request.Longitude, cancellationToken)
                .ConfigureAwait(false);

            if (provinceCode is not null)
            {
                logger.LogInformation(
                    "No ward boundary contains point ({Lat}, {Lng}) — province {ProvinceCode} detected for Department queue (BR-ORG-011)",
                    request.Latitude, request.Longitude, provinceCode);
            }
            else
            {
                logger.LogInformation(
                    "No ward or province boundary contains point ({Lat}, {Lng}) — report cannot be auto-routed (BR-ORG-011)",
                    request.Latitude, request.Longitude);
            }
        }

        var reporterId = currentUser.UserId;

        // ── Resolve image(s) based on flow ──────────────────────────────────
        ResolvedImage resolvedImage;

        if (!string.IsNullOrEmpty(request.TempImageId))
        {
            // AI flow: lookup analysis. New direct-R2 flow also supplies Images;
            // legacy multipart flow only supplies TempImageId and still uploads here.
            var tempEntry = await tempStore.GetAsync(request.TempImageId, cancellationToken)
                .ConfigureAwait(false);
            if (tempEntry is null)
            {
                logger.LogWarning("Temp image not found for ID {TempImageId}", request.TempImageId);
                return Errors.Ai.TempImageNotFound;
            }

            if (tempEntry.AiResult?.Decision == AiDecision.IrrelevantOrSuspectedAbusive)
            {
                logger.LogWarning("Image rejected by AI for report {TempImageId}", request.TempImageId);
                return Errors.Ai.ImageRejectedByAi;
            }

            if (request.Images is { Count: > 0 })
            {
                var first = request.Images[0];
                if (tempEntry.PublicUrl is not null
                    && !string.Equals(
                        tempEntry.PublicUrl,
                        first.Url.Trim(),
                        StringComparison.Ordinal))
                    {
                        logger.LogWarning(
                            "Upload metadata mismatch (URL) for tempImageId={TempImageId} | tempUrl={TempUrl} submitUrl={SubmitUrl}",
                            request.TempImageId, tempEntry.PublicUrl, first.Url.Trim());
                        return Errors.Media.UploadMetadataMismatch;
                    }
                if (tempEntry.StorageKey is not null
                    && !string.Equals(
                        tempEntry.StorageKey,
                        first.Key?.Trim(),
                        StringComparison.Ordinal))
                    {
                        logger.LogWarning(
                            "Upload metadata mismatch (Key) for tempImageId={TempImageId} | tempKey={TempKey} submitKey={SubmitKey}",
                            request.TempImageId, tempEntry.StorageKey, first.Key?.Trim());
                        return Errors.Media.UploadMetadataMismatch;
                    }

                resolvedImage = new ResolvedImage(
                    Url: first.Url.Trim(),
                    MimeType: first.MimeType.Trim(),
                    SizeBytes: first.SizeBytes,
                    IsAiFlow: true,
                    TempImageId: request.TempImageId,
                    ImageBytes: tempEntry.Bytes,
                    AiResult: tempEntry.AiResult);
            }
            else
            {
                FileUploadResult uploadResult;
                try
                {
                    using var stream = new MemoryStream(tempEntry.Bytes);
                    uploadResult = await fileStorage.UploadAsync(
                        stream, tempEntry.FileName, tempEntry.ContentType,
                        "reports/images", cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    logger.LogWarning("Storage upload failed for image {Url}", tempEntry.FileName);
                    return Errors.Users.StorageUploadFailed;
                }

                resolvedImage = new ResolvedImage(
                    Url: uploadResult.Url,
                    MimeType: tempEntry.ContentType,
                    SizeBytes: tempEntry.Bytes.LongLength,
                    IsAiFlow: true,
                    TempImageId: request.TempImageId,
                    ImageBytes: tempEntry.Bytes,
                    AiResult: tempEntry.AiResult);
            }
        }
        else
        {
            // Manual flow persists the uploaded image without scheduling AI classification.
            var first = request.Images![0];
            byte[]? manualBytes;
            var swDownload = System.Diagnostics.Stopwatch.StartNew();
            if (!string.IsNullOrWhiteSpace(first.Key))
            {
                var stored = await fileStorage.DownloadAsync(
                        first.Key.Trim(),
                        10 * 1024 * 1024,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (stored is null || !IsSizeWithinTolerance(stored.SizeBytes, first.SizeBytes))
                {
                    logger.LogWarning(
                        "Upload metadata mismatch for image {Url} (stored={StoredSize}, declared={DeclaredSize})",
                        first.Url, stored?.SizeBytes, first.SizeBytes);
                    return Errors.Media.UploadMetadataMismatch;
                }
                manualBytes = stored.Bytes;
            }
            else
            {
                manualBytes = await imageBytesFetcher
                    .TryFetchAsync(first.Url.Trim(), cancellationToken)
                    .ConfigureAwait(false);
            }
            swDownload.Stop();
            logger.LogWarning(
                "[TIMING] Re-download image from storage took {ElapsedMs}ms (size={Size} bytes)",
                swDownload.ElapsedMilliseconds, manualBytes?.LongLength ?? 0);
            resolvedImage = new ResolvedImage(
                Url: first.Url.Trim(),
                MimeType: first.MimeType.Trim(),
                SizeBytes: first.SizeBytes,
                IsAiFlow: false,
                TempImageId: null,
                ImageBytes: manualBytes,
                AiResult: null);
        }

        // ── Create Report ───────────────────────────────────────────────────
        logger.LogInformation("Creating report for user {UserId}", currentUser.UserId);
        var code = await GenerateUniqueCodeAsync(cancellationToken).ConfigureAwait(false);

        var report = Report.Create(
            code, reporterId,
            request.CategoryId, request.Severity, request.Description,
            request.Latitude, request.Longitude,
            request.Address, wardCode, provinceCode,
            request.HideReporterName,
            ModuleSystemSettings.ReportSla(systemSettings));

        if (resolvedImage.AiResult is { } analyzed)
        {
            report.ApplyAiResults(
                analyzed.Classify.PrimaryClass ?? "unknown",
                (decimal)analyzed.Classify.Confidence,
                AiSeverityMapper.Parse(analyzed.Classify.Severity));
        }

        logger.LogInformation("Adding report {ReportCode} to database", report.Code);
        reports.Add(report);

        // ── Auto-routing: report goes directly to LocalOffice by WardCode ──
        if (!string.IsNullOrEmpty(wardCode))
        {
            var office = await localOffices.QueryAsNoTracking()
                .FirstOrDefaultAsync(
                    o => o.WardCode == wardCode && o.IsOnboarded, cancellationToken)
                .ConfigureAwait(false);

            if (office is not null)
            {
                logger.LogInformation("Routing report {ReportCode} to LocalOffice {OfficeId}", report.Code, office.Id);
                report.RouteToLocalOffice(office.Id, office.DepartmentId);
            }
        }

        // Fallback: route to Department queue if no LocalOffice matched
        if (report.AssignedOfficeId is null && !string.IsNullOrEmpty(provinceCode))
        {
            var dept = await departments.QueryAsNoTracking()
                .FirstOrDefaultAsync(
                    d => d.ProvinceCode == provinceCode, cancellationToken)
                .ConfigureAwait(false);

            if (dept is not null)
            {
                logger.LogInformation("Routing report {ReportCode} to Department {DepartmentId}", report.Code, dept.Id);
                report.RouteToDepartment(dept.Id);
            }
        }

        // ── Persist primary image ───────────────────────────────────────────
        var primaryMedia = ReportMedia.Create(
            report.Id, MediaType.Image,
            resolvedImage.Url, resolvedImage.MimeType, resolvedImage.SizeBytes,
            reporterId);
        reportMedia.Add(primaryMedia);

        // ── BR-REP-011: EXIF GPS quality check on primary image ─────────────
        string? exifWarning = null;
        if (resolvedImage.ImageBytes is { Length: > 0 } bytes)
        {
            var swExif = System.Diagnostics.Stopwatch.StartNew();
            var exif = exifAnalyzer.Analyze(
                bytes,
                request.Latitude,
                request.Longitude);
            swExif.Stop();
            logger.LogWarning("[TIMING] EXIF analyze took {ElapsedMs}ms", swExif.ElapsedMilliseconds);

            if (!string.IsNullOrEmpty(exif.ExifJson))
                primaryMedia.SetExifData(exif.ExifJson);

            if (exif.IsSuspicious)
            {
                report.FlagSuspicious(JsonSerializer.Serialize(exif.SuspiciousReasons));
                exifWarning = ExifSuspicionEvaluator.StaleWarningMessage;
                logger.LogWarning(
                    "Report {ReportCode} flagged suspicious: {Reasons}",
                    report.Code, string.Join(", ", exif.SuspiciousReasons));
            }
        }

        var persistedImages = new List<ReportMedia> { primaryMedia };

        // Direct-R2 AI and manual flows may both carry additional images.
        if (request.Images is { Count: > 1 })
        {
            foreach (var img in request.Images.Skip(1))
            {
                var media = ReportMedia.Create(
                    report.Id, MediaType.Image,
                    img.Url.Trim(), img.MimeType.Trim(), img.SizeBytes,
                    reporterId);
                reportMedia.Add(media);
                persistedImages.Add(media);
            }
        }

        var history = ReportStatusHistory.Create(
            report.Id, fromStatus: null,
            toStatus: ReportStatus.Submitted, changedBy: reporterId);
        statusHistory.Add(history);

        // ── Optional waste tags ───────────────────────────────────────────────
        if (request.WasteTagIds is { Count: > 0 })
        {
            var tags = await wasteTags.GetByIdsAsync(request.WasteTagIds.ToList(), cancellationToken)
                .ConfigureAwait(false);

            if (tags.Count != request.WasteTagIds.Count)
            {
                logger.LogWarning("Waste tag not found for IDs {WasteTagIds}", request.WasteTagIds);
                return Errors.Reports.WasteTagNotFound;
            }

            var inactiveTags = tags.Where(t => !t.IsActive).ToList();
            if (inactiveTags.Count > 0)
            {
                logger.LogWarning("Waste tag inactive for IDs {WasteTagIds}", request.WasteTagIds);
                return Errors.Reports.WasteTagInactive;
            }

            var newTags = request.WasteTagIds
                .Select(tagId => ReportWasteTag.Create(report.Id, tagId, reporterId))
                .ToList();
            reportWasteTags.AddRange(newTags);
        }

        // ── Tier 1 duplicate detection (BR-REP-030): same category + within 25m ──
        // Runs inline (fast, free). Tier 2 (AI image compare) is triggered out-of-band via a
        // background job (see ReportPossibleDuplicateFlaggedEvent) to keep submit under p95<2s (BR-SYS-001).
        await FlagPossibleDuplicateAsync(report, cancellationToken).ConfigureAwait(false);

        // ── BR-REP-034: suspected violation recurrence — mutually exclusive with duplicate flag ──
        if (!report.IsPossibleDuplicate)
            await FlagSuspectedViolationRecurrenceAsync(report, cancellationToken).ConfigureAwait(false);

        report.RaiseSubmittedForVerification();

        var swSave = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex)
        {
            var mapped = PostgresUniqueViolationMapper.TryMap(ex);
            if (mapped is not null)
                return mapped;
            throw;
        }
        swSave.Stop();
        logger.LogWarning("[TIMING] SaveChangesAsync took {ElapsedMs}ms", swSave.ElapsedMilliseconds);

        logger.LogInformation(
            "Report {ReportCode} submitted by {ReporterId}, routed to office {OfficeId} / department {DepartmentId}",
            report.Code, reporterId, report.AssignedOfficeId, report.AssignedDepartmentId);

        swTotal.Stop();
        logger.LogWarning("[TIMING] Total SubmitPollutionReport handler took {ElapsedMs}ms", swTotal.ElapsedMilliseconds);

        // ── Cleanup temp after successful save (AI flow only) ───────────────
        if (resolvedImage.IsAiFlow)
            await tempStore.DeleteAsync(resolvedImage.TempImageId!, cancellationToken).ConfigureAwait(false);

        // ── Build response ──────────────────────────────────────────────────
        var categoryInfo = new SubmitPollutionReportCategoryInfo(
            category.Id, category.Code, category.NameVi, category.NameEn, category.IconUrl);

        var imageInfos = persistedImages
            .Select(m => new SubmitPollutionReportImageInfo(m.Id, m.Url, m.MimeType, m.SizeBytes))
            .ToArray();

        return new SubmitPollutionReportResponse(
            report.Id, report.Code, categoryInfo,
            report.Severity, report.Description,
            report.Latitude, report.Longitude,
            report.Address, report.WardCode, report.ProvinceCode,
            reporterId,
            report.Status, report.CreatedAt, report.SlaVerifyDueAt,
            report.AiPending, imageInfos,
            report.IsPossibleDuplicate, report.PossibleDuplicateOfReportId,
            report.IsSuspectedViolationRecurrence, report.SuspectedRecurrenceOfReportId,
            report.IsSuspicious, exifWarning);
    }

    /// <summary>
    /// BR-REP-030: Tier 1 duplicate check — find the canonical primary (Verified/InProgress first,
    /// else oldest) with the same category, ward, province, and within ~25m, then flag this report
    /// as a possible duplicate.
    /// Closed reports (BR-REP-016 auto-close) are excluded — new submissions at the same spot
    /// start a fresh report, not a duplicate of a finished case.
    /// Each flagged report gets its own Tier 2 AI job vs that primary.
    /// </summary>
    private async Task FlagPossibleDuplicateAsync(Report report, CancellationToken ct)
    {
        if (!AdministrativeUnitMatch.HasWardAndProvince(report.WardCode, report.ProvinceCode))
            return;

        var radiusMeters = ReportSystemSettings.DuplicateRadiusMeters(systemSettings);
        var maxCandidates = ReportSystemSettings.DuplicateMaxCandidates(systemSettings);
        var timeWindowHours = ReportSystemSettings.DuplicateTimeWindowHours(systemSettings);
        var latDelta = (decimal)(radiusMeters / 111_320.0);
        var cosLat = Math.Max(Math.Cos((double)report.Latitude * Math.PI / 180.0), 1e-6);
        var lngDelta = (decimal)(radiusMeters / (111_320.0 * cosLat));

        var cutoff = timeWindowHours > 0
            ? DateTime.UtcNow.AddHours(-timeWindowHours)
            : (DateTime?)null;

        var candidateQuery = reports.QueryAsNoTracking()
            .Where(r => r.CategoryId == report.CategoryId)
            .Where(r => r.WardCode == report.WardCode && r.ProvinceCode == report.ProvinceCode)
            .Where(r => r.Id != report.Id)
            .Where(r => r.Status != ReportStatus.Duplicate
                     && r.Status != ReportStatus.Rejected
                     && r.Status != ReportStatus.Closed)
            .Where(r => r.Latitude >= report.Latitude - latDelta && r.Latitude <= report.Latitude + latDelta)
            .Where(r => r.Longitude >= report.Longitude - lngDelta && r.Longitude <= report.Longitude + lngDelta);

        if (cutoff.HasValue)
            candidateQuery = candidateQuery.Where(r => r.CreatedAt >= cutoff.Value);

        var candidates = await candidateQuery
            .OrderByDescending(r =>
                r.Status == ReportStatus.Verified
                || r.Status == ReportStatus.InProgress
                || r.Status == ReportStatus.Reopened)
            .ThenBy(r => r.CreatedAt)
            .Select(r => new DuplicateNearbyReport(
                r.Id, r.Latitude, r.Longitude, r.WardCode, r.ProvinceCode, r.Status, r.CreatedAt))
            .Take(maxCandidates)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var primaryId = DuplicateTier1PrimarySelector.SelectPrimary(
            report.Latitude, report.Longitude, report.WardCode, report.ProvinceCode, candidates, radiusMeters);

        if (primaryId is not null)
            report.MarkPossibleDuplicate(primaryId.Value, DuplicateTier1PrimarySelector.Tier1Source);
    }

    /// <summary>
    /// BR-REP-034: flag when a recently Closed report (≤30 days) exists at the same spot, ward,
    /// province, and category.
    /// Skipped when duplicate is flagged or when another report is InProgress nearby (cleanup underway).
    /// </summary>
    private async Task FlagSuspectedViolationRecurrenceAsync(Report report, CancellationToken ct)
    {
        if (!AdministrativeUnitMatch.HasWardAndProvince(report.WardCode, report.ProvinceCode))
            return;

        var radiusMeters = ReportSystemSettings.RecurrenceRadiusMeters(systemSettings);
        var maxCandidates = ReportSystemSettings.DuplicateMaxCandidates(systemSettings);
        var minDaysAfterClose = ReportSystemSettings.RecurrenceMinDaysAfterClose(systemSettings);
        var maxDaysAfterClose = ReportSystemSettings.RecurrenceMaxDaysAfterClose(systemSettings);
        var latDelta = (decimal)(radiusMeters / 111_320.0);
        var cosLat = Math.Max(Math.Cos((double)report.Latitude * Math.PI / 180.0), 1e-6);
        var lngDelta = (decimal)(radiusMeters / (111_320.0 * cosLat));
        var now = DateTime.UtcNow;
        var oldestClosed = now.AddDays(-maxDaysAfterClose);
        var newestClosed = minDaysAfterClose > 0 ? now.AddDays(-minDaysAfterClose) : now;

        if (await HasActiveCleanupNearbyAsync(report, latDelta, lngDelta, radiusMeters, ct).ConfigureAwait(false))
            return;

        var candidates = await reports.QueryAsNoTracking()
            .Where(r => r.CategoryId == report.CategoryId)
            .Where(r => r.WardCode == report.WardCode && r.ProvinceCode == report.ProvinceCode)
            .Where(r => r.Id != report.Id)
            .Where(r => r.Status == ReportStatus.Closed)
            .Where(r => r.ClosedAt >= oldestClosed && r.ClosedAt <= newestClosed)
            .Where(r => r.Latitude >= report.Latitude - latDelta && r.Latitude <= report.Latitude + latDelta)
            .Where(r => r.Longitude >= report.Longitude - lngDelta && r.Longitude <= report.Longitude + lngDelta)
            .OrderByDescending(r => r.ClosedAt)
            .Select(r => new ViolationRecurrenceNearbyReport(
                r.Id, r.Latitude, r.Longitude, r.WardCode, r.ProvinceCode, r.ClosedAt!.Value))
            .Take(maxCandidates)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var priorId = ViolationRecurrencePrimarySelector.SelectPrimary(
            report.Latitude, report.Longitude, report.WardCode, report.ProvinceCode, candidates, radiusMeters);

        if (priorId is not null)
            report.MarkSuspectedViolationRecurrence(priorId.Value);
    }

    /// <summary>
    /// True when a report is actively being handled or cleaned (Verified / InProgress / Reopened)
    /// within the recurrence radius and same category.
    /// </summary>
    /// <remarks>
    /// Status list inlined (not calling <see cref="ViolationRecurrencePrimarySelector.BlocksRecurrenceDetection"/>)
    /// because EF Core cannot translate an arbitrary static method call to SQL. Keep both in sync.
    /// </remarks>
    private async Task<bool> HasActiveCleanupNearbyAsync(
        Report report,
        decimal latDelta,
        decimal lngDelta,
        double radiusMeters,
        CancellationToken ct)
    {
        var nearby = await reports.QueryAsNoTracking()
            .Where(r => r.CategoryId == report.CategoryId)
            .Where(r => r.WardCode == report.WardCode && r.ProvinceCode == report.ProvinceCode)
            .Where(r => r.Id != report.Id)
            .Where(r => r.Status == ReportStatus.Verified || r.Status == ReportStatus.InProgress || r.Status == ReportStatus.Reopened)
            .Where(r => r.Latitude >= report.Latitude - latDelta && r.Latitude <= report.Latitude + latDelta)
            .Where(r => r.Longitude >= report.Longitude - lngDelta && r.Longitude <= report.Longitude + lngDelta)
            .Select(r => new { r.Latitude, r.Longitude })
            .Take(20)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return nearby.Any(n =>
            GeoMath.HaversineMeters(report.Latitude, report.Longitude, n.Latitude, n.Longitude) <= radiusMeters);
    }

    private async Task<string> GenerateUniqueCodeAsync(CancellationToken ct)
    {
        string code;
        var attempts = 0;
        do
        {
            var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            code = $"RPT-{DateTime.UtcNow:yyMMdd}-{suffix}";
            attempts++;
        } while (attempts < 12 &&
                 await reports.ExistsAsync(r => r.Code == code, ct).ConfigureAwait(false));
        return code;
    }

    /// <summary>
    /// So sánh size ảnh client khai (đo trước khi PUT) với size thật trên storage.
    /// Cho phép sai số nhỏ (1% hoặc tối thiểu 4KB) — JPEG re-encode giữa các bước xử lý ảnh trên
    /// mobile (Hermes Blob fallback, nén lại khi retry) có thể lệch vài byte dù cùng nội dung ảnh.
    /// </summary>
    private static bool IsSizeWithinTolerance(long storedSizeBytes, long declaredSizeBytes)
    {
        const long minToleranceBytes = 4 * 1024;
        var tolerance = Math.Max(minToleranceBytes, declaredSizeBytes / 100);
        return Math.Abs(storedSizeBytes - declaredSizeBytes) <= tolerance;
    }

    private sealed record ResolvedImage(
        string Url,
        string MimeType,
        long SizeBytes,
        bool IsAiFlow,
        string? TempImageId,
        byte[]? ImageBytes,
        AiClassificationResult? AiResult);
}
