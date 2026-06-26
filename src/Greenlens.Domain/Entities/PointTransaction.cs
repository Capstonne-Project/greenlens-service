using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;

namespace Greenlens.Domain.Entities;

/// <summary>
/// Tracks a single point change for a user. Immutable after creation.
/// </summary>
/// <remarks>Implements: BR-GAM-001.</remarks>
public sealed class PointTransaction : BaseEntity
{
    private PointTransaction() { } // EF Core

    public Guid UserPointsId { get; private set; }
    public int Points { get; private set; }
    public PointReason Reason { get; private set; }

    /// <summary>The report that triggered this transaction. Null for fraud penalties.</summary>
    public Guid? ReportId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    // ── Navigation ──
    public UserPoints? UserPointsAggregate { get; private set; }

    internal static PointTransaction Create(
        Guid userPointsId, int points, PointReason reason, Guid? reportId)
    {
        return new PointTransaction
        {
            UserPointsId = userPointsId,
            Points = points,
            Reason = reason,
            ReportId = reportId,
            CreatedAt = DateTime.UtcNow
        };
    }
}
