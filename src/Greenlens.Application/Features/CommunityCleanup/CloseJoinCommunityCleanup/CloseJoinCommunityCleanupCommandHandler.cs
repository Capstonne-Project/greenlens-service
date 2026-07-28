using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.CommunityCleanup.CloseJoinCommunityCleanup;

public sealed class CloseJoinCommunityCleanupCommandHandler(
    ICommunityCleanupEventRepository events,
    IUnitOfWork uow,
    ILogger<CloseJoinCommunityCleanupCommandHandler> logger) : IRequestHandler<CloseJoinCommunityCleanupCommand, Result>
{
    public async Task<Result> Handle(CloseJoinCommunityCleanupCommand request, CancellationToken ct)
    {
        var ev = await events.GetByIdAsync(request.EventId, ct).ConfigureAwait(false);
        if (ev is null)
            return Errors.CommunityCleanup.EventNotFound;

        try
        {
            ev.CloseJoin();
        }
        catch (InvalidOperationException)
        {
            return Errors.CommunityCleanup.InvalidStatusTransition;
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Community cleanup {EventId} join closed", request.EventId);
        return Result.Success();
    }
}
