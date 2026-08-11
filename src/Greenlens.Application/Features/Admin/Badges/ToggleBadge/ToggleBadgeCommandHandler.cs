using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Admin.Badges.ToggleBadge;

public sealed class ToggleBadgeCommandHandler(
    IBadgeRepository badges,
    IUnitOfWork uow,
    ILogger<ToggleBadgeCommandHandler> logger)
    : IRequestHandler<ToggleBadgeCommand, Result>
{
    public async Task<Result> Handle(ToggleBadgeCommand request, CancellationToken ct)
    {
        var badge = await badges.GetByIdAsync(request.Id, ct).ConfigureAwait(false);
        if (badge is null)
        {
            logger.LogWarning("Badge not found: {Id}", request.Id);
            return Errors.Gamification.BadgeNotFound;
        }

        if (request.IsActive)
            badge.Activate();
        else
            badge.Deactivate();

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Badge {BadgeId} toggled to {IsActive}", request.Id, request.IsActive);
        return Result.Success();
    }
}
