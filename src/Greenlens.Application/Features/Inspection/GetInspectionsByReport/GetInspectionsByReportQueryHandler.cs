using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Reports.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Inspection.GetInspectionsByReport;

public sealed class GetInspectionsByReportQueryHandler(
    IInspectionReportRepository inspections,
    IReportRepository reports,
    ITeamMemberRepository teamMembers,
    IUserRepository users,
    ICurrentUser currentUser,
    ILogger<GetInspectionsByReportQueryHandler> logger)
    : IRequestHandler<GetInspectionsByReportQuery, Result<GetInspectionsByReportResponse>>
{
    public async Task<Result<GetInspectionsByReportResponse>> Handle(
        GetInspectionsByReportQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting inspections for report {ReportId}", request.ReportId);

        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
        {
            logger.LogWarning("Report not found for report {ReportId}", request.ReportId);
            return Errors.Reports.ReportNotFound;
        }

        if (currentUser.Role == UserRole.LEO.ToString())
        {
            var user = await users.GetByIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
            if (user is null)
                return Errors.Users.UserNotFound;

            var scopeError = ReportReviewCandidateFilters.ValidateReportAccess(
                report, user, currentUser.Role);
            if (scopeError is not null)
                return scopeError;
        }

        var query = inspections.QueryAsNoTracking()
            .Include(ir => ir.CreatedByOfficer)
            .Include(ir => ir.ViolatingEntity)
            .Where(ir => ir.ReportId == request.ReportId);

        if (currentUser.Role == UserRole.Inspector.ToString())
        {
            var myTeamIds = await teamMembers.QueryAsNoTracking()
                .Where(tm => tm.UserId == currentUser.UserId)
                .Select(tm => tm.TeamId)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            if (myTeamIds.Count == 0)
                return new GetInspectionsByReportResponse([]);

            query = query.Where(ir =>
                ir.AssignedTeamId != null && myTeamIds.Contains(ir.AssignedTeamId.Value));
        }

        var items = await query
            .OrderByDescending(ir => ir.CreatedAt)
            .Select(ir => new InspectionSummaryDto(
                ir.Id,
                ir.Status,
                ir.ViolatorName,
                ir.ViolationLevel,
                ir.PenaltyAmount,
                ir.PaidAmount,
                ir.IsRepeatOffender,
                ir.ViolatingEntityId,
                ir.ViolatingEntity != null ? ir.ViolatingEntity.Name : null,
                ir.CreatedByOfficerId,
                ir.CreatedByOfficer!.FullName,
                ir.SlaInspectionDueAt,
                ir.ClosedAt,
                ir.CreatedAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        logger.LogInformation("Inspections by report: {Count} item(s)", items.Count);

        return new GetInspectionsByReportResponse(items);
    }
}
