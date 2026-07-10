using Greenlens.Domain.Common;

namespace Greenlens.Domain.Entities;

/// <summary>
/// Immutable audit log entry. Captures sensitive actions with full entity diff.
/// </summary>
/// <remarks>
/// Implements: BR-ADM-010. Retention ≥ 12 months (enforced by DataRetentionJob).
/// Does NOT extend AuditableEntity — audit logs are immutable (never updated).
/// </remarks>
public sealed class AuditLog : BaseEntity
{
    private AuditLog() { }

    public Guid UserId { get; private set; }
    public string Action { get; private set; } = default!;
    public string EntityType { get; private set; } = default!;
    public string? EntityId { get; private set; }

    /// <summary>JSON snapshot of the entity BEFORE the action.</summary>
    public string? OldValues { get; private set; }

    /// <summary>JSON snapshot of the command/entity AFTER the action.</summary>
    public string? NewValues { get; private set; }

    public string IpAddress { get; private set; } = default!;
    public string? UserAgent { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // ── Navigation ──
    public User? User { get; private set; }

    // ────────────────────────────────────────────────────
    // Factory
    // ────────────────────────────────────────────────────

    public static AuditLog Create(
        Guid userId,
        string action,
        string entityType,
        string? entityId,
        string? oldValues,
        string? newValues,
        string ipAddress,
        string? userAgent)
    {
        return new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow
        };
    }
}
