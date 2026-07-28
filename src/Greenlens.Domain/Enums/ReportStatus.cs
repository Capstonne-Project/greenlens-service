namespace Greenlens.Domain.Enums;

/// <summary>
/// Report lifecycle statuses (v3.0 — LEO direct verification model).
/// Submit:   Submitted (auto-routed to LocalOffice by GPS).
/// LEO:      Submitted → Verified → InProgress (assign team or dispatch company) → Resolved → Closed.
/// Reject:   Submitted → Rejected (LEO, reason ≥ 20 chars).
/// Reopen:   Resolved → [LEO approve] → Reopened → InProgress (max 1 time, BR-REP-015).
/// </summary>
/// <remarks>Implements: BR-REP-020, BR-REP-021.</remarks>
public enum ReportStatus
{
    Submitted,
    Verified,
    InProgress,
    Resolved,
    /// <summary>LEO approved citizen reopen request; awaiting team re-assignment.</summary>
    Reopened,
    Closed,
    Rejected,
    Duplicate
}
