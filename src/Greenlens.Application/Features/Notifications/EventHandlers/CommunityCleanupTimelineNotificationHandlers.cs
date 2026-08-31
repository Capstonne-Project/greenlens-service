using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Notifications.EventHandlers;

/// <summary>
/// Notifies joined participants (excluding the Leader) and the LEO once when the cleanup formally starts.
/// </summary>
/// <remarks>
/// Implements: BR-CMU-006 (start event notifies participants; LEO informed on formal start).
/// Leader check-in is a separate step and must NOT emit this notification — mobile calls
/// check-in then start in sequence; notifying on both caused duplicate pushes.
/// </remarks>
internal sealed class CommunityCleanupStartedNotificationHandler(
    ICommunityCleanupParticipantRepository participants,
    INotificationService notificationService,
    ILogger<CommunityCleanupStartedNotificationHandler> logger)
    : INotificationHandler<CommunityCleanupStartedEvent>
{
    public async Task Handle(CommunityCleanupStartedEvent notification, CancellationToken ct)
    {
        var recipients = await CommunityCleanupNotificationHelpers
            .GetJoinedMemberUserIdsAsync(participants, notification.EventId, notification.LeaderUserId, ct)
            .ConfigureAwait(false);

        logger.LogInformation(
            "Community cleanup {EventId} started, notifying {Count} joined participant(s) and LEO {LeoId}",
            notification.EventId, recipients.Count, notification.LeoId);

        foreach (var userId in recipients)
        {
            await notificationService.SendFromTemplateAsync(
                userId,
                NotificationType.CommunityCleanupStarted,
                new Dictionary<string, string> { ["title"] = notification.Title },
                notification.EventId,
                ct).ConfigureAwait(false);
        }

        await notificationService.SendFromTemplateAsync(
            notification.LeoId,
            NotificationType.CommunityCleanupLeaderStarted,
            new Dictionary<string, string> { ["title"] = notification.Title },
            notification.EventId,
            ct).ConfigureAwait(false);
    }
}

/// <summary>
/// Notifies the LEO (program owner) when the Leader posts a progress update.
/// Citizens do NOT need this — see remarks on CommunityCleanupLeaderCheckedInNotificationHandler.
/// </summary>
internal sealed class CommunityCleanupProgressUpdatedNotificationHandler(
    INotificationService notificationService,
    ILogger<CommunityCleanupProgressUpdatedNotificationHandler> logger)
    : INotificationHandler<CommunityCleanupProgressUpdatedEvent>
{
    public async Task Handle(CommunityCleanupProgressUpdatedEvent notification, CancellationToken ct)
    {
        logger.LogInformation(
            "Community cleanup {EventId} progress → {Percent}%, notifying LEO {LeoId}",
            notification.EventId, notification.ProgressPercent, notification.LeoId);

        await notificationService.SendFromTemplateAsync(
            notification.LeoId,
            NotificationType.CommunityCleanupProgressUpdated,
            new Dictionary<string, string>
            {
                ["title"] = notification.Title,
                ["percent"] = notification.ProgressPercent.ToString()
            },
            notification.EventId,
            ct).ConfigureAwait(false);
    }
}

/// <summary>Notifies the LEO that the Leader submitted completion evidence and it awaits review.</summary>
internal sealed class CommunityCleanupVerificationSubmittedNotificationHandler(
    INotificationService notificationService,
    ILogger<CommunityCleanupVerificationSubmittedNotificationHandler> logger)
    : INotificationHandler<CommunityCleanupVerificationSubmittedEvent>
{
    public async Task Handle(CommunityCleanupVerificationSubmittedEvent notification, CancellationToken ct)
    {
        logger.LogInformation(
            "Community cleanup {EventId} submitted for verification, notifying LEO {LeoId}",
            notification.EventId, notification.LeoId);

        await notificationService.SendFromTemplateAsync(
            notification.LeoId,
            NotificationType.CommunityCleanupVerificationSubmitted,
            new Dictionary<string, string> { ["title"] = notification.Title },
            notification.EventId,
            ct).ConfigureAwait(false);
    }
}

