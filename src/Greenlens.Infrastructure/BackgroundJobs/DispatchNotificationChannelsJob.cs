using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.BackgroundJobs;

/// <summary>
/// Delivers FCM push and SMTP email for a persisted notification row (out of HTTP request).
/// </summary>
/// <remarks>
/// Implements: BR-NTF-001 (channels), BR-SYS-001 (non-blocking API).
/// Idempotent: skips channels already marked dispatched (Hangfire retry safe).
/// </remarks>
[AutomaticRetry(Attempts = 3, DelaysInSeconds = [30, 120, 600])]
internal sealed class DispatchNotificationChannelsJob(
    ApplicationDbContext db,
    IPushNotificationSender pushSender,
    IEmailSender emailSender,
    ILogger<DispatchNotificationChannelsJob> logger)
{
    public async Task ExecuteAsync(Guid notificationId, CancellationToken ct = default)
    {
        var row = await db.Notifications
            .AsNoTracking()
            .Where(n => n.Id == notificationId)
            .Select(n => new
            {
                n.Id,
                n.RecipientId,
                n.Type,
                n.Title,
                n.Message,
                n.ReferenceId,
                n.Channel,
                n.PushDispatchedAt,
                n.EmailDispatchedAt,
                RecipientEmail = n.Recipient!.Email,
                FcmToken = n.Recipient!.FcmDeviceToken
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (row is null)
        {
            logger.LogWarning("DispatchNotificationChannelsJob: notification {Id} not found", notificationId);
            return;
        }

        var pushPending = ShouldSendPush(row.Channel, row.PushDispatchedAt);
        var emailPending = ShouldSendEmail(row.Channel, row.EmailDispatchedAt);

        if (!pushPending && !emailPending)
        {
            logger.LogDebug(
                "DispatchNotificationChannelsJob: notification {Id} already fully dispatched",
                notificationId);
            return;
        }

        var pushSent = false;
        var emailSent = false;

        if (pushPending && !string.IsNullOrEmpty(row.FcmToken))
        {
            try
            {
                var data = new Dictionary<string, string>
                {
                    ["notificationId"] = row.Id.ToString(),
                    ["type"] = row.Type.ToString(),
                };
                if (row.ReferenceId.HasValue)
                    data["referenceId"] = row.ReferenceId.Value.ToString();

                await pushSender.SendPushAsync(row.FcmToken, row.Title, row.Message, data, ct)
                    .ConfigureAwait(false);
                pushSent = true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "FCM push failed for notification {Id}, user {UserId}", notificationId, row.RecipientId);
                throw;
            }
        }
        else if (pushPending)
        {
            // No device token — nothing to send; mark as dispatched to avoid endless retries.
            pushSent = true;
            logger.LogDebug(
                "DispatchNotificationChannelsJob: no FCM token for user {UserId}, skipping push",
                row.RecipientId);
        }

        if (emailPending)
        {
            try
            {
                await emailSender.SendNotificationEmailAsync(row.RecipientEmail, row.Title, row.Message, ct)
                    .ConfigureAwait(false);
                emailSent = true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Email failed for notification {Id}, user {UserId}", notificationId, row.RecipientId);
                throw;
            }
        }

        if (pushSent || emailSent)
        {
            var tracked = await db.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId, ct)
                .ConfigureAwait(false);

            if (tracked is null)
                return;

            if (pushSent && tracked.PushDispatchedAt is null)
                tracked.MarkPushDispatched();

            if (emailSent && tracked.EmailDispatchedAt is null)
                tracked.MarkEmailDispatched();

            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        logger.LogInformation(
            "DispatchNotificationChannelsJob: notification {Id} push={PushSent} email={EmailSent}",
            notificationId, pushSent, emailSent);
    }

    private static bool ShouldSendPush(NotificationChannel channel, DateTime? dispatchedAt)
        => dispatchedAt is null && channel is NotificationChannel.Push or NotificationChannel.Both;

    private static bool ShouldSendEmail(NotificationChannel channel, DateTime? dispatchedAt)
        => dispatchedAt is null && channel is NotificationChannel.Email or NotificationChannel.Both;
}
