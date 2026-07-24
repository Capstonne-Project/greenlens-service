using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace Greenlens.Application.Features.Analytics.GetCompanyStaffPerformance;

/// <summary>
/// Per-staff task volume and completion rate, attributed via the last progress update
/// on each assignment (the field staff who actually worked the task).
/// </summary>
public sealed class GetCompanyStaffPerformanceQueryHandler(
    IReportAssignmentRepository assignments,
    IUserRepository users,
    ICompanyStaffRepository companyStaff,
    ICurrentUser currentUser,
    IDateTimeProvider clock,
    ILogger<GetCompanyStaffPerformanceQueryHandler> logger)
    : IRequestHandler<GetCompanyStaffPerformanceQuery, Result<List<StaffPerformanceItem>>>
{
    public async Task<Result<List<StaffPerformanceItem>>> Handle(
        GetCompanyStaffPerformanceQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting company staff performance");

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

        var staffAssignments = await assignments.QueryAsNoTracking()
            .Where(a => a.Team!.CompanyId == companyId
                        && a.ProgressUpdatedByUserId != null
                        && a.AssignedAt >= from && a.AssignedAt <= to)
            .Select(a => new { StaffId = a.ProgressUpdatedByUserId!.Value, a.Status })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        logger.LogInformation("Staff assignments: {StaffAssignments}", staffAssignments);

        var staffIds = staffAssignments.Select(a => a.StaffId).Distinct().ToList();
        var staffNames = await users.QueryAsNoTracking()
            .Where(u => staffIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName })
            .ToDictionaryAsync(u => u.Id, u => u.FullName, ct)
            .ConfigureAwait(false);

        var result = staffAssignments
            .GroupBy(a => a.StaffId)
            .Select(g =>
            {
                var tasksHandled = g.Count();
                var tasksCompleted = g.Count(a => a.Status == AssignmentStatus.Completed);
                var completionRate = tasksHandled == 0
                    ? 0m
                    : Math.Round(100m * tasksCompleted / tasksHandled, 1);

                return new StaffPerformanceItem(
                    g.Key,
                    staffNames.GetValueOrDefault(g.Key, "Unknown"),
                    tasksHandled,
                    tasksCompleted,
                    completionRate);
            })
            .OrderByDescending(i => i.CompletionRate)
            .ToList();

        logger.LogInformation("Company staff performance retrieved successfully");

        return result;
    }
}
