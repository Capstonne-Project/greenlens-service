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
    /// Assignments in the active report cycle (BR-REP-015). Excludes prior-cycle Completed rows
    /// when a newer cycle has open assignments on the same report.
    /// </summary>
    internal static IReadOnlyList<ReportAssignment> SelectCurrentCycleAssignments(
        IEnumerable<ReportAssignment> assignments,
        ReportStatus reportStatus)
    {
        var latestPerTeam = SelectLatestPerTeam(assignments);
        if (latestPerTeam.Count == 0)
            return [];

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

        return latestPerTeam
            .Where(a => a.Status != AssignmentStatus.Declined)
            .ToList();
    }

    internal static bool AllCurrentCycleNonDeclinedCompleted(
        IEnumerable<ReportAssignment> assignments,
        ReportStatus reportStatus)
    {
        var current = SelectCurrentCycleAssignments(assignments, reportStatus);
        if (current.Count == 0)
            return false;

        return current
            .Where(a => a.Status != AssignmentStatus.Declined)
            .All(a => a.Status == AssignmentStatus.Completed);
    }

    internal static bool AllCurrentCycleEscalatedOrCompleted(
        IEnumerable<ReportAssignment> assignments,
        ReportStatus reportStatus)
    {
        var current = SelectCurrentCycleAssignments(assignments, reportStatus);
        if (current.Count == 0)
            return false;

        return current
            .Where(a => a.Status != AssignmentStatus.Declined)
            .All(a => a.Status is AssignmentStatus.Escalated or AssignmentStatus.Completed);
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
