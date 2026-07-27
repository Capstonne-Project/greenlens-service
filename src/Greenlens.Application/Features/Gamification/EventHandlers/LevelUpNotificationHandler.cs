using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Gamification.EventHandlers;

/// <summary>Notify citizen when gamification level increases (BR-GAM-003, BR-NTF-002).</summary>
/// <remarks>Implements: BR-GAM-003, BR-NTF-002.</remarks>
internal sealed class LevelUpNotificationHandler(
    INotificationService notificationService,
    ILogger<LevelUpNotificationHandler> logger)
    : INotificationHandler<LevelUpEvent>
{
    public async Task Handle(LevelUpEvent notification, CancellationToken ct)
    {
        if (notification.NewLevel <= notification.PreviousLevel)
        {
            logger.LogDebug(
                "LevelUp skipped for user {UserId}: previous={Previous}, new={New}",
                notification.UserId, notification.PreviousLevel, notification.NewLevel);
            return;
        }

        logger.LogInformation(
            "Notification: user {UserId} leveled up {Previous} → {New}",
            notification.UserId, notification.PreviousLevel, notification.NewLevel);

        await notificationService.SendFromTemplateAsync(
            notification.UserId,
            NotificationType.LevelUp,
            GamificationNotificationPlaceholders.ForLevelUp(notification.NewLevel),
            referenceId: notification.UserId,
            ct).ConfigureAwait(false);
    }
}
