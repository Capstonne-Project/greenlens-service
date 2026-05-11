using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.SubmitPollutionReport;

/// <summary>
/// Submit a new pollution report from mobile or web client.
/// </summary>
/// <remarks>
/// Implements: BR-REP-001 (≥1 photo — validated in FluentValidation),
/// BR-REP-002 (max images per report — validator),
/// BR-REP-003 (GPS bounds — validated in FluentValidation),
/// BR-REP-005 (category required),
/// BR-REP-003 (administrative codes: ward must exist under selected province when both sent),
/// BR-REP-013 (initial Submitted state via <see cref="Report.Create"/>).
/// Rate limiting BR-REP-010 is enforced at API middleware / Redis (not in this handler).
/// </remarks>
public sealed class SubmitPollutionReportCommandHandler(
    IPollutionCategoryRepository categories,
    IReportRepository reports,
    IReportMediaRepository reportMedia,
    IReportStatusHistoryRepository statusHistory,
    IWardRepository wards,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : IRequestHandler<SubmitPollutionReportCommand, Result<SubmitPollutionReportResponse>>
{
    public async Task<Result<SubmitPollutionReportResponse>> Handle(
        SubmitPollutionReportCommand request,
        CancellationToken cancellationToken)
    {
        if (!request.IsAnonymous && !currentUser.IsAuthenticated)
            return Errors.Reports.AuthenticationRequired;

        var category = await categories.GetByIdAsync(request.CategoryId, cancellationToken)
            .ConfigureAwait(false);
        if (category is null || !category.IsActive)
            return Errors.Reports.CategoryNotFound;

        var provinceCode = request.ProvinceCode?.Trim();
        var wardCode = request.WardCode?.Trim();
        if (!string.IsNullOrEmpty(provinceCode) && !string.IsNullOrEmpty(wardCode))
        {
            var wardOk = await wards.ExistsAsync(
                    w => w.Code == wardCode && w.ProvinceCode == provinceCode,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!wardOk)
                return Errors.Reports.InvalidWardProvincePair;
        }

        Guid? reporterId = null;
        if (!request.IsAnonymous)
            reporterId = currentUser.UserId;

        var code = await GenerateUniqueCodeAsync(cancellationToken).ConfigureAwait(false);

        var report = Report.Create(
            code,
            reporterId,
            request.IsAnonymous,
            request.CategoryId,
            request.Severity,
            request.Description,
            request.Latitude,
            request.Longitude,
            request.Address,
            wardCode,
            provinceCode);

        reports.Add(report);

        var persistedImages = new List<ReportMedia>(request.Images.Count);
        foreach (var image in request.Images)
        {
            var media = ReportMedia.Create(
                report.Id,
                MediaType.Image,
                image.Url.Trim(),
                image.MimeType.Trim(),
                image.SizeBytes,
                reporterId);
            reportMedia.Add(media);
            persistedImages.Add(media);
        }

        var history = ReportStatusHistory.Create(
            report.Id,
            fromStatus: null,
            toStatus: ReportStatus.Submitted,
            changedBy: reporterId);

        statusHistory.Add(history);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var categoryInfo = new SubmitPollutionReportCategoryInfo(
            category.Id,
            category.Code,
            category.NameVi,
            category.NameEn,
            category.IconUrl);

        var imageInfos = persistedImages
            .Select(m => new SubmitPollutionReportImageInfo(m.Id, m.Url, m.MimeType, m.SizeBytes))
            .ToArray();

        return new SubmitPollutionReportResponse(
            report.Id,
            report.Code,
            categoryInfo,
            report.Severity,
            report.Description,
            report.Latitude,
            report.Longitude,
            report.Address,
            report.WardCode,
            report.ProvinceCode,
            report.IsAnonymous,
            report.ReporterId,
            report.Status,
            report.CreatedAt,
            report.SlaVerifyDueAt,
            report.AiPending,
            imageInfos);
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
}
