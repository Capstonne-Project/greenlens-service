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
/// Return full report detail including satisfaction feedback.
/// </summary>
/// <remarks>
/// Implements: BR-REP-018 (satisfaction in response).
/// </remarks>
public sealed class GetReportByIdQueryHandler(
    IReportRepository reports,
    IReportSatisfactionRepository satisfactions,
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
        // Fetch reporter's satisfaction (there is at most one per report per user).
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

        logger.LogInformation("Lấy chi tiết báo cáo thành công. Mã báo cáo: {ReportCode}", r.Code);
        return new ReportDetailResponse(
            r.Id, r.Code, r.ReporterId,
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
            pendingReopen);
    }
}
