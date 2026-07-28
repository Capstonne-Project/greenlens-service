using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.CommunityCleanup.StartCommunityCleanup;

public sealed class StartCommunityCleanupCommandHandler(
    ICommunityCleanupEventRepository events,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<StartCommunityCleanupCommandHandler> logger) : IRequestHandler<StartCommunityCleanupCommand, Result>
{
    public async Task<Result> Handle(StartCommunityCleanupCommand request, CancellationToken ct)
    {
        var ev = await events.GetByIdAsync(request.EventId, ct).ConfigureAwait(false);
        if (ev is null)
            return Errors.CommunityCleanup.EventNotFound;

        if (ev.LeaderUserId != currentUser.UserId)
            return Errors.CommunityCleanup.NotEventLeader;

        if (DateTime.UtcNow < ev.StartsAt)
            return Errors.CommunityCleanup.TooEarlyToStart;

        try
        {
            ev.Start();
        }
        catch (InvalidOperationException)
        {
            return Errors.CommunityCleanup.InvalidStatusTransition;
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Community cleanup {EventId} started by Leader {UserId}", request.EventId, currentUser.UserId);
        return Result.Success();
    }
}
