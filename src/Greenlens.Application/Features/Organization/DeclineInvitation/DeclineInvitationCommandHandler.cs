using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.DeclineInvitation;

/// <summary>BR-ORG-021: Citizen declines — keeps Citizen role, invitation marked Declined.</summary>
public sealed class DeclineInvitationCommandHandler(
    IStaffInvitationRepository invitations,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<DeclineInvitationCommandHandler> logger)
    : IRequestHandler<DeclineInvitationCommand, Result>
{
    public async Task<Result> Handle(DeclineInvitationCommand request, CancellationToken ct)
    {
        var invitation = await invitations.GetByIdAsync(request.InvitationId, ct)
            .ConfigureAwait(false);

        if (invitation is null)
            return Errors.Organization.InvitationNotFound;

        if (invitation.InvitedUserId != currentUser.UserId)
            return Errors.Auth.Forbidden;

        var result = invitation.Decline();
        if (!result.IsSuccess)
            return result;

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "User {UserId} declined invitation {InvitationId}",
            currentUser.UserId, invitation.Id);

        return Result.Success();
    }
}
