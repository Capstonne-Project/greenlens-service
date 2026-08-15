using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Organization.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.AcceptInvitation;

/// <summary>
/// Citizen accepts a staff invitation — role changes, assigned to office + optional team.
/// </summary>
/// <remarks>
/// Implements: BR-ORG-021 (accept invitation, single-use, role change),
/// BR-NTF-002 (notify LEO when invitation is accepted).
/// </remarks>
public sealed class AcceptInvitationCommandHandler(
    IStaffInvitationRepository invitations,
    IUserRepository users,
    ITeamMemberRepository teamMembers,
    IApplicationDbContext db,
    IEnvironmentalTeamRepository teams,
    INotificationService notifications,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<AcceptInvitationCommandHandler> logger)
    : IRequestHandler<AcceptInvitationCommand, Result<AcceptInvitationResponse>>
{
    public async Task<Result<AcceptInvitationResponse>> Handle(
        AcceptInvitationCommand request,
        CancellationToken ct)
    {
        logger.LogInformation("Accepting invitation {InvitationId}", request.InvitationId);

        var invitation = await invitations.GetByIdAsync(request.InvitationId, ct)
            .ConfigureAwait(false);

        if (invitation is null)
        {
            logger.LogWarning("Invitation {InvitationId} not found", request.InvitationId);
            return Errors.Organization.InvitationNotFound;
        }

        if (invitation.InvitedUserId != currentUser.UserId)
        {
            logger.LogWarning("Invitation {InvitationId} is not owned by user {UserId}", request.InvitationId, currentUser.UserId);
            return Errors.Auth.Forbidden;
        }

        var acceptResult = invitation.Accept();
        if (!acceptResult.IsSuccess)
        {
            logger.LogWarning("Failed to accept invitation {InvitationId}", request.InvitationId);
            return Result<AcceptInvitationResponse>.Failure(acceptResult.Error!);
        }

        var user = await users.GetByIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (user is null)
        {
            logger.LogWarning("User {UserId} not found", currentUser.UserId);
            return Errors.Users.UserNotFound;
        }

        user.ChangeRole(invitation.TargetRole);
        user.AssignToLocalOffice(invitation.LocalOfficeId);

        var isLeader = false;
        if (invitation.TeamId.HasValue)
        {
            var alreadyMember = await teamMembers
                .IsUserInTeamAsync(invitation.TeamId.Value, user.Id, ct)
                .ConfigureAwait(false);
            if (alreadyMember)
            {
                logger.LogWarning("User {UserId} is already a member of team {TeamId}", user.Id, invitation.TeamId.Value);
                return Errors.Organization.MemberAlreadyInTeam;
            }

            isLeader = invitation.IsLeader
                && !await TeamMembershipRules
                    .TeamHasLeaderAsync(teamMembers, invitation.TeamId.Value, excludeUserId: null, ct)
                    .ConfigureAwait(false)
                && !await TeamMembershipRules
                    .TeamHasPendingLeaderInvitationAsync(
                        invitations, invitation.TeamId.Value, excludeInvitationId: invitation.Id, ct)
                    .ConfigureAwait(false);

            if (invitation.IsLeader && !isLeader)
            {
                logger.LogWarning(
                    "Invitation {InvitationId} requested leader but team {TeamId} already has one; joining as regular member",
                    invitation.Id, invitation.TeamId.Value);
            }

            var member = TeamMember.Create(invitation.TeamId.Value, user.Id, isLeader);
            teamMembers.Add(member);
        }

        try
        {
            await uow.SaveChangesAsync(ct).ConfigureAwait(false);
            logger.LogInformation("Changes saved for invitation {InvitationId}", request.InvitationId);
        }
        catch (DbUpdateException ex)
        {
            var mapped = PostgresUniqueViolationMapper.TryMap(ex);
            if (mapped is not null)
            {
                logger.LogWarning("Failed to save changes for invitation {InvitationId}", request.InvitationId);
                return Result<AcceptInvitationResponse>.Failure(mapped);
            }
            throw;
        }

        logger.LogInformation(
            "User {UserId} accepted invitation {InvitationId}, now {Role} in office {OfficeId}",
            user.Id, invitation.Id, invitation.TargetRole, invitation.LocalOfficeId);

        var (wardName, teamName) = await StaffInvitationNotificationPlaceholders
            .ResolveContextAsync(
                invitation.LocalOfficeId,
                invitation.TeamId,
                db,
                teams,
                ct)
            .ConfigureAwait(false);

        await notifications.SendFromTemplateAsync(
            invitation.InvitedByUserId,
            NotificationType.StaffInvitationAccepted,
            StaffInvitationNotificationPlaceholders.ForResponded(
                user.FullName,
                wardName,
                invitation.TargetRole,
                teamName),
            invitation.Id,
            ct).ConfigureAwait(false);

        return new AcceptInvitationResponse(
            user.Id,
            invitation.TargetRole,
            invitation.LocalOfficeId,
            invitation.TeamId,
            isLeader);
    }
}
