using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Organization.Common;

/// <summary>Shared membership guards for community and company environmental teams.</summary>
internal static class TeamMembershipRules
{
    /// <summary>Inspection tasks still owned by the team (not closed).</summary>
    private static readonly InspectionStatus[] TerminalInspectionStatuses =
    [
        InspectionStatus.Closed,
        InspectionStatus.ClosedNoViolation
    ];

    public static Task<bool> HasActiveTasksAsync(
        Guid teamId,
        IReportAssignmentRepository assignments,
        IInspectionReportRepository inspections,
        CancellationToken ct) =>
        HasActiveTasksAsync(teamId, assignments, inspections.QueryAsNoTracking(), ct);

    public static async Task<bool> HasActiveTasksAsync(
        Guid teamId,
        IReportAssignmentRepository assignments,
        IQueryable<InspectionReport> inspectionQuery,
        CancellationToken ct)
    {
        var cleanupTasks = await assignments
            .CountInProgressByTeamAsync(teamId, ct)
            .ConfigureAwait(false);

        if (cleanupTasks > 0)
            return true;

        return await inspectionQuery
            .AnyAsync(
                ir => ir.AssignedTeamId == teamId
                      && !TerminalInspectionStatuses.Contains(ir.Status),
                ct)
            .ConfigureAwait(false);
    }

    public static Task<bool> TeamHasLeaderAsync(
        ITeamMemberRepository members,
        Guid teamId,
        Guid? excludeUserId,
        CancellationToken ct)
    {
        var query = members.QueryAsNoTracking()
            .Where(m => m.TeamId == teamId && m.IsLeader);

        if (excludeUserId.HasValue)
            query = query.Where(m => m.UserId != excludeUserId.Value);

        return query.AnyAsync(ct);
    }

    /// <summary>True when another pending invitation already reserves the team leader slot.</summary>
    public static Task<bool> TeamHasPendingLeaderInvitationAsync(
        IStaffInvitationRepository invitations,
        Guid teamId,
        Guid? excludeInvitationId,
        CancellationToken ct)
    {
        var query = invitations.QueryAsNoTracking()
            .Where(i => i.TeamId == teamId
                        && i.Status == InvitationStatus.Pending
                        && i.IsLeader);

        if (excludeInvitationId.HasValue)
            query = query.Where(i => i.Id != excludeInvitationId.Value);

        return query.AnyAsync(ct);
    }
}
