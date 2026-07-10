using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;

namespace Greenlens.Domain.Entities;

/// <summary>
/// Configurable penalty amount range for each violation level per pollution category.
/// Admin updates when regulations change; existing issued decisions are unaffected.
/// </summary>
/// <remarks>
/// Implements: BR-ADM-008 (penalty framework), BR-INS-011 (violation levels).
/// </remarks>
public sealed class PenaltyFramework : AuditableEntity
{
    private PenaltyFramework() { }

    public Guid CategoryId { get; private set; }
    public ViolationLevel ViolationLevel { get; private set; }

    /// <summary>Minimum fine amount in VND.</summary>
    public decimal MinAmount { get; private set; }

    /// <summary>Maximum fine amount in VND.</summary>
    public decimal MaxAmount { get; private set; }

    public string Currency { get; private set; } = "VND";

    /// <summary>When this framework entry becomes effective.</summary>
    public DateTime EffectiveFrom { get; private set; }

    /// <summary>Optional end date. Null = no expiry (current).</summary>
    public DateTime? EffectiveTo { get; private set; }

    public bool IsActive { get; private set; } = true;

    // ── Navigation ──
    public PollutionCategory? Category { get; private set; }

    // ────────────────────────────────────────────────────
    // Factory
    // ────────────────────────────────────────────────────

    public static PenaltyFramework Create(
        Guid categoryId,
        ViolationLevel violationLevel,
        decimal minAmount,
        decimal maxAmount,
        DateTime effectiveFrom,
        DateTime? effectiveTo = null)
    {
        return new PenaltyFramework
        {
            CategoryId = categoryId,
            ViolationLevel = violationLevel,
            MinAmount = minAmount,
            MaxAmount = maxAmount,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            IsActive = true
        };
    }

    // ────────────────────────────────────────────────────
    // Behavior
    // ────────────────────────────────────────────────────

    public void Update(decimal minAmount, decimal maxAmount, DateTime effectiveFrom, DateTime? effectiveTo)
    {
        MinAmount = minAmount;
        MaxAmount = maxAmount;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
    }

    public void Deactivate()
    {
        IsActive = false;
        EffectiveTo ??= DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        EffectiveTo = null;
    }
}
