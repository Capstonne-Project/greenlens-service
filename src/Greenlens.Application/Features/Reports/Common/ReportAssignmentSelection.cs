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
    /// Current assignment for display (newest non-declined, else newest overall).
    /// </summary>
    internal static ReportAssignment? ResolveCurrentAssignment(IEnumerable<ReportAssignment> assignments) =>
        assignments
            .OrderByDescending(a => a.AssignedAt)
            .FirstOrDefault(a => a.Status != AssignmentStatus.Declined)
        ?? assignments.OrderByDescending(a => a.AssignedAt).FirstOrDefault();
}
