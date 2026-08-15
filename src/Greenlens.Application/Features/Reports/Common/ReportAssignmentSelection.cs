using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.Features.Reports.Common;

/// <summary>
/// Resolves which <see cref="ReportAssignment"/> row handlers should act on when multiple
/// rows exist per (report, team) — e.g. after BR-REP-015 reopen creates a new cycle.
/// </summary>
internal static class ReportAssignmentSelection
{
    internal static ReportAssignment? SelectLatestForTeam(
        IEnumerable<ReportAssignment> assignments,
        Guid teamId) =>
        assignments
            .Where(a => a.TeamId == teamId)
            .OrderByDescending(a => a.AssignedAt)
            .FirstOrDefault();

    internal static ReportAssignment? SelectLatestForTeams(
        IEnumerable<ReportAssignment> assignments,
        IReadOnlyCollection<Guid> teamIds) =>
        assignments
            .Where(a => teamIds.Contains(a.TeamId))
            .OrderByDescending(a => a.AssignedAt)
            .FirstOrDefault();

    internal static bool HasOpenAssignment(IEnumerable<ReportAssignment> assignments) =>
        assignments.Any(a => a.Status is AssignmentStatus.Assigned or AssignmentStatus.InProgress);

    internal static bool HasOpenAssignmentForTeam(
        IEnumerable<ReportAssignment> assignments,
        Guid teamId)
    {
        var latest = SelectLatestForTeam(assignments, teamId);
        return latest?.Status is AssignmentStatus.Assigned or AssignmentStatus.InProgress;
    }

    internal static bool AllNonDeclinedCompleted(IEnumerable<ReportAssignment> assignments) =>
        assignments
            .Where(a => a.Status != AssignmentStatus.Declined)
            .All(a => a.Status == AssignmentStatus.Completed);

    /// <summary>
    /// Latest assignment row per team — used for list views so reopen cycles do not duplicate tasks.
    /// </summary>
    internal static IReadOnlyList<ReportAssignment> SelectLatestPerTeam(
        IEnumerable<ReportAssignment> assignments) =>
        assignments
            .GroupBy(a => a.TeamId)
            .Select(g => g.OrderByDescending(a => a.AssignedAt).First())
            .ToList();

    /// <summary>
    /// Cycle boundary for BR-REP-015 reopen. First cycle uses earliest assignment batch;
    /// subsequent cycles use the latest Reopened status history entry.
    /// </summary>
    internal static DateTime? ResolveCycleStartAt(
        int reopenedCount,
        IEnumerable<ReportStatusHistory> statusHistory,
        IReadOnlyList<ReportAssignment> latestPerTeamAssignments)
    {
        if (reopenedCount > 0)
        {
            var fromHistory = statusHistory
                .Where(h => h.ToStatus == ReportStatus.Reopened)
                .MaxBy(h => h.CreatedAt)?.CreatedAt;

            if (fromHistory.HasValue)
                return fromHistory;

            return latestPerTeamAssignments
                .Where(a => a.CompletedAt.HasValue)
                .MaxBy(a => a.CompletedAt)?.CompletedAt;
        }

        return latestPerTeamAssignments.Count > 0
            ? latestPerTeamAssignments.Min(a => a.AssignedAt)
            : null;
    }

    /// <summary>
    /// True when company dispatch belongs to the active report cycle (BR-REP-015 / BR-CMP-005).
    /// </summary>
    internal static bool IsCompanyDispatchInCurrentCycle(
        int reopenedCount,
        DateTime? dispatchedToCompanyAt,
        DateTime? cycleStartAt) =>
        reopenedCount == 0
        || (cycleStartAt.HasValue
            && dispatchedToCompanyAt.HasValue
            && dispatchedToCompanyAt >= cycleStartAt);

    /// <summary>
    /// Assignments in the active report cycle (BR-REP-015). Excludes prior-cycle rows after reopen.
    /// </summary>
    internal static IReadOnlyList<ReportAssignment> SelectCurrentCycleAssignments(
        IEnumerable<ReportAssignment> assignments,
        ReportStatus reportStatus,
        int reopenedCount = 0,
        IEnumerable<ReportStatusHistory>? statusHistory = null)
    {
        var latestPerTeam = SelectLatestPerTeam(assignments);
        if (latestPerTeam.Count == 0)
            return [];

        if (reopenedCount > 0)
        {
            var cycleStartAt = ResolveCycleStartAt(
                reopenedCount,
                statusHistory ?? [],
                latestPerTeam);

            if (cycleStartAt.HasValue)
            {
                latestPerTeam = latestPerTeam
                    .Where(a => a.AssignedAt >= cycleStartAt.Value)
                    .ToList();
            }

            if (latestPerTeam.Count == 0)
                return [];
        }

        var openAssignments = latestPerTeam
            .Where(a => a.Status is AssignmentStatus.Assigned or AssignmentStatus.InProgress)
            .ToList();

        if (openAssignments.Count > 0)
        {
            var cycleStart = openAssignments.Min(a => a.AssignedAt);
            return latestPerTeam
                .Where(a => a.Status is AssignmentStatus.Assigned or AssignmentStatus.InProgress
                    || a.AssignedAt >= cycleStart)
                .ToList();
        }

        if (reportStatus is ReportStatus.Reopened)
            return [];

        if (reportStatus is ReportStatus.InProgress)
            return latestPerTeam;

        return latestPerTeam
            .Where(a => a.Status != AssignmentStatus.Declined)
            .ToList();
    }

