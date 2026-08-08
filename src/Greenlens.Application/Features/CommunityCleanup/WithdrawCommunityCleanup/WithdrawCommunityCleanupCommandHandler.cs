using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.CommunityCleanup.WithdrawCommunityCleanup;

/// <remarks>Draft rule BR-CMU-006: only while OpenForJoin/JoinClosed and not yet CheckedIn.</remarks>
public sealed class WithdrawCommunityCleanupCommandHandler(
    ICommunityCleanupEventRepository events,
    ICommunityCleanupParticipantRepository participants,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<WithdrawCommunityCleanupCommandHandler> logger) : IRequestHandler<WithdrawCommunityCleanupCommand, Result>
{
    public async Task<Result> Handle(WithdrawCommunityCleanupCommand request, CancellationToken ct)
    {
        var ev = await events.GetByIdAsync(request.EventId, ct).ConfigureAwait(false);
        if (ev is null)
            return Errors.CommunityCleanup.EventNotFound;

        var participant = await participants.GetByEventAndUserAsync(request.EventId, currentUser.UserId, ct).ConfigureAwait(false);
        if (participant is null)
            return Errors.CommunityCleanup.ParticipantNotFound;

        if (ev.Status is not (CommunityCleanupStatus.OpenForJoin or CommunityCleanupStatus.JoinClosed))
            return Errors.CommunityCleanup.CannotWithdraw;

        try
        {
            participant.Withdraw();
        }
        catch (InvalidOperationException)
        {
            return Errors.CommunityCleanup.CannotWithdraw;
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("User {UserId} withdrew from community cleanup {EventId}", currentUser.UserId, request.EventId);
        return Result.Success();
    }
}
