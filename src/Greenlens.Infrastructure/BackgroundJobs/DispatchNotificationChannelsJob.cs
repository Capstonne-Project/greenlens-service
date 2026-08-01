using Greenlens.Application.Common.Interfaces;
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
/// Push and email are independent: one channel failing does not block the other.
/// Each channel is marked dispatched immediately after success so Hangfire retry only retries failed channels.
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

        Exception? pushFailure = null;
        Exception? emailFailure = null;
        var pushAttempted = false;
        var pushSucceeded = false;
        var emailAttempted = false;
        var emailSucceeded = false;

        if (pushPending)
        {
            pushAttempted = true;

            if (string.IsNullOrEmpty(row.FcmToken))
            {
                await MarkPushDispatchedAsync(notificationId, ct).ConfigureAwait(false);
                pushSucceeded = true;
                logger.LogDebug(
                    "DispatchNotificationChannelsJob: no FCM token for user {UserId}, skipping push",
                    row.RecipientId);
            }
            else
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

                    await MarkPushDispatchedAsync(notificationId, ct).ConfigureAwait(false);
                    pushSucceeded = true;
                }
                catch (Exception ex)
                {
                    pushFailure = ex;
                    logger.LogError(
                        ex,
                        "FCM push failed for notification {Id}, user {UserId}",
                        notificationId,
                        row.RecipientId);
                }
            }
        }

        if (emailPending)
        {
            emailAttempted = true;

            try
            {
                await emailSender.SendNotificationEmailAsync(row.RecipientEmail, row.Title, row.Message, ct)
                    .ConfigureAwait(false);

                await MarkEmailDispatchedAsync(notificationId, ct).ConfigureAwait(false);
                emailSucceeded = true;
            }
            catch (Exception ex)
            {
                emailFailure = ex;
                logger.LogError(
                    ex,
                    "Email failed for notification {Id}, user {UserId}",
                    notificationId,
                    row.RecipientId);
            }
        }

        logger.LogInformation(
            "DispatchNotificationChannelsJob: notification {Id} pushAttempted={PushAttempted} pushOk={PushOk} emailAttempted={EmailAttempted} emailOk={EmailOk}",
            notificationId,
            pushAttempted,
            pushSucceeded,
            emailAttempted,
            emailSucceeded);

        if (pushFailure is not null && emailFailure is not null)
            throw new AggregateException("Notification channel dispatch failed", pushFailure, emailFailure);

        if (pushFailure is not null)
            throw pushFailure;

        if (emailFailure is not null)
            throw emailFailure;
    }

    private async Task MarkPushDispatchedAsync(Guid notificationId, CancellationToken ct)
    {
        var tracked = await db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId, ct)
            .ConfigureAwait(false);

        if (tracked is null || tracked.PushDispatchedAt is not null)
            return;

        tracked.MarkPushDispatched();
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private async Task MarkEmailDispatchedAsync(Guid notificationId, CancellationToken ct)
    {
        var tracked = await db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId, ct)
            .ConfigureAwait(false);

        if (tracked is null || tracked.EmailDispatchedAt is not null)
            return;

        tracked.MarkEmailDispatched();
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private static bool ShouldSendPush(NotificationChannel channel, DateTime? dispatchedAt)
        => dispatchedAt is null && channel is NotificationChannel.Push or NotificationChannel.Both;

    private static bool ShouldSendEmail(NotificationChannel channel, DateTime? dispatchedAt)
        => dispatchedAt is null && channel is NotificationChannel.Email or NotificationChannel.Both;
}
