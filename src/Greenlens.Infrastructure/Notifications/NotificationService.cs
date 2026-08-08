using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Greenlens.Infrastructure.Notifications.Hubs;

namespace Greenlens.Infrastructure.Notifications;

/// <summary>
/// Core notification service. Orchestrates:
/// 1. Preference check (BR-NTF-001)
/// 2. Anti-spam guard — max 20/type/day (BR-NTF-003)
/// 3. Persistence (store Notification entity)
/// 4. SignalR (sync, in-request)
/// 5. FCM + SMTP via Hangfire background job (BR-SYS-001)
/// </summary>
/// <remarks>
/// Performance — async channel dispatch (no longer blocks HTTP on FCM/SMTP):
/// POST /v1/reports, PUT verify/reject/resolve, POST assign, dispatch-to-company,
/// assign-company-team, reassign, confirm-duplicate, flag, reopen-requests/*,
/// POST comments, community-cleanups, issue-penalty, recruit staff, invitations,
/// company suspend/terminate, and all domain-event handlers calling this service.
/// </remarks>
internal sealed class NotificationService(
    ApplicationDbContext db,
    IChangeTrackerCleaner changeTrackerCleaner,
    INotificationDispatchScheduler dispatchScheduler,
    IHubContext<NotificationHub, INotificationClient> hubContext,
    ILogger<NotificationService> logger) : INotificationService
{
    private const int MaxNotificationsPerTypePerDay = 20;
    private static readonly System.Text.RegularExpressions.Regex PlaceholderPattern = new(@"\{[a-z_]+\}", System.Text.RegularExpressions.RegexOptions.Compiled);

    public async Task SendFromTemplateAsync(
        Guid recipientId,
        NotificationType type,
        Dictionary<string, string> placeholders,
        Guid? referenceId = null,
        CancellationToken ct = default)
    {
        var template = await db.Set<NotificationTemplate>()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Type == type && t.IsPublished && t.IsActive, ct)
            .ConfigureAwait(false);

        string title;
        string message;

        if (template is null)
        {
            logger.LogWarning("No published NotificationTemplate found for Type {Type}. Using generic fallback.", type);
            title = $"Thông báo: {type}";
            message = "Bạn có một thông báo mới từ hệ thống.";
        }
        else
        {
            title = RenderPlaceholders(template.TitleVi, placeholders);
            message = RenderPlaceholders(template.BodyVi, placeholders);
        }

        await SendRawAsync(recipientId, type, title, message, referenceId, ct).ConfigureAwait(false);
    }

    private static string RenderPlaceholders(string template, Dictionary<string, string> data)
    {
        return PlaceholderPattern.Replace(template, match =>
        {
            var key = match.Value.Trim('{', '}');
            return data.TryGetValue(key, out var value) ? value : match.Value;
        });
    }

    public async Task SendRawAsync(
        Guid recipientId,
        NotificationType type,
        string title,
        string message,
        Guid? referenceId = null,
        CancellationToken ct = default)
    {
        // 1. Load recipient contact info (no tracking — side effects run after main command commit)
        var recipient = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == recipientId)
            .Select(u => new { u.Email, u.FcmDeviceToken })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (recipient is null)
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
        changeTrackerCleaner.ClearTrackedEntities();

        // 6. Web Dashboard Real-Time Push (SignalR) — sync, fast
        try
        {
            var payload = new RealTimeNotificationPayload(
                notification.Id,
                notification.Type,
                notification.Title,
                notification.Message,
                notification.ReferenceId,
                notification.CreatedAt);

            await hubContext.Clients.User(recipientId.ToString()).ReceiveNotification(payload);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SignalR notification failed for user {UserId}", recipientId);
        }

        // 7. Enqueue FCM + SMTP (background — do not block HTTP)
        dispatchScheduler.Enqueue(notification.Id);

        logger.LogInformation(
            "Notification persisted: {Type} to user {UserId} via {Channel}; channel dispatch enqueued",
            type, recipientId, channel);
    }
}