/// <summary>Notifies the Leader when the LEO rejects submitted evidence.</summary>
internal sealed class CommunityCleanupVerificationRejectedNotificationHandler(
    INotificationService notificationService,
    ILogger<CommunityCleanupVerificationRejectedNotificationHandler> logger)
    : INotificationHandler<CommunityCleanupVerificationRejectedEvent>
{
    public async Task Handle(CommunityCleanupVerificationRejectedEvent notification, CancellationToken ct)
    {
        logger.LogInformation(
            "Community cleanup {EventId} verification rejected, notifying Leader {LeaderUserId}",
            notification.EventId, notification.LeaderUserId);

        await notificationService.SendFromTemplateAsync(
            notification.LeaderUserId,
            NotificationType.CommunityCleanupVerificationRejected,
            new Dictionary<string, string>
            {
                ["title"] = notification.Title,
                ["reason"] = notification.Reason
            },
            notification.EventId,
            ct).ConfigureAwait(false);
    }
}

/// <summary>Notifies every participant (Leader included) once the LEO approves the cleanup as complete.</summary>
internal sealed class CommunityCleanupVerifiedNotificationHandler(
    ICommunityCleanupParticipantRepository participants,
    INotificationService notificationService,
    ILogger<CommunityCleanupVerifiedNotificationHandler> logger)
    : INotificationHandler<CommunityCleanupVerifiedEvent>
{
    public async Task Handle(CommunityCleanupVerifiedEvent notification, CancellationToken ct)
    {
        var all = await participants.GetByEventIdAsync(notification.EventId, ct).ConfigureAwait(false);
        var recipients = all
            .Where(p => p.Status == CommunityCleanupParticipantStatus.CheckedIn)
            .Select(p => p.UserId)
            .Distinct()
            .ToList();
        recipients = CommunityCleanupNotificationHelpers.WithLeo(recipients, notification.LeoId);

        logger.LogInformation(
            "Community cleanup {EventId} verified, notifying {Count} recipient(s) (incl. LEO)",
            notification.EventId, recipients.Count);

        foreach (var userId in recipients)
        {
            await notificationService.SendFromTemplateAsync(
                userId,
                NotificationType.CommunityCleanupVerified,
                new Dictionary<string, string> { ["title"] = notification.Title },
                notification.EventId,
                ct).ConfigureAwait(false);
        }
    }
}

/// <summary>Shared helper: active (checked-in) participants of an event, excluding one user (typically the Leader).</summary>
internal static class CommunityCleanupNotificationHelpers
{
    internal static async Task<List<Guid>> GetOtherActiveParticipantIdsAsync(
        ICommunityCleanupParticipantRepository participants,
        Guid eventId,
        Guid excludeUserId,
        CancellationToken ct)
    {
        var all = await participants.GetByEventIdAsync(eventId, ct).ConfigureAwait(false);
        return all
            .Where(p => p.Status == CommunityCleanupParticipantStatus.CheckedIn && p.UserId != excludeUserId)
            .Select(p => p.UserId)
            .Distinct()
            .ToList();
    }

    /// <summary>Members still in Joined status — prompt them to check in when the Leader starts.</summary>
    internal static async Task<List<Guid>> GetJoinedMemberUserIdsAsync(
        ICommunityCleanupParticipantRepository participants,
        Guid eventId,
        Guid excludeLeaderUserId,
        CancellationToken ct)
    {
        var all = await participants.GetByEventIdAsync(eventId, ct).ConfigureAwait(false);
        return all
            .Where(p => p.UserId != excludeLeaderUserId)
            .Where(p => p.Status == CommunityCleanupParticipantStatus.Joined)
            .Select(p => p.UserId)
            .Distinct()
            .ToList();
    }

    /// <summary>Adds the LEO to a recipient list (deduped) so they stay informed of the cleanup timeline.</summary>
    internal static List<Guid> WithLeo(List<Guid> recipients, Guid leoId)
    {
        if (!recipients.Contains(leoId))
            recipients.Add(leoId);
        return recipients;
    }
}
