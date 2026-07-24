using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace Greenlens.Application.Features.Analytics.GetCompanyTeamPerformance;

/// <summary>Per-team KPIs (assigned/completed/completion rate/avg time) for the caller's company.</summary>
public sealed class GetCompanyTeamPerformanceQueryHandler(
    IReportAssignmentRepository assignments,
    IEnvironmentalTeamRepository teams,
    ICompanyStaffRepository companyStaff,
    ICurrentUser currentUser,
    IDateTimeProvider clock,
    ILogger<GetCompanyTeamPerformanceQueryHandler> logger)
    : IRequestHandler<GetCompanyTeamPerformanceQuery, Result<List<TeamPerformanceItem>>>
{
    public async Task<Result<List<TeamPerformanceItem>>> Handle(
        GetCompanyTeamPerformanceQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting company team performance");

        var companyIdResult = await CompanyContextResolver
            .ResolveCompanyIdAsync(companyStaff, currentUser.UserId, ct)
            .ConfigureAwait(false);
        logger.LogInformation("Company ID: {CompanyId}", companyIdResult.Value);
        if (companyIdResult.IsFailure)
            {
                logger.LogError("Failed to resolve company ID: {Error}", companyIdResult.Error);
                return companyIdResult.Error!;
            }

        var companyId = companyIdResult.Value;
        logger.LogInformation("Company ID: {CompanyId}", companyId);
        var (from, to) = DateRangeDefaults.Resolve(request.From, request.To, clock.UtcNow);
        logger.LogInformation("From: {From}, To: {To}", from, to);

        var teamAssignments = await assignments.QueryAsNoTracking()
            .Where(a => a.Team!.CompanyId == companyId
                        && a.AssignedAt >= from && a.AssignedAt <= to)
            .Select(a => new { a.TeamId, a.Status, a.AssignedAt, a.CompletedAt })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        logger.LogInformation("Team assignments: {TeamAssignments}", teamAssignments);

        var teamNames = await teams.QueryAsNoTracking()
            .Where(t => t.CompanyId == companyId)
            .Select(t => new { t.Id, t.Name })
            .ToDictionaryAsync(t => t.Id, t => t.Name, ct)
            .ConfigureAwait(false);

        logger.LogInformation("Team names: {TeamNames}", teamNames);

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

        logger.LogInformation("Company team performance retrieved successfully");

        return result;
    }
}
