using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Features.Gamification.AwardPoints;
using Greenlens.Application.Features.Gamification.CheckBadges;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Gamification;

/// <summary>
/// Shared config lookup and AwardPoints dispatch for gamification event handlers.
/// </summary>
internal static class GamificationPointAwarder
{
    internal static async Task<int> GetConfiguredPointsAsync(
        IApplicationDbContext db,
        PointReason reason,
        int fallback,
        CancellationToken ct)
    {
        var config = await db.Set<GamificationConfig>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ActionType == reason, ct)
            .ConfigureAwait(false);

        if (config is null)
            return fallback;

        return config.IsActive ? config.Points : 0;
    }

    /// <returns>True when points were persisted; false when skipped or failed.</returns>
    internal static async Task<bool> TryAwardAsync(
        ISender sender,
        ILogger logger,
        Guid userId,
        int points,
        PointReason reason,
        Guid? referenceId,
        string eventLabel,
        CancellationToken ct)
    {
        var awardResult = await sender
            .Send(new AwardPointsCommand(userId, points, reason, referenceId), ct)
            .ConfigureAwait(false);

        if (awardResult.IsFailure)
        {
            logger.LogWarning(
                "Failed to award {EventLabel} points for user {UserId}, reference {ReferenceId}: {ErrorCode}",
                eventLabel, userId, referenceId, awardResult.Error!.Code);
            return false;
        }

        if (awardResult.Value!.WasSkipped)
        {
            logger.LogWarning(
                "{EventLabel} points skipped for user {UserId}, reference {ReferenceId} (locked or duplicate transaction)",
                eventLabel, userId, referenceId);
            return false;
        }

        logger.LogInformation(
            "Awarded {Points} {EventLabel} points to user {UserId} (reference={ReferenceId})",
            points, eventLabel, userId, referenceId);

        return true;
    }

    internal static async Task TryCheckBadgesAsync(
        ISender sender,
        Guid userId,
        CancellationToken ct)
    {
        await sender.Send(new CheckBadgesCommand(userId), ct).ConfigureAwait(false);
    }
}
