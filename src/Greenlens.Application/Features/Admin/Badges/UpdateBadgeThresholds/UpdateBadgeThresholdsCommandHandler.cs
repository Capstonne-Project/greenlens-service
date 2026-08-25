using System.Text.Json;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Gamification;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Admin.Badges.UpdateBadgeThresholds;

/// <summary>
/// Updates the persisted eligibility threshold for a badge.
/// </summary>
/// <remarks>Implements: BR-ADM-005, BR-GAM-004, BR-ADM-010.</remarks>
public sealed class UpdateBadgeThresholdsCommandHandler(
    IBadgeRepository badges,
    IUnitOfWork uow,
    IAuditLogger auditLogger,
    ILogger<UpdateBadgeThresholdsCommandHandler> logger)
    : IRequestHandler<UpdateBadgeThresholdsCommand, Result>
{
    public async Task<Result> Handle(UpdateBadgeThresholdsCommand request, CancellationToken ct)
    {
        var badge = await badges.GetByIdAsync(request.Id, ct).ConfigureAwait(false);
        if (badge is null)
        {
            logger.LogWarning("Badge not found: {Id}", request.Id);
            return Errors.Gamification.BadgeNotFound;
        }

        var oldThreshold = BadgeThresholdPolicy.GetThreshold(badge);
        if (oldThreshold == request.Threshold)
            return Result.Success();

        var oldSnapshot = JsonSerializer.Serialize(new
        {
            badge.Code,
            axis = BadgeThresholdPolicy.GetAxis(badge.Code).ToString(),
            threshold = oldThreshold
        });

        badge.UpdateThreshold(request.Threshold);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        await auditLogger.LogAsync(
            "UpdateBadgeThresholds",
            "Badge",
            badge.Id.ToString(),
            oldValues: oldSnapshot,
            newValues: JsonSerializer.Serialize(new
            {
                badge.Code,
                axis = BadgeThresholdPolicy.GetAxis(badge.Code).ToString(),
                threshold = BadgeThresholdPolicy.GetThreshold(badge)
            }),
            ct).ConfigureAwait(false);

        logger.LogInformation(
            "Badge {BadgeId} ({Code}) threshold updated to {Threshold}",
            badge.Id,
            badge.Code,
            request.Threshold);

        return Result.Success();
    }
}
