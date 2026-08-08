using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;


namespace Greenlens.Application.Features.CommunityCleanup.RejectCommunityVerification;

public sealed class RejectCommunityVerificationCommandHandler(
    ICommunityCleanupEventRepository events,
    IUnitOfWork uow,
    ILogger<RejectCommunityVerificationCommandHandler> logger) : IRequestHandler<RejectCommunityVerificationCommand, Result>
{
    public async Task<Result> Handle(RejectCommunityVerificationCommand request, CancellationToken ct)
    {
        var ev = await events.GetByIdAsync(request.EventId, ct).ConfigureAwait(false);
        if (ev is null)
            return Errors.CommunityCleanup.EventNotFound;

        if (ev.Status != CommunityCleanupStatus.PendingVerification)
            return Errors.CommunityCleanup.InvalidStatusTransition;

        ev.Reject(request.Reason);

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Community cleanup {EventId} verification rejected: {Reason}", request.EventId, request.Reason);
        return Result.Success();
    }
}
