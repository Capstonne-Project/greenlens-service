namespace Greenlens.Domain.Enums;

/// <summary>
/// Report lifecycle statuses (v1.2 — two parallel flows).
/// Cleanup flow:    Submitted → Verified → Assigned → InProgress → Resolved → Closed.
/// Inspection flow: Submitted → Verified → Assigned → InProgress → PenaltyIssued → Closed.
/// Decline path:    Assigned → Verified (team declines, officer re-assigns).
/// </summary>
/// <remarks>Implements: BR-REP-020.</remarks>
public enum ReportStatus
{
    Submitted,
    Verified,
    Assigned,
    InProgress,
    Resolved,
    Closed,
    Rejected,
    Duplicate,
    PenaltyIssued,
    ClosedNoViolation
}
