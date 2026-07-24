using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Analytics.GetCompanyOverview;

/// <summary>Overview KPIs scoped to the caller's company: task volume, SLA compliance, resolution time.</summary>
public sealed class GetCompanyOverviewQueryHandler(
    IReportRepository reports,
    ICompanyStaffRepository companyStaff,
    IEnvironmentalTeamRepository teams,
    ICurrentUser currentUser,
    IDateTimeProvider clock)
    : IRequestHandler<GetCompanyOverviewQuery, Result<CompanyOverviewResponse>>
{
    public async Task<Result<CompanyOverviewResponse>> Handle(
        GetCompanyOverviewQuery request, CancellationToken ct)
    {
        var companyIdResult = await CompanyContextResolver
            .ResolveCompanyIdAsync(companyStaff, currentUser.UserId, ct)
            .ConfigureAwait(false);
        if (companyIdResult.IsFailure)
            return companyIdResult.Error!;

        var companyId = companyIdResult.Value;
        var (from, to) = DateRangeDefaults.Resolve(request.From, request.To, clock.UtcNow);

        var dispatched = await reports.QueryAsNoTracking()
            .Where(r => r.AssignedCompanyId == companyId
                        && r.DispatchedToCompanyAt >= from && r.DispatchedToCompanyAt <= to)
            .Select(r => new
            {
                r.Status,
                r.VerifiedAt,
                r.ResolvedAt,
                r.SlaResolveBreached
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var assignedTasks = dispatched.Count;
        var completedTasks = dispatched.Count(r => r.Status is ReportStatus.Resolved or ReportStatus.Closed);
        var pendingTasks = assignedTasks - completedTasks;

        var activeTeams = await teams.QueryAsNoTracking()
            .CountAsync(t => t.CompanyId == companyId && t.IsActive, ct)
            .ConfigureAwait(false);
        var activeStaff = await companyStaff.QueryAsNoTracking()
            .CountAsync(s => s.CompanyId == companyId && s.IsActive, ct)
            .ConfigureAwait(false);

        var slaComplianceRate = assignedTasks == 0
            ? 100m
            : Math.Round(100m * dispatched.Count(r => !r.SlaResolveBreached) / assignedTasks, 1);

        var resolutionHoursSamples = dispatched
            .Where(r => r.Status is ReportStatus.Resolved or ReportStatus.Closed
                        && r.VerifiedAt.HasValue && r.ResolvedAt.HasValue)
            .Select(r => (decimal)(r.ResolvedAt!.Value - r.VerifiedAt!.Value).TotalHours)
            .ToList();

        var averageResolutionHours = resolutionHoursSamples.Count == 0
            ? 0m
            : Math.Round(resolutionHoursSamples.Average(), 1);

        return new CompanyOverviewResponse(
            assignedTasks,
            completedTasks,
            pendingTasks,
            activeTeams,
            activeStaff,
            slaComplianceRate,
            averageResolutionHours);
    }
}
