namespace Greenlens.Domain.Enums;

/// <summary>
/// Lifecycle statuses for InspectionReport (sub-process running parallel to Report).
/// Draft → PenaltyIssued → (Paid / PartiallyPaid / Overdue) → Closed.
/// Also: Draft → ClosedNoViolation (BR-INS-013).
/// </summary>
/// <remarks>Implements: BR-INS-001, BR-INS-013.</remarks>
public enum InspectionStatus
{
    Draft,
    PenaltyIssued,
    Paid,
    PartiallyPaid,
    Overdue,
    Closed,

    /// <summary>No violation found — closed with reason (BR-INS-013).</summary>
    ClosedNoViolation
}
