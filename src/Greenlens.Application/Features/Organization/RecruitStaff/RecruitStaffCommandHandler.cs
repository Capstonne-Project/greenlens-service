using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.RecruitStaff;

/// <summary>
/// LEO sends an invitation to a Citizen user to join their LocalOffice team.
/// Creates a StaffInvitation (7-day expiry) instead of instant role change.
/// </summary>
/// <remarks>
/// Implements: BR-ORG-020 (invite via email), BR-ORG-021 (7-day expiry, single-use).
/// </remarks>
public sealed class RecruitStaffCommandHandler(
    IUserRepository users,
    IEnvironmentalTeamRepository teams,
    IStaffInvitationRepository invitations,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<RecruitStaffCommandHandler> logger) : IRequestHandler<RecruitStaffCommand, Result<RecruitStaffResponse>>
{
    public async Task<Result<RecruitStaffResponse>> Handle(
        RecruitStaffCommand request,
        CancellationToken ct)
    {
        // ── 1. Verify current user is LEO with an assigned office ──
        var leo = await users.GetByIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (leo is null)
            return Errors.Users.UserNotFound;

        if (!leo.LocalOfficeId.HasValue)
            return Errors.Organization.OfficerNoOffice;

        var leoOfficeId = leo.LocalOfficeId.Value;

        // ── 2. Find target user by email ──
        var targetUser = await users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), ct)
            .ConfigureAwait(false);

        if (targetUser is null)
            return Errors.Users.UserNotFound;

        // ── 3. Validate current role = Citizen ──
        if (targetUser.Role != UserRole.Citizen)
            return Errors.Organization.InvalidRoleForRecruit;

        // ── 4. Validate target role is Cleaner or Inspector ──
        if (request.TargetRole is not (UserRole.Cleaner or UserRole.Inspector))
            return Errors.Organization.InvalidRoleForTeamMember;

        // ── 5. Check user not already assigned to another office ──
        if (targetUser.LocalOfficeId.HasValue)
            return Errors.Organization.UserAlreadyInOffice;

        // ── 6. Check no existing pending invitation for this user ──
        var hasPending = await invitations.ExistsAsync(
            i => i.InvitedUserId == targetUser.Id && i.Status == InvitationStatus.Pending, ct)
            .ConfigureAwait(false);

        if (hasPending)
            return Errors.Organization.DuplicateInvitation;

        // ── 7. Validate team if provided ──
        Guid? assignedTeamId = null;
        if (request.TeamId.HasValue)
        {
            var team = await teams.GetByIdAsync(request.TeamId.Value, ct).ConfigureAwait(false);

            if (team is null)
                return Errors.Organization.TeamNotFound;

            if (team.LocalOfficeId != leoOfficeId)
                return Errors.Organization.TeamNotInOffice;

            // Role-TeamType compatibility
            var roleMatchesTeam = (request.TargetRole, team.TeamType) switch
            {
                (UserRole.Cleaner, TeamType.Cleanup) => true,
                (UserRole.Inspector, TeamType.Inspection) => true,
                _ => false
            };

            if (!roleMatchesTeam)
                return Errors.Organization.InvalidRoleForTeamMember;

            assignedTeamId = team.Id;
        }

        // ── 8. Create invitation instead of instant recruit ──
        var invitation = StaffInvitation.Create(
            invitedByUserId: currentUser.UserId,
            invitedUserId: targetUser.Id,
            localOfficeId: leoOfficeId,
            targetRole: request.TargetRole,
            teamId: assignedTeamId);

        invitations.Add(invitation);

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "LEO {LeoId} sent invitation {InvitationId} to user {UserId} ({Email}) as {Role}",
            currentUser.UserId, invitation.Id, targetUser.Id, targetUser.Email, request.TargetRole);

        return new RecruitStaffResponse(
            targetUser.Id,
            targetUser.Email,
            targetUser.FullName,
            request.TargetRole,
            leoOfficeId,
            assignedTeamId,
            null); // No teamMemberId yet — user must accept first
    }
}
