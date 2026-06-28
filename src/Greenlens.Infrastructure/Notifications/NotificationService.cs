using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.Notifications;

/// <summary>
/// Core notification service. Orchestrates:
/// 1. Preference check (BR-NTF-001)
/// 2. Anti-spam guard — max 20/type/day (BR-NTF-003)
/// 3. Persistence (store Notification entity)
/// 4. Channel dispatch (push via FCM, email via SMTP)
/// </summary>
internal sealed class NotificationService(
    ApplicationDbContext db,
    IUserRepository userRepo,
    IPushNotificationSender pushSender,
    IEmailSender emailSender,
    ILogger<NotificationService> logger) : INotificationService
{
    private const int MaxNotificationsPerTypePerDay = 20;

    public async Task SendAsync(
        Guid recipientId,
        NotificationType type,
        string title,
        string message,
        Guid? referenceId = null,
        CancellationToken ct = default)
    {
        // 1. Load user for FCM token and email
        var user = await userRepo.GetByIdAsync(recipientId, ct).ConfigureAwait(false);
        if (user is null)
        {
            logger.LogWarning("Notification skipped: user {UserId} not found", recipientId);
            return;
        }

        // 2. Check preferences — default to enabled if no preference exists
        var pref = await db.NotificationPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == recipientId && p.Type == type, ct)
            .ConfigureAwait(false);

        var pushEnabled = pref?.PushEnabled ?? true;
        var emailEnabled = pref?.EmailEnabled ?? true;

        if (!pushEnabled && !emailEnabled)
        {
            logger.LogDebug("Notification skipped: user {UserId} disabled all channels for {Type}",
                recipientId, type);
            return;
        }

        // 3. Anti-spam check (BR-NTF-003): max 20 per type per day
        var todayStart = DateTime.UtcNow.Date;
        var todayCount = await db.Notifications
            .CountAsync(n => n.RecipientId == recipientId
                          && n.Type == type
                          && n.CreatedAt >= todayStart, ct)
            .ConfigureAwait(false);

        if (todayCount >= MaxNotificationsPerTypePerDay)
        {
            logger.LogDebug(
                "Notification throttled: user {UserId} exceeded {Max}/day for {Type}",
                recipientId, MaxNotificationsPerTypePerDay, type);
            return; // TODO: queue for digest (P2)
        }

        // 4. Determine channel
        var channel = (pushEnabled, emailEnabled) switch
        {
            (true, true) => NotificationChannel.Both,
            (true, false) => NotificationChannel.Push,
            (false, true) => NotificationChannel.Email,
            _ => NotificationChannel.Both // unreachable due to guard above
        };

        // 5. Persist notification
        var notification = Notification.Create(recipientId, type, title, message, channel, referenceId);
        db.Notifications.Add(notification);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // 6. Dispatch to channels (fire-and-forget style, errors logged but not thrown)
        if (pushEnabled && !string.IsNullOrEmpty(user.FcmDeviceToken))
        {
            try
            {
                var data = referenceId.HasValue
                    ? new Dictionary<string, string> { ["referenceId"] = referenceId.Value.ToString(), ["type"] = type.ToString() }
                    : null;

                await pushSender.SendPushAsync(user.FcmDeviceToken, title, message, data, ct)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "FCM push failed for user {UserId}", recipientId);
            }
        }

        if (emailEnabled)
        {
            try
            {
                await emailSender.SendNotificationEmailAsync(user.Email, title, message, ct)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Email notification failed for user {UserId}", recipientId);
            }
        }

        logger.LogInformation(
            "Notification sent: {Type} to user {UserId} via {Channel}",
            type, recipientId, channel);
    }
}
