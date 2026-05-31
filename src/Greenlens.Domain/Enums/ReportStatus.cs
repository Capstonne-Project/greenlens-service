namespace Greenlens.Domain.Enums;

/// <summary>
/// Report lifecycle statuses (v2.0 — two-tier dispatch model).
/// DEO flow:       Submitted → Verified → Dispatched (DEO sends to ward).
/// LEO flow:       Dispatched → InProgress (LEO assigns team) → Resolved → Closed.
/// Inspection:     Dispatched → InProgress → PenaltyIssued → Closed.
/// Decline path:   InProgress → Dispatched (all teams decline — LEO re-assigns).
/// Re-dispatch:    Dispatched → Dispatched (DEO re-routes to different ward).
/// </summary>
/// <remarks>Implements: BR-REP-020.</remarks>
public enum ReportStatus
{
    Submitted,
    Verified,
    Dispatched,
    Assigned,
    InProgress,
    Resolved,
    Closed,
    Rejected,
    Duplicate,
    PenaltyIssued,
    ClosedNoViolation
}
