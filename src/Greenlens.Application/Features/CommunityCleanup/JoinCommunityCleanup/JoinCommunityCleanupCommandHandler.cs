using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.CommunityCleanup.JoinCommunityCleanup;

/// <remarks>Draft rules BR-CMU-004 (open + capacity), BR-CMU-005 (join once, immediately Joined).</remarks>
public sealed class JoinCommunityCleanupCommandHandler(
    ICommunityCleanupEventRepository events,
    ICommunityCleanupParticipantRepository participants,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<JoinCommunityCleanupCommandHandler> logger) : IRequestHandler<JoinCommunityCleanupCommand, Result>
{
    public async Task<Result> Handle(JoinCommunityCleanupCommand request, CancellationToken ct)
    {
        var ev = await events.GetByIdAsync(request.EventId, ct).ConfigureAwait(false);
        if (ev is null)
            return Errors.CommunityCleanup.EventNotFound;

        if (ev.Status != CommunityCleanupStatus.OpenForJoin)
            return Errors.CommunityCleanup.JoinClosed;

        var existing = await participants.GetByEventAndUserAsync(request.EventId, currentUser.UserId, ct).ConfigureAwait(false);
        if (existing is not null && existing.Status != CommunityCleanupParticipantStatus.Withdrawn)
            return Errors.CommunityCleanup.AlreadyJoined;

        var activeCount = await participants.CountActiveByEventIdAsync(request.EventId, ct).ConfigureAwait(false);
        if (activeCount >= ev.MaxParticipants)
            return Errors.CommunityCleanup.EventFull;

        if (existing is not null)
        {
            // Re-joining after a previous withdraw — replace the old row rather than violate the unique index.
            participants.Remove(existing);
        }

        participants.Add(CommunityCleanupParticipant.Create(ev.Id, currentUser.UserId, CommunityCleanupParticipantRole.Member));

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("User {UserId} joined community cleanup {EventId}", currentUser.UserId, request.EventId);
        return Result.Success();
    }
}
