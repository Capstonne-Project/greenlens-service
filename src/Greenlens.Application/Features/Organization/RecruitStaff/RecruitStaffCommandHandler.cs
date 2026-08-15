using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Notifications;
using Greenlens.Application.Features.Organization;
using Greenlens.Application.Features.Organization.Common;
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
/// Implements: BR-ORG-020 (invite via email), BR-ORG-021 (7-day expiry, single-use),
/// BR-NTF-002 (notify Citizen when invitation is sent).
/// </remarks>
public sealed class RecruitStaffCommandHandler(
    IUserRepository users,
    IEnvironmentalTeamRepository teams,
    IStaffInvitationRepository invitations,
    ITeamMemberRepository teamMembers,
    IApplicationDbContext db,
    INotificationService notifications,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<RecruitStaffCommandHandler> logger) : IRequestHandler<RecruitStaffCommand, Result<RecruitStaffResponse>>
{
    public async Task<Result<RecruitStaffResponse>> Handle(
        RecruitStaffCommand request,
        CancellationToken ct)
    {
        logger.LogInformation("Recruiting staff for user {UserId}", currentUser.UserId);

        // ── 1. Verify current user is LEO with an assigned office ──
        var leo = await users.GetByIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (leo is null)
        {
            logger.LogWarning("LEO not found for user ID {UserId}", currentUser.UserId);
            return Errors.Users.UserNotFound;
        }

        if (!leo.LocalOfficeId.HasValue)
        {
            logger.LogWarning("LEO {UserId} has no office", currentUser.UserId);
            return Errors.Organization.OfficerNoOffice;
        }

        var leoOfficeId = leo.LocalOfficeId.Value;

        // ── 2. Find target user by email ──
        var targetUser = await users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), ct)
            .ConfigureAwait(false);

        if (targetUser is null)
        {
            logger.LogWarning("Target user not found for email {Email}", request.Email);
            return Errors.Users.UserNotFound;
        }

        // ── 3. Validate current role = Citizen ──
        if (targetUser.Role != UserRole.Citizen)
        {
            logger.LogWarning("Target user {UserId} is not a citizen", targetUser.Id);
            return Errors.Organization.InvalidRoleForRecruit;
        }
        // ── 4. Validate target role is Cleaner or Inspector ──
        if (request.TargetRole is not (UserRole.Cleaner or UserRole.Inspector))
        {
            logger.LogWarning("Target role {Role} is not a cleaner or inspector", request.TargetRole);
            return Errors.Organization.InvalidRoleForTeamMember;
        }
        // ── 5. Check user not already assigned to another office ──
        if (targetUser.LocalOfficeId.HasValue)
        {
            logger.LogWarning("Target user {UserId} is already assigned to another office", targetUser.Id);
            return Errors.Organization.UserAlreadyInOffice;
        }

        // ── 6. Check no existing pending invitation for this user ──
        var hasPending = await invitations.ExistsAsync(
            i => i.InvitedUserId == targetUser.Id && i.Status == InvitationStatus.Pending, ct)
            .ConfigureAwait(false);

        if (hasPending)
        {
            logger.LogWarning("Target user {UserId} already has a pending invitation", targetUser.Id);
            return Errors.Organization.DuplicateInvitation;
        }

        // ── 7. Validate team if provided ──
        Guid? assignedTeamId = null;
        string? assignedTeamName = null;
        if (request.TeamId.HasValue)
        {
            var team = await teams.GetByIdAsync(request.TeamId.Value, ct).ConfigureAwait(false);

            if (team is null)
            {
                logger.LogWarning("Team {TeamId} not found", request.TeamId.Value);
                return Errors.Organization.TeamNotFound;
            }

            if (team.LocalOfficeId != leoOfficeId)
            {
                logger.LogWarning("Team {TeamId} is not in the LEO's office", request.TeamId.Value);
                return Errors.Organization.TeamNotInOffice;
            }

            // Role-TeamType compatibility
            var roleMatchesTeam = (request.TargetRole, team.TeamType) switch
            {
                (UserRole.Cleaner, TeamType.Cleanup) => true,
                (UserRole.Inspector, TeamType.Inspection) => true,
                _ => false
            };

            if (!roleMatchesTeam)
            {
                logger.LogWarning("Target role {Role} is not compatible with team type {TeamType}", request.TargetRole, team.TeamType);
                return Errors.Organization.InvalidRoleForTeamMember;
            }

            assignedTeamId = team.Id;
            assignedTeamName = team.Name;
        }

        // ── 8. IsLeader can only be requested together with a team, and a team can have only one leader ──
        var isLeader = request.IsLeader ?? false;
        if (isLeader)
        {
            if (!assignedTeamId.HasValue)
            {
                logger.LogWarning("IsLeader requested without a team for user {UserId}", targetUser.Id);
                return Errors.Organization.InvalidRoleForTeamMember;
            }

            if (await TeamMembershipRules.TeamHasLeaderAsync(teamMembers, assignedTeamId.Value, excludeUserId: null, ct)
                .ConfigureAwait(false))
            {
                logger.LogWarning("Team {TeamId} already has a leader", assignedTeamId.Value);
                return Errors.Organization.TeamAlreadyHasLeader;
            }

            if (await TeamMembershipRules.TeamHasPendingLeaderInvitationAsync(
                    invitations, assignedTeamId.Value, excludeInvitationId: null, ct)
                .ConfigureAwait(false))
            {
                logger.LogWarning("Team {TeamId} already has a pending leader invitation", assignedTeamId.Value);
                return Errors.Organization.TeamAlreadyHasLeader;
            }
        }

        // ── 9. Create invitation instead of instant recruit ──
        var invitation = StaffInvitation.Create(
            invitedByUserId: currentUser.UserId,
            invitedUserId: targetUser.Id,
            localOfficeId: leoOfficeId,
            targetRole: request.TargetRole,
            teamId: assignedTeamId,
            isLeader: isLeader);

        invitations.Add(invitation);

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        var (wardName, _) = await StaffInvitationNotificationPlaceholders
            .ResolveContextAsync(leoOfficeId, assignedTeamId, db, teams, ct)
            .ConfigureAwait(false);

        await notifications.SendFromTemplateAsync(
            targetUser.Id,
            NotificationType.StaffInvitationReceived,
            StaffInvitationNotificationPlaceholders.ForReceived(
                NotificationVietnameseLabels.LeoOfficer(
                    wardName == "khu vực liên quan" ? null : wardName),
                wardName,
                request.TargetRole,
                assignedTeamName),
            invitation.Id,
            ct).ConfigureAwait(false);

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
            null,
            isLeader);
    }
}
