using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Notifications.EventHandlers;

/// <summary>
/// Notifies the appointed Leader that a Community Cleanup program was opened for them to run.
/// Draft rule: docs/community-cleanup-feature-spec.md §8, BR-CMU-002.
/// The program itself is discoverable by Citizens on the public map (a "Cộng đồng" marker on
/// the report's pin) rather than via a broadcast notification to every Citizen account.
/// </summary>
internal sealed class CommunityCleanupOpenedNotificationHandler(
    INotificationService notificationService,
    ILogger<CommunityCleanupOpenedNotificationHandler> logger)
    : INotificationHandler<CommunityCleanupOpenedEvent>
{
    public async Task Handle(CommunityCleanupOpenedEvent notification, CancellationToken ct)
    {
        logger.LogInformation(
            "Community cleanup {EventId} opened — notifying leader {LeaderUserId}",
            notification.EventId, notification.LeaderUserId);

        await notificationService.SendFromTemplateAsync(
            notification.LeaderUserId,
            NotificationType.CommunityCleanupLeaderAssigned,
            new Dictionary<string, string> { ["title"] = notification.Title },
            notification.EventId,
            ct).ConfigureAwait(false);
    }
}
