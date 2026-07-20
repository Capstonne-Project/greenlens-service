using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;

namespace Greenlens.Domain.Entities;

/// <summary>
/// Per-user, per-notification-type channel preference (BR-NTF-001).
/// User can toggle push and email independently for each notification type.
/// </summary>
public sealed class NotificationPreference : AuditableEntity
{
    private NotificationPreference() { }

    public Guid UserId { get; private set; }
    public NotificationType Type { get; private set; }
    public bool PushEnabled { get; private set; } = true;
    public bool EmailEnabled { get; private set; } = true;

    // ── Navigation ──
    public User? User { get; private set; }

    // ────────────────────────────────────────────────────
    // Factory
    // ────────────────────────────────────────────────────

    public static NotificationPreference Create(
        Guid userId,
        NotificationType type,
        bool pushEnabled = true,
        bool emailEnabled = true)
    {
        return new NotificationPreference
        {
            UserId = userId,
            Type = type,
            PushEnabled = pushEnabled,
            EmailEnabled = emailEnabled,
            CreatedAt = DateTime.UtcNow
        };
    }

    // ────────────────────────────────────────────────────
    // Behavior
    // ────────────────────────────────────────────────────

    public void Update(bool pushEnabled, bool emailEnabled)
    {
        PushEnabled = pushEnabled;
        EmailEnabled = emailEnabled;
        UpdatedAt = DateTime.UtcNow;
    }
}
