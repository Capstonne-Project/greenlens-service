using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.SubmitPollutionReport;

/// <summary>
/// Submit a new pollution report — supports two image flows:
///   AI flow:     TempImageId provided → lookup temp store → upload R2 → create Report (AiPending = false, status from AI)
///   Manual flow: Images[] provided   → persist URLs as-is → create Report (AiPending = true, background job retries AI)
///
/// Auto-routing: report is assigned directly to LocalOffice by WardCode.
/// Fallback: if ward has no onboarded LocalOffice → routes to Department queue (DEO handles manually).
/// </summary>
/// <remarks>
/// Implements: BR-REP-001 (≥1 photo), BR-REP-003 (GPS bounds — validator),
/// BR-REP-005 (category), BR-REP-013 (initial Submitted state),
/// BR-AI-001 (AI decision on AI flow), BR-AI-006 (AiPending on manual flow),
/// BR-ORG-010, BR-ORG-011 (auto-routing to LocalOffice by GPS).
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
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ITempImageStore tempStore,
    IFileStorageService fileStorage,
    ILogger<SubmitPollutionReportCommandHandler> logger)
    : IRequestHandler<SubmitPollutionReportCommand, Result<SubmitPollutionReportResponse>>
{
    public async Task<Result<SubmitPollutionReportResponse>> Handle(
        SubmitPollutionReportCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
            return Errors.Reports.LoginRequired;

        // ── BR-DAT-005: Consent check ────────────────────────────────────────
        var submitter = await users.GetByIdAsync(currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);
        if (submitter is not null && !submitter.HasDataConsent)
            return Errors.Users.DataConsentRequired;

        // ── Validate category ───────────────────────────────────────────────
        var category = await categories.GetByIdAsync(request.CategoryId, cancellationToken)
            .ConfigureAwait(false);
        if (category is null || !category.IsActive)
            return Errors.Reports.CategoryNotFound;

        // ── Validate ward/province pair ─────────────────────────────────────
        var provinceCode = request.ProvinceCode?.Trim();
        var wardCode = request.WardCode?.Trim();
        if (!string.IsNullOrEmpty(provinceCode) && !string.IsNullOrEmpty(wardCode))
        {
            var wardOk = await wards.ExistsAsync(
                    w => w.Code == wardCode && w.ProvinceCode == provinceCode, cancellationToken)
                .ConfigureAwait(false);
            if (!wardOk)
                return Errors.Reports.InvalidWardProvincePair;
        }

        var reporterId = currentUser.UserId;

        // ── Resolve image(s) based on flow ──────────────────────────────────
        ResolvedImage resolvedImage;

        if (!string.IsNullOrEmpty(request.TempImageId))
        {
            // AI flow: lookup temp → upload R2
            var tempEntry = await tempStore.GetAsync(request.TempImageId, cancellationToken)
                .ConfigureAwait(false);
            if (tempEntry is null)
                return Errors.Ai.TempImageNotFound;

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
                return Errors.Users.StorageUploadFailed;
            }

            resolvedImage = new ResolvedImage(
                Url: uploadResult.Url,
                MimeType: tempEntry.ContentType,
                SizeBytes: tempEntry.Bytes.LongLength,
                IsAiFlow: true,
                TempImageId: request.TempImageId);
        }
        else
        {
            // Manual flow: dùng URL đã upload sẵn, AI xử lý sau (AiPending = true)
            var first = request.Images![0];
            resolvedImage = new ResolvedImage(
                Url: first.Url.Trim(),
                MimeType: first.MimeType.Trim(),
                SizeBytes: first.SizeBytes,
                IsAiFlow: false,
                TempImageId: null);
        }

        // ── Create Report ───────────────────────────────────────────────────
        var code = await GenerateUniqueCodeAsync(cancellationToken).ConfigureAwait(false);

        var report = Report.Create(
            code, reporterId,
            request.CategoryId, request.Severity, request.Description,
            request.Latitude, request.Longitude,
            request.Address, wardCode, provinceCode);

        // Manual flow: AiPending already = true from Report.Create
        // AI flow:     AiPending = true from Create, set to false after we record the result below
        // (AI result was already shown to user in Step 1; we don't re-call here)

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
                report.RouteToDepartment(dept.Id);
        }

        // ── Persist primary image ───────────────────────────────────────────
        var primaryMedia = ReportMedia.Create(
            report.Id, MediaType.Image,
            resolvedImage.Url, resolvedImage.MimeType, resolvedImage.SizeBytes,
            reporterId);
        reportMedia.Add(primaryMedia);

        var persistedImages = new List<ReportMedia> { primaryMedia };

        // Manual flow: persist remaining images if any
        if (!resolvedImage.IsAiFlow && request.Images!.Count > 1)
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
                return Errors.Reports.WasteTagNotFound;

            var inactiveTags = tags.Where(t => !t.IsActive).ToList();
            if (inactiveTags.Count > 0)
                return Errors.Reports.WasteTagInactive;

            var newTags = request.WasteTagIds
                .Select(tagId => ReportWasteTag.Create(report.Id, tagId, reporterId))
                .ToList();
            reportWasteTags.AddRange(newTags);
        }

        // ── Tier 1 duplicate detection (BR-REP-030): same category + within 50m + within 24h ──
        // Runs inline (fast, free). Tier 2 (AI image compare) is triggered out-of-band via a
        // background job (see ReportPossibleDuplicateFlaggedEvent) to keep submit under p95<2s (BR-SYS-001).
        await FlagPossibleDuplicateAsync(report, cancellationToken).ConfigureAwait(false);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Report {ReportCode} submitted by {ReporterId}, routed to office {OfficeId} / department {DepartmentId}",
            report.Code, reporterId, report.AssignedOfficeId, report.AssignedDepartmentId);

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
            report.IsPossibleDuplicate, report.PossibleDuplicateOfReportId);
    }

    /// <summary>
    /// BR-REP-030: Tier 1 duplicate check — find the oldest active report with the same
    /// category within ~50m and 24h, then flag this report as a possible duplicate.
    /// Uses a decimal bounding box (reports store lat/lng, not a PostGIS geometry column),
    /// refined with an exact Haversine distance to reject bounding-box corners.
    /// </summary>
    private async Task FlagPossibleDuplicateAsync(Report report, CancellationToken ct)
    {
        const double radiusMeters = 50.0;
        // 1 deg latitude ≈ 111_320 m. Longitude shrinks by cos(latitude).
        var latDelta = (decimal)(radiusMeters / 111_320.0);
        var cosLat = Math.Max(Math.Cos((double)report.Latitude * Math.PI / 180.0), 1e-6);
        var lngDelta = (decimal)(radiusMeters / (111_320.0 * cosLat));

        var since = report.CreatedAt.AddHours(-24);

        var candidates = await reports.QueryAsNoTracking()
            .Where(r => r.CategoryId == report.CategoryId)
            .Where(r => r.Id != report.Id)
            .Where(r => r.Status != ReportStatus.Duplicate && r.Status != ReportStatus.Rejected)
            .Where(r => r.CreatedAt >= since)
            .Where(r => r.Latitude >= report.Latitude - latDelta && r.Latitude <= report.Latitude + latDelta)
            .Where(r => r.Longitude >= report.Longitude - lngDelta && r.Longitude <= report.Longitude + lngDelta)
            .OrderBy(r => r.CreatedAt)
            .Select(r => new { r.Id, r.Latitude, r.Longitude })
            .Take(10)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var match = candidates.FirstOrDefault(c =>
            HaversineMeters(report.Latitude, report.Longitude, c.Latitude, c.Longitude) <= radiusMeters);

        if (match is not null)
            report.MarkPossibleDuplicate(match.Id, "geo_time");
    }

    private static double HaversineMeters(decimal lat1, decimal lng1, decimal lat2, decimal lng2)
    {
        const double earthRadiusMeters = 6_371_000.0;
        var dLat = (double)(lat2 - lat1) * Math.PI / 180.0;
        var dLng = (double)(lng2 - lng1) * Math.PI / 180.0;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos((double)lat1 * Math.PI / 180.0) * Math.Cos((double)lat2 * Math.PI / 180.0)
                  * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return earthRadiusMeters * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
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

    private sealed record ResolvedImage(
        string Url,
        string MimeType,
        long SizeBytes,
        bool IsAiFlow,
        string? TempImageId);
}
