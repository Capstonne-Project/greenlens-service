using Greenlens.Domain.Common;

namespace Greenlens.Domain.Entities;

/// <summary>
/// Join entity: User ↔ Badge (N-N). Records when a badge was earned.
/// </summary>
/// <remarks>Implements: BR-GAM-004.</remarks>
public sealed class UserBadge : BaseEntity
{
    private UserBadge() { } // EF Core

    public Guid UserId { get; private set; }
    public Guid BadgeId { get; private set; }
    public DateTime AwardedAt { get; private set; }

    /// <summary>The report that triggered badge award (context). Null for point-threshold badges.</summary>
    public Guid? ReportId { get; private set; }

    // ── Navigation ──
    public User? User { get; private set; }
    public Badge? Badge { get; private set; }

    public static UserBadge Create(Guid userId, Guid badgeId, Guid? reportId = null)
    {
        return new UserBadge
        {
            UserId = userId,
            BadgeId = badgeId,
            AwardedAt = DateTime.UtcNow,
            ReportId = reportId
        };
    }
}