    internal static bool AllCurrentCycleNonDeclinedCompleted(
        IEnumerable<ReportAssignment> assignments,
        ReportStatus reportStatus,
        int reopenedCount = 0,
        IEnumerable<ReportStatusHistory>? statusHistory = null)
    {
        var current = SelectCurrentCycleAssignments(
            assignments, reportStatus, reopenedCount, statusHistory);
        if (current.Count == 0)
            return false;

        return current
            .Where(a => a.Status != AssignmentStatus.Declined)
            .All(a => a.Status == AssignmentStatus.Completed);
    }

    internal static bool AllCurrentCycleEscalatedOrCompleted(
        IEnumerable<ReportAssignment> assignments,
        ReportStatus reportStatus,
        int reopenedCount = 0,
        IEnumerable<ReportStatusHistory>? statusHistory = null)
    {
        var current = SelectCurrentCycleAssignments(
            assignments, reportStatus, reopenedCount, statusHistory);
        if (current.Count == 0)
            return false;

        return current
            .Where(a => a.Status != AssignmentStatus.Declined)
            .All(a => a.Status is AssignmentStatus.Escalated or AssignmentStatus.Completed);
    }

    /// <summary>
    /// Assignment for GET /progress and office list — cycle-aware with decline/reassign UX (BR-CLN-007, BR-OFF-012).
    /// </summary>
    internal static ReportAssignment? ResolveProgressAssignment(
        IEnumerable<ReportAssignment> assignments,
        ReportStatus reportStatus,
        int reopenedCount = 0,
        IEnumerable<ReportStatusHistory>? statusHistory = null)
    {
        var cycle = SelectCurrentCycleAssignments(
            assignments, reportStatus, reopenedCount, statusHistory);

        var open = cycle
            .Where(a => a.Status is AssignmentStatus.Assigned or AssignmentStatus.InProgress)
            .MaxBy(a => a.AssignedAt);
        if (open is not null)
            return open;

        if (reportStatus == ReportStatus.InProgress)
        {
            var nonDeclined = cycle
                .Where(a => a.Status != AssignmentStatus.Declined)
                .ToList();

            if (nonDeclined.Count > 0
                && nonDeclined.All(a => a.Status == AssignmentStatus.Completed))
            {
                return nonDeclined.MaxBy(a => a.CompletedAt ?? a.AssignedAt);
            }

            return cycle
                .Where(a => a.Status == AssignmentStatus.Declined)
                .MaxBy(a => a.AssignedAt);
        }

        if (reportStatus == ReportStatus.Reopened)
            return null;

        return cycle
            .Where(a => a.Status != AssignmentStatus.Declined)
            .MaxBy(a => a.CompletedAt ?? a.AssignedAt);
    }

    /// <summary>
    /// EF filter: keep only the newest assignment row per (report, team).
    /// </summary>
    internal static IQueryable<ReportAssignment> WhereLatestPerReportTeam(
        IQueryable<ReportAssignment> query,
        IQueryable<ReportAssignment> scope) =>
        query.Where(a =>
            !scope.Any(a2 =>
                a2.ReportId == a.ReportId &&
                a2.TeamId == a.TeamId &&
                a2.AssignedAt > a.AssignedAt));

    internal static bool MatchesCurrentAssignmentStatusFilter(
        IEnumerable<ReportAssignment> assignments,
        ReportStatus reportStatus,
        AssignmentStatus statusFilter)
    {
        var current = ResolveCurrentAssignment(assignments, reportStatus);
        return current?.Status == statusFilter;
    }

    /// <summary>
    /// Current assignment for display — active cycle only after reopen (BR-REP-015).
    /// Prefers open rows (Assigned/InProgress); while Reopened/InProgress with no open row,
    /// returns null so completed history from a prior cycle is not shown as "current".
    /// </summary>
    internal static ReportAssignment? ResolveCurrentAssignment(
        IEnumerable<ReportAssignment> assignments,
        ReportStatus reportStatus)
    {
        var list = assignments as IReadOnlyList<ReportAssignment> ?? assignments.ToList();
        if (list.Count == 0)
            return null;

        var open = list
            .Where(a => a.Status is AssignmentStatus.Assigned or AssignmentStatus.InProgress)
            .OrderByDescending(a => a.AssignedAt)
            .FirstOrDefault();

        if (open is not null)
            return open;

        if (reportStatus is ReportStatus.Reopened or ReportStatus.InProgress)
            return null;

        return list
                .OrderByDescending(a => a.AssignedAt)
                .FirstOrDefault(a => a.Status != AssignmentStatus.Declined)
            ?? list.OrderByDescending(a => a.AssignedAt).FirstOrDefault();
    }
}
