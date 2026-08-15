using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Reports.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.GetMyTaskProgressStats;

/// <summary>Scoping mirrors <see cref="Greenlens.Application.Features.Reports.GetMyAssignments.GetMyAssignmentsQueryHandler"/> — same team-membership lookup.</summary>
public sealed class GetMyTaskProgressStatsQueryHandler(
    ITeamMemberRepository teamMembers,
    IReportAssignmentRepository assignments,
    ICurrentUser currentUser,
    IDateTimeProvider clock,
    ILogger<GetMyTaskProgressStatsQueryHandler> logger)
    : IRequestHandler<GetMyTaskProgressStatsQuery, Result<MyTaskProgressStatsResponse>>
{
    private const int TrendDays = 30;

    public async Task<Result<MyTaskProgressStatsResponse>> Handle(
        GetMyTaskProgressStatsQuery request, CancellationToken ct)
    {
        var utcNow = clock.UtcNow;

        var myTeamIds = await teamMembers
            .QueryAsNoTracking()
            .Where(m => m.UserId == currentUser.UserId)
            .Select(m => m.TeamId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (myTeamIds.Count == 0)
        {
            logger.LogWarning("Người dùng không thuộc đội nào. User ID: {UserId}", currentUser.UserId);
            return new MyTaskProgressStatsResponse(0, [], [], 0, BuildEmptyTrend(utcNow));
        }

        var assignmentScope = assignments.QueryAsNoTracking();

        var baseQuery = ReportAssignmentSelection.WhereLatestPerReportTeam(
            assignmentScope.Where(a => myTeamIds.Contains(a.TeamId)),
            assignmentScope)
            .Include(a => a.Report);

        var totalCount = await baseQuery.CountAsync(ct).ConfigureAwait(false);

        var statusCounts = await baseQuery
            .GroupBy(a => a.Status)
            .Select(g => new StatusCountItem(g.Key, g.Count()))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var severityCounts = await baseQuery
            .GroupBy(a => a.Report!.Severity)
            .Select(g => new SeverityCountItem(g.Key, g.Count()))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var overdueCount = await baseQuery
            .CountAsync(
                a => a.Report!.SlaResolveDueAt != null
                    && a.Report.SlaResolveDueAt < utcNow
                    && a.Status != AssignmentStatus.Completed
                    && a.Status != AssignmentStatus.Declined,
                ct)
            .ConfigureAwait(false);

        var trendFrom = utcNow.Date.AddDays(-(TrendDays - 1));
        var completedDates = await baseQuery
            .Where(a => a.CompletedAt != null && a.CompletedAt >= trendFrom)
            .Select(a => a.CompletedAt!.Value.Date)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var completedByDate = completedDates.GroupBy(d => d).ToDictionary(g => g.Key, g => g.Count());
        var trend = Enumerable.Range(0, TrendDays)
            .Select(offset => trendFrom.AddDays(offset))
            .Select(date => new DailyCompletionItem(DateOnly.FromDateTime(date), completedByDate.GetValueOrDefault(date)))
            .ToList();

        logger.LogInformation("Lấy thống kê tiến độ nhiệm vụ thành công. Tổng: {Total}", totalCount);

        return new MyTaskProgressStatsResponse(totalCount, statusCounts, severityCounts, overdueCount, trend);
    }

    private static List<DailyCompletionItem> BuildEmptyTrend(DateTime utcNow)
    {
        var from = utcNow.Date.AddDays(-(TrendDays - 1));
        return Enumerable.Range(0, TrendDays)
            .Select(offset => from.AddDays(offset))
            .Select(date => new DailyCompletionItem(DateOnly.FromDateTime(date), 0))
            .ToList();
    }
}
