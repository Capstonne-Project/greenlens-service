using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.SubmitPollutionReport;

/// <summary>
/// Submit a new pollution report — supports two image flows:
///   AI flow:     TempImageId provided → lookup temp store → upload R2 → create Report (AiPending = false, status from AI)
///   Manual flow: Images[] provided   → persist URLs as-is → create Report (AiPending = true, background job retries AI)
/// </summary>
/// <remarks>
/// Implements: BR-REP-001 (≥1 photo), BR-REP-003 (GPS bounds — validator),
/// BR-REP-005 (category), BR-REP-013 (initial Submitted state),
/// BR-AI-001 (AI decision on AI flow), BR-AI-006 (AiPending on manual flow),
/// BR-ORG-010, BR-ORG-011 (auto-routing).
/// </remarks>
public sealed class SubmitPollutionReportCommandHandler(
    IPollutionCategoryRepository categories,
    IReportRepository reports,
    IReportMediaRepository reportMedia,
    IReportStatusHistoryRepository statusHistory,
    IWardRepository wards,
    ILocalOfficeRepository localOffices,
    IDepartmentRepository departments,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ITempImageStore tempStore,
    IFileStorageService fileStorage)
    : IRequestHandler<SubmitPollutionReportCommand, Result<SubmitPollutionReportResponse>>
{
    public async Task<Result<SubmitPollutionReportResponse>> Handle(
        SubmitPollutionReportCommand request,
        CancellationToken cancellationToken)
    {
        if (!request.IsAnonymous && !currentUser.IsAuthenticated)
            return Errors.Reports.AuthenticationRequired;

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

        Guid? reporterId = request.IsAnonymous ? null : currentUser.UserId;

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
            code, reporterId, request.IsAnonymous,
            request.CategoryId, request.Severity, request.Description,
            request.Latitude, request.Longitude,
            request.Address, wardCode, provinceCode);

        // Manual flow: AiPending already = true from Report.Create
        // AI flow:     AiPending = true from Create, set to false after we record the result below
        // (AI result was already shown to user in Step 1; we don't re-call here)

        reports.Add(report);

        // ── Auto-routing: BR-ORG-010, BR-ORG-011 ───────────────────────────
        if (!string.IsNullOrEmpty(wardCode))
        {
            var officeExists = await localOffices.ExistsByWardCodeAsync(wardCode, cancellationToken)
                .ConfigureAwait(false);

            if (officeExists)
            {
                var office = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                    .FirstOrDefaultAsync(
                        localOffices.QueryAsNoTracking(),
                        o => o.WardCode == wardCode, cancellationToken)
                    .ConfigureAwait(false);

                if (office is not null) report.RouteToOffice(office.Id, office.OfficerId);
            }
            else if (!string.IsNullOrEmpty(provinceCode))
            {
                var deptExists = await departments.ExistsByProvinceCodeAsync(provinceCode, cancellationToken)
                    .ConfigureAwait(false);

                if (deptExists)
                {
                    var dept = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                        .FirstOrDefaultAsync(
                            departments.QueryAsNoTracking(),
                            d => d.ProvinceCode == provinceCode, cancellationToken)
                        .ConfigureAwait(false);

                    if (dept is not null) report.RouteToDepartmentQueue(dept.Id);
                }
            }
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

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

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
            report.IsAnonymous, report.ReporterId,
            report.Status, report.CreatedAt, report.SlaVerifyDueAt,
            report.AiPending, imageInfos);
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
