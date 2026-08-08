using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.DeclineInvitation;

/// <summary>
/// BR-ORG-021: Citizen declines — keeps Citizen role, invitation marked Declined.
/// BR-NTF-002: Notify LEO when invitation is declined.
/// </summary>
public sealed class DeclineInvitationCommandHandler(
    IStaffInvitationRepository invitations,
    IUserRepository users,
    IApplicationDbContext db,
    IEnvironmentalTeamRepository teams,
    INotificationService notifications,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<DeclineInvitationCommandHandler> logger)
    : IRequestHandler<DeclineInvitationCommand, Result>
{
    public async Task<Result> Handle(DeclineInvitationCommand request, CancellationToken ct)
    {
        logger.LogInformation("Declining invitation {InvitationId} for user {UserId}", request.InvitationId, currentUser.UserId);

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

        var result = invitation.Decline();
        if (!result.IsSuccess)
        {
            logger.LogWarning("Failed to decline invitation {InvitationId}", request.InvitationId);
            return result;
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        var user = await users.GetByIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (user is not null)
        {
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
                NotificationType.StaffInvitationDeclined,
                StaffInvitationNotificationPlaceholders.ForResponded(
                    user.FullName,
                    wardName,
                    invitation.TargetRole,
                    teamName),
                invitation.Id,
                ct).ConfigureAwait(false);
        }

        logger.LogInformation("Invitation {InvitationId} declined for user {UserId}", request.InvitationId, currentUser.UserId);
        return Result.Success();
    }
}
