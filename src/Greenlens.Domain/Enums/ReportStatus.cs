namespace Greenlens.Domain.Enums;

/// <summary>
/// Report lifecycle statuses (v1.1 — two parallel flows).
/// Cleanup flow: Submitted → Verified → InProgress → Resolved → Closed.
/// Inspection flow: Submitted → Verified → InProgress → PenaltyIssued → Closed.
/// </summary>
/// <remarks>Implements: BR-REP-020.</remarks>
public enum ReportStatus
{
    Submitted,
    Verified,
    InProgress,
    Resolved,
    Closed,
    Rejected,
    Duplicate,
    PenaltyIssued,
    ClosedNoViolation
}
