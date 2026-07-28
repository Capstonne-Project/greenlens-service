using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Notifications.EventHandlers;

/// <summary>
/// Notifies Citizens that a new Community Cleanup program has opened for joining.
/// Draft rule: docs/community-cleanup-feature-spec.md §8 "CommunityCleanupOpenedNearby" —
/// broadcasts to all Citizens (no per-user geo data exists yet to scope by proximity).
/// </summary>
internal sealed class CommunityCleanupOpenedNotificationHandler(
    INotificationService notificationService,
    IUserRepository users,
    ILogger<CommunityCleanupOpenedNotificationHandler> logger)
    : INotificationHandler<CommunityCleanupOpenedEvent>
{
    public async Task Handle(CommunityCleanupOpenedEvent notification, CancellationToken ct)
    {
        var citizenIds = await users.QueryAsNoTracking()
            .Where(u => u.Role == UserRole.Citizen)
            .Select(u => u.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        logger.LogInformation(
            "Community cleanup {EventId} opened — notifying {Count} citizen(s)",
            notification.EventId, citizenIds.Count);

        foreach (var citizenId in citizenIds)
        {
            await notificationService.SendFromTemplateAsync(
                citizenId,
                NotificationType.CommunityCleanupOpened,
                new Dictionary<string, string> { ["title"] = notification.Title },
                notification.EventId,
                ct).ConfigureAwait(false);
        }
    }
}
