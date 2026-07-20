namespace Greenlens.Domain.Enums;

/// <summary>
/// Report lifecycle statuses (v3.0 — LEO direct verification model).
/// Submit:   Submitted (auto-routed to LocalOffice by GPS).
/// LEO:      Submitted → Verified → InProgress → Resolved → Closed.
/// Reject:   Submitted → Rejected (LEO, reason ≥ 20 chars).
/// Reopen:   Resolved → InProgress (max 2 times).
/// </summary>
/// <remarks>Implements: BR-REP-020, BR-REP-021.</remarks>
public enum ReportStatus
{
    Submitted,
    Verified,
    InProgress,
    Resolved,
    Closed,
    Rejected,
    Duplicate
}
