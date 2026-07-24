using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Analytics.GetCompanyTeamPerformance;

/// <summary>Per-team KPIs (assigned/completed/completion rate/avg time) for the caller's company.</summary>
public sealed class GetCompanyTeamPerformanceQueryHandler(
    IReportAssignmentRepository assignments,
    IEnvironmentalTeamRepository teams,
    ICompanyStaffRepository companyStaff,
    ICurrentUser currentUser,
    IDateTimeProvider clock)
    : IRequestHandler<GetCompanyTeamPerformanceQuery, Result<List<TeamPerformanceItem>>>
{
    public async Task<Result<List<TeamPerformanceItem>>> Handle(
        GetCompanyTeamPerformanceQuery request, CancellationToken ct)
    {
        var companyIdResult = await CompanyContextResolver
            .ResolveCompanyIdAsync(companyStaff, currentUser.UserId, ct)
            .ConfigureAwait(false);
        if (companyIdResult.IsFailure)
            return companyIdResult.Error!;

        var companyId = companyIdResult.Value;
        var (from, to) = DateRangeDefaults.Resolve(request.From, request.To, clock.UtcNow);

        var teamAssignments = await assignments.QueryAsNoTracking()
            .Where(a => a.Team!.CompanyId == companyId
                        && a.AssignedAt >= from && a.AssignedAt <= to)
            .Select(a => new { a.TeamId, a.Status, a.AssignedAt, a.CompletedAt })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var teamNames = await teams.QueryAsNoTracking()
            .Where(t => t.CompanyId == companyId)
            .Select(t => new { t.Id, t.Name })
            .ToDictionaryAsync(t => t.Id, t => t.Name, ct)
            .ConfigureAwait(false);

        var result = teamAssignments
            .GroupBy(a => a.TeamId)
            .Select(g =>
            {
                var assignedTasks = g.Count();
                var completed = g.Where(a => a.Status == AssignmentStatus.Completed).ToList();
                var completionRate = assignedTasks == 0
                    ? 0m
                    : Math.Round(100m * completed.Count / assignedTasks, 1);
                var avgHours = completed.Count == 0
                    ? 0m
                    : Math.Round(
                        (decimal)completed.Average(a => (a.CompletedAt!.Value - a.AssignedAt).TotalHours), 1);

                return new TeamPerformanceItem(
                    g.Key,
                    teamNames.GetValueOrDefault(g.Key, "Unknown"),
                    assignedTasks,
                    completed.Count,
                    completionRate,
                    avgHours);
            })
            .OrderByDescending(i => i.CompletionRate)
            .ToList();

        return result;
    }
}
