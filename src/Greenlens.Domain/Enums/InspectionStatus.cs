namespace Greenlens.Domain.Enums;

/// <summary>
/// Lifecycle statuses for InspectionReport (sub-process running parallel to Report).
/// Draft → PenaltyIssued → (Paid / PartiallyPaid / Overdue) → Closed.
/// </summary>
/// <remarks>Implements: BR-INS-001.</remarks>
public enum InspectionStatus
{
    Draft,
    PenaltyIssued,
    Paid,
    PartiallyPaid,
    Overdue,
    Closed
}
