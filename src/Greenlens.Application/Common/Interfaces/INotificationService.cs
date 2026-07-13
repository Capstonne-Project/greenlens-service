using Greenlens.Domain.Enums;

namespace Greenlens.Application.Common.Interfaces;

/// <summary>
/// Core notification service — orchestrates preference checks, anti-spam,
/// persistence, and channel dispatch (BR-NTF-001..003).
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Send a notification to a user. Checks preferences, anti-spam limits,
    /// persists the notification, and dispatches to enabled channels.
    /// </summary>
    Task SendRawAsync(
        Guid recipientId,
        NotificationType type,
        string title,
        string message,
        Guid? referenceId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Sends a notification by fetching the published NotificationTemplate for the given type,
    /// and replacing its placeholders with the provided dictionary.
    /// </summary>
    Task SendFromTemplateAsync(
        Guid recipientId,
        NotificationType type,
        Dictionary<string, string> placeholders,
        Guid? referenceId = null,
        CancellationToken ct = default);
}
