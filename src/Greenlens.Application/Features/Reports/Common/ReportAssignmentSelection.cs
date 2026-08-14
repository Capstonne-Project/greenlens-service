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
