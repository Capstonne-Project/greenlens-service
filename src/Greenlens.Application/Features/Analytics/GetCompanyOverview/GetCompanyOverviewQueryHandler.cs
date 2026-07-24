using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace Greenlens.Application.Features.Analytics.GetCompanyOverview;

/// <summary>Overview KPIs scoped to the caller's company: task volume, SLA compliance, resolution time.</summary>
public sealed class GetCompanyOverviewQueryHandler(
    IReportRepository reports,
    ICompanyStaffRepository companyStaff,
    IEnvironmentalTeamRepository teams,
    ICurrentUser currentUser,
    IDateTimeProvider clock,
    ILogger<GetCompanyOverviewQueryHandler> logger)
    : IRequestHandler<GetCompanyOverviewQuery, Result<CompanyOverviewResponse>>
{
    public async Task<Result<CompanyOverviewResponse>> Handle(
        GetCompanyOverviewQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting company overview");

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

        logger.LogInformation("Dispatched: {Dispatched}", dispatched);

        var assignedTasks = dispatched.Count;
        logger.LogInformation("Assigned tasks: {AssignedTasks}", assignedTasks);
        var completedTasks = dispatched.Count(r => r.Status is ReportStatus.Resolved or ReportStatus.Closed);
        logger.LogInformation("Completed tasks: {CompletedTasks}", completedTasks);
        var pendingTasks = assignedTasks - completedTasks;
        logger.LogInformation("Pending tasks: {PendingTasks}", pendingTasks);

        var activeTeams = await teams.QueryAsNoTracking()
            .CountAsync(t => t.CompanyId == companyId && t.IsActive, ct)
            .ConfigureAwait(false);
        logger.LogInformation("Active teams: {ActiveTeams}", activeTeams);
        var activeStaff = await companyStaff.QueryAsNoTracking()
            .CountAsync(s => s.CompanyId == companyId && s.IsActive, ct)
            .ConfigureAwait(false);

        logger.LogInformation("Active staff: {ActiveStaff}", activeStaff);

        var slaComplianceRate = assignedTasks == 0
            ? 100m
            : Math.Round(100m * dispatched.Count(r => !r.SlaResolveBreached) / assignedTasks, 1);

        logger.LogInformation("SLA compliance rate: {SlaComplianceRate}", slaComplianceRate);

        var resolutionHoursSamples = dispatched
            .Where(r => r.Status is ReportStatus.Resolved or ReportStatus.Closed
                        && r.VerifiedAt.HasValue && r.ResolvedAt.HasValue)
            .Select(r => (decimal)(r.ResolvedAt!.Value - r.VerifiedAt!.Value).TotalHours)
            .ToList();

        logger.LogInformation("Resolution hours samples: {ResolutionHoursSamples}", resolutionHoursSamples);

        var averageResolutionHours = resolutionHoursSamples.Count == 0
            ? 0m
            : Math.Round(resolutionHoursSamples.Average(), 1);

        logger.LogInformation("Average resolution hours: {AverageResolutionHours}", averageResolutionHours);

        logger.LogInformation("Company overview retrieved successfully");

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
