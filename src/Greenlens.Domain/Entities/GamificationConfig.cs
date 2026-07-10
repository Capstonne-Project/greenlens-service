using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;

namespace Greenlens.Domain.Entities;

/// <summary>
/// Admin-configurable point amounts for each gamification action.
/// One row per PointReason — seeded with defaults, admin can update.
/// </summary>
/// <remarks>Implements: BR-ADM-005.</remarks>
public sealed class GamificationConfig : AuditableEntity
{
    private GamificationConfig() { }

    /// <summary>The action type this config applies to.</summary>
    public PointReason ActionType { get; private set; }

    /// <summary>Points awarded (positive) or deducted (negative) for this action.</summary>
    public int Points { get; private set; }

    /// <summary>Admin can disable an action from awarding points.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Human-readable description of this action.</summary>
    public string Description { get; private set; } = default!;

    public static GamificationConfig Create(PointReason actionType, int points, string description)
    {
        return new GamificationConfig
        {
            ActionType = actionType,
            Points = points,
            Description = description,
            IsActive = true
        };
    }

    public void Update(int points, string description, bool isActive)
    {
        Points = points;
        Description = description;
        IsActive = isActive;
    }
}
