using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.GetReportById;

/// <summary>
/// Return full report detail including satisfaction, pending reopen request, and merged-duplicate thumbs.
/// </summary>
/// <remarks>
/// Implements: BR-REP-012 (ẩn danh người gửi), BR-REP-015 (pending reopen in response),
/// BR-REP-018 (satisfaction in response), BR-REP-032 (mergedReports + SourceReportId thumbs),
/// BR-AUTH-022 (reporter đã xóa tài khoản → ẩn danh tính).
/// </remarks>
public sealed class GetReportByIdQueryHandler(
    IReportRepository reports,
    IReportMediaRepository reportMedia,
    IReportSatisfactionRepository satisfactions,
    IInspectionReportRepository inspections,
    IApplicationDbContext db,
    ICurrentUser currentUser,
    ILogger<GetReportByIdQueryHandler> logger)
    : IRequestHandler<GetReportByIdQuery, Result<ReportDetailResponse>>
{
    public async Task<Result<ReportDetailResponse>> Handle(
        GetReportByIdQuery request, CancellationToken ct)
    {
        var r = await reports.QueryAsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Media)
            .Include(x => x.Assignments).ThenInclude(a => a.Team)
            .Include(x => x.WasteTags).ThenInclude(wt => wt.WasteTag)
            .Include(x => x.ParentReport)
            .Include(x => x.Reporter)
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct)
            .ConfigureAwait(false);

        if (r is null)
        {
            logger.LogWarning("Report not found for ID {ReportId}", request.Id);
            return Errors.Reports.ReportNotFound;
        }

        // BR-ADM-006: hidden reports are invisible to public
        if (r.IsHidden)
        {
            logger.LogWarning("Report {ReportId} is hidden", request.Id);
            return Errors.Reports.ReportNotFound;
        }

        var media = r.Media.Select(m => new ReportMediaItem(
            m.Id, m.Type.ToString(), m.Url, m.MimeType, m.SizeBytes)).ToList();

        var assignments = r.Assignments.Select(a => new ReportAssignmentItem(
            a.Id, a.TeamId, a.Team?.Name, a.Team?.TeamType.ToString() ?? "",
            a.Status.ToString(), a.Note, a.AssignedAt,
            a.StartedAt, a.CompletedAt,
            a.ProgressPercent, a.ProgressNote, a.ProgressUpdatedAt)).ToList();

        var wasteTagItems = r.WasteTags
            .Where(wt => wt.WasteTag is not null)
            .Select(wt => new ReportWasteTagItem(
                wt.WasteTagId, wt.WasteTag!.Code,
                wt.WasteTag.NameVi, wt.WasteTag.NameEn,
                wt.WasteTag.IconUrl))
            .ToList();

        // ── Satisfaction (BR-REP-018) ──
        var reporterSatisfaction = await satisfactions.QueryAsNoTracking()
            .Where(s => s.ReportId == request.Id && s.UserId == r.ReporterId)
            .Select(s => new ReportSatisfactionInfo(
                s.IsSatisfied, s.Rating, s.Comment, s.CreatedAt))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var hasCurrentUserRated = currentUser.IsAuthenticated
            && await satisfactions.ExistsAsync(
                s => s.ReportId == request.Id && s.UserId == currentUser.UserId, ct)
                .ConfigureAwait(false);

        PendingReopenRequestInfo? pendingReopen = null;
        if (r.HasPendingReopenRequest)
        {
            var pending = await db.Set<Domain.Entities.ReportReopenRequest>()
                .AsNoTracking()
                .Include(x => x.Media)
                .Where(x => x.ReportId == r.Id && x.Status == ReopenRequestStatus.Pending)
                .OrderByDescending(x => x.RequestedAt)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            if (pending is not null)
            {
                var evidence = pending.Media
                    .Select(m => new ReportMediaItem(
                        m.Id, m.Type.ToString(), m.Url, m.MimeType, m.SizeBytes))
                    .ToList();
                pendingReopen = new PendingReopenRequestInfo(
                    pending.Id, pending.Reason, pending.RequestedAt, evidence);
            }
        }

        // ── Merged duplicates (BR-REP-032) ──
        var children = await reports.QueryAsNoTracking()
            .Where(c => c.ParentReportId == r.Id)
            .Select(c => new { c.Id, c.Code, c.CreatedAt, c.Status })
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        IReadOnlyList<MergedReportItem> mergedReports = [];
        if (children.Count > 0)
        {
            var childIds = children.Select(c => c.Id).ToList();
            var thumbsBySource = await reportMedia.QueryAsNoTracking()
                .Where(m => m.ReportId == r.Id
                            && m.SourceReportId != null
                            && childIds.Contains(m.SourceReportId.Value))
                .OrderBy(m => m.UploadedAt)
                .Select(m => new { SourceId = m.SourceReportId!.Value, Url = m.ThumbnailUrl ?? m.Url })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var firstThumb = thumbsBySource
                .GroupBy(x => x.SourceId)
                .ToDictionary(g => g.Key, g => g.First().Url);

            mergedReports = children
                .Select(c => new MergedReportItem(
                    c.Id,
                    c.Code,
                    firstThumb.GetValueOrDefault(c.Id),
                    c.CreatedAt,
                    c.Status))
                .ToList();
        }

        Guid? mergedIntoPrimaryId = r.ParentReportId;
        string? mergedIntoPrimaryCode = r.ParentReport?.Code;

        // ── Reporter identity (BR-REP-012, BR-AUTH-022) ──
        // Ẩn danh hoặc đã xóa tài khoản → không lộ tên/avatar; FE hiển thị "Người dùng ẩn danh".
        var showReporter = !r.HideReporterName && r.Reporter is not null && !r.Reporter.IsBanned;
        var reporterName = showReporter ? r.Reporter!.FullName : null;
        var reporterAvatarUrl = showReporter ? r.Reporter!.AvatarUrl : null;
        PriorClosedReportSummary? priorClosed = null;
        if (r.IsSuspectedViolationRecurrence && r.SuspectedRecurrenceOfReportId.HasValue)
        {
            var priorId = r.SuspectedRecurrenceOfReportId.Value;
            var prior = await reports.QueryAsNoTracking()
                .Include(x => x.Category)
                .FirstOrDefaultAsync(x => x.Id == priorId, ct)
                .ConfigureAwait(false);

            if (prior is not null)
            {
                var priorInspections = await inspections.GetByReportIdAsync(priorId, ct).ConfigureAwait(false);
                priorClosed = new PriorClosedReportSummary(
                    prior.Id,
                    prior.Code,
                    prior.ClosedAt,
                    prior.Category.Code,
                    priorInspections.Count > 0);
            }
        }

        logger.LogInformation("Lấy chi tiết báo cáo thành công. Mã báo cáo: {ReportCode}", r.Code);
        return new ReportDetailResponse(
            r.Id, r.Code, r.ReporterId,
            reporterName, reporterAvatarUrl,
            r.CategoryId, r.Category.Code, r.Category.NameVi,
            r.Severity, r.SeveritySetBy, r.Status, r.Description,
            r.Latitude, r.Longitude, r.Address,
            r.WardCode, r.ProvinceCode,
            r.PriorityScore, r.ReporterCount, r.ReopenedCount,
            r.AiClassifiedType, r.AiConfidence,
            r.VerifiedBy, r.AssignedByOfficerId, r.AssignedOfficeId,
            media, assignments, wasteTagItems,
            r.AiSuggestedWasteTagCodes,
            r.CreatedAt, r.VerifiedAt, r.StartedAt,
            r.ResolvedAt, r.ClosedAt,
            r.SlaVerifyDueAt, r.SlaResolveDueAt,
            reporterSatisfaction,
            hasCurrentUserRated,
            r.HasPendingReopenRequest,
            pendingReopen,
            mergedIntoPrimaryId,
            mergedIntoPrimaryCode,
            mergedReports,
            r.IsSuspectedViolationRecurrence,
            r.SuspectedRecurrenceOfReportId,
            priorClosed);
    }
}
