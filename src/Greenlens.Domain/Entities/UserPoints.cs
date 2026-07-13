using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;

namespace Greenlens.Domain.Entities;

/// <summary>
/// Gamification aggregate root — one per Citizen user.
/// Tracks total points, level, lock status, and point transaction history.
/// </summary>
/// <remarks>
/// Implements: BR-GAM-001 (points), BR-GAM-003 (levels), BR-GAM-006 (lock).
/// Separated from User to respect SRP — gamification can be toggled independently.
/// </remarks>
public sealed class UserPoints : SoftDeletableEntity
{
    private readonly List<PointTransaction> _transactions = [];

    private UserPoints() { } // EF Core

    public Guid UserId { get; private set; }

    /// <summary>Running total. Always == sum of Transactions.Points.</summary>
    public int TotalPoints { get; private set; }

    /// <summary>BR-GAM-006: When true, no points can be awarded.</summary>
    public bool IsLocked { get; private set; }

    /// <summary>BR-GAM-006: Lock expires at this time. Null if not locked.</summary>
    public DateTime? LockedUntil { get; private set; }
    public string? LockedReason { get; private set; }

    // ── Navigation ──
    public User? User { get; private set; }
    public IReadOnlyCollection<PointTransaction> Transactions => _transactions.AsReadOnly();

    // ────────────────────────────────────────────────────
    // Factory
    // ────────────────────────────────────────────────────

    public static UserPoints Create(Guid userId)
    {
        return new UserPoints
        {
            UserId = userId,
            TotalPoints = 0,
            IsLocked = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    // ────────────────────────────────────────────────────
    // BR-GAM-003: Level computation
    // ────────────────────────────────────────────────────

    /// <summary>
    /// BR-GAM-003: L1 (0–99), L2 (100–499), L3 (500–1499), L4 (1500–4999), L5 (≥5000).
    /// </summary>
    public int Level => TotalPoints switch
    {
        >= 5000 => 5,
        >= 1500 => 4,
        >= 500 => 3,
        >= 100 => 2,
        _ => 1
    };

    // ────────────────────────────────────────────────────
    // BR-GAM-001: Award / deduct points
    // ────────────────────────────────────────────────────

    /// <summary>
    /// Award points for a report action. Returns the created transaction.
    /// Idempotent: if a transaction with the same ReportId + Reason already exists, returns null.
    /// </summary>
    public PointTransaction? AwardPoints(int points, PointReason reason, Guid? reportId)
    {
        if (IsLocked && LockedUntil > DateTime.UtcNow)
            return null; // BR-GAM-006: locked, silently skip

        // Auto-unlock if lock has expired
        if (IsLocked && LockedUntil <= DateTime.UtcNow)
        {
            IsLocked = false;
            LockedUntil = null;
            LockedReason = null;
        }

        // Idempotent check: same report + reason = already awarded
        if (reportId.HasValue &&
            _transactions.Any(t => t.ReportId == reportId && t.Reason == reason))
        {
            return null;
        }

        var previousLevel = Level;
        var tx = PointTransaction.Create(Id, points, reason, reportId);
        _transactions.Add(tx);
        TotalPoints += points;

        // Floor at 0 — don't allow negative total
        if (TotalPoints < 0) TotalPoints = 0;

        // Raise event if level changed
        if (Level > previousLevel)
        {
            AddDomainEvent(new LevelUpEvent(UserId, previousLevel, Level));
        }

        return tx;
    }

    // ────────────────────────────────────────────────────
    // BR-GAM-006: Lock / unlock
    // ────────────────────────────────────────────────────

    /// <summary>
    /// BR-GAM-006: Lock gamification for fraud. Deducts all points from the batch period
    /// and prevents further point accumulation for the specified duration.
    /// </summary>
    public int Lock(string reason, int lockDays = 30)
    {
        IsLocked = true;
        LockedUntil = DateTime.UtcNow.AddDays(lockDays);
        LockedReason = reason;

        // Deduct all accumulated points as fraud penalty
        var penaltyPoints = -TotalPoints;
        if (penaltyPoints < 0) // only deduct if user has points
        {
            var tx = PointTransaction.Create(Id, penaltyPoints, PointReason.FraudPenalty, null);
            _transactions.Add(tx);
            TotalPoints = 0;
        }

        return penaltyPoints;
    }

    public void Unlock()
    {
        IsLocked = false;
        LockedUntil = null;
        LockedReason = null;
    }
}

/// <summary>Raised when a user levels up (BR-GAM-003).</summary>
public sealed record LevelUpEvent(Guid UserId, int PreviousLevel, int NewLevel) : IDomainEvent;
