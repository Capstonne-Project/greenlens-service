using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;

namespace Greenlens.Domain.Entities;

/// <summary>
/// A single notification sent to a user. Supports read/unread tracking.
/// </summary>
/// <remarks>
/// Implements: BR-NTF-001 (channels), BR-NTF-002 (event triggers).
/// Anti-spam (BR-NTF-003) is enforced by NotificationService before creating this entity.
/// </remarks>
public sealed class Notification : AuditableEntity
{
    private Notification() { }

    public Guid RecipientId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = default!;
    public string Message { get; private set; } = default!;

    /// <summary>Optional reference to the related entity (e.g. ReportId, BadgeId).</summary>
    public Guid? ReferenceId { get; private set; }

    public NotificationChannel Channel { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }

    /// <summary>UTC timestamp when FCM push was successfully dispatched (idempotency for Hangfire retry).</summary>
    public DateTime? PushDispatchedAt { get; private set; }

    /// <summary>UTC timestamp when SMTP email was successfully dispatched (idempotency for Hangfire retry).</summary>
    public DateTime? EmailDispatchedAt { get; private set; }

    // ── Navigation ──
    public User? Recipient { get; private set; }

    // ────────────────────────────────────────────────────
    // Factory
    // ────────────────────────────────────────────────────

    public static Notification Create(
        Guid recipientId,
        NotificationType type,
        string title,
        string message,
        NotificationChannel channel = NotificationChannel.Both,
        Guid? referenceId = null)
    {
        return new Notification
        {
            RecipientId = recipientId,
            Type = type,
            Title = title,
            Message = message,
            Channel = channel,
            ReferenceId = referenceId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    // ────────────────────────────────────────────────────
    // Behavior
    // ────────────────────────────────────────────────────

    public void MarkAsRead()
    {
        if (IsRead) return;
        IsRead = true;
        ReadAt = DateTime.UtcNow;
    }

    public void MarkPushDispatched() => PushDispatchedAt = DateTime.UtcNow;

    public void MarkEmailDispatched() => EmailDispatchedAt = DateTime.UtcNow;
}
