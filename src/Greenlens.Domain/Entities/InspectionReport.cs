using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;

namespace Greenlens.Domain.Entities;

/// <summary>
/// InspectionReport — sub-process running parallel to Report umbrella lifecycle.
/// Created by LEO when a violation is identified during verification.
/// Inspection Team handles penalty enforcement for ALL pollution types.
/// </summary>
/// <remarks>
/// Implements: BR-INS-001 → BR-INS-013.
/// State machine: Draft → PenaltyIssued → (Paid / PartiallyPaid / Overdue) → Closed.
/// </remarks>
public sealed class InspectionReport : AuditableEntity
{
    private InspectionReport() { } // EF Core constructor

    // ── Link to parent Report ──
    public Guid ReportId { get; private set; }

    // ── Status ──
    public InspectionStatus Status { get; private set; } = InspectionStatus.Draft;

    // ── Violation details ──
    public string? ViolationDescription { get; private set; }
    public string? ViolatorName { get; private set; }
    public string? ViolatorAddress { get; private set; }
    public string? ViolatorIdentity { get; private set; }

    // ── Penalty ──
    public decimal? PenaltyAmount { get; private set; }
    public string? PenaltyDecisionNumber { get; private set; }
    public DateTime? PenaltyIssuedAt { get; private set; }
    public DateTime? PenaltyDueDate { get; private set; }
    public decimal? PaidAmount { get; private set; }

    // ── Officers ──
    /// <summary>LEO who created this inspection report.</summary>
    public Guid CreatedByOfficerId { get; private set; }
    /// <summary>Inspector who issued the penalty decision.</summary>
    public Guid? IssuedByInspectorId { get; private set; }

    // ── Lifecycle timestamps ──
    public DateTime? ClosedAt { get; private set; }
    public string? ClosedReason { get; private set; }

    // ── Navigation ──
    public Report? Report { get; private set; }
    public User? CreatedByOfficer { get; private set; }
    public User? IssuedByInspector { get; private set; }

    // ────────────────────────────────────────────────────
    // Factory
    // ────────────────────────────────────────────────────

    /// <summary>BR-INS-001: LEO raises an InspectionReport linked to a verified Report.</summary>
    public static InspectionReport Create(
        Guid reportId,
        Guid leoId,
        string? violationDescription = null,
        string? violatorName = null,
        string? violatorAddress = null,
        string? violatorIdentity = null)
    {
        return new InspectionReport
        {
            ReportId = reportId,
            CreatedByOfficerId = leoId,
            Status = InspectionStatus.Draft,
            ViolationDescription = violationDescription,
            ViolatorName = violatorName,
            ViolatorAddress = violatorAddress,
            ViolatorIdentity = violatorIdentity,
            CreatedAt = DateTime.UtcNow
        };
    }

    // ────────────────────────────────────────────────────
    // State machine transitions
    // ────────────────────────────────────────────────────

    /// <summary>Inspector issues penalty. Draft → PenaltyIssued. BR-INS-012.</summary>
    public void IssuePenalty(
        Guid inspectorId,
        decimal amount,
        string decisionNumber,
        DateTime dueDate)
    {
        EnsureStatus(InspectionStatus.Draft);

        Status = InspectionStatus.PenaltyIssued;
        IssuedByInspectorId = inspectorId;
        PenaltyAmount = amount;
        PenaltyDecisionNumber = decisionNumber;
        PenaltyIssuedAt = DateTime.UtcNow;
        PenaltyDueDate = dueDate;
    }

    /// <summary>Full payment received. PenaltyIssued/PartiallyPaid → Paid.</summary>
    public void MarkPaid(decimal paidAmount)
    {
        if (Status is not (InspectionStatus.PenaltyIssued or InspectionStatus.PartiallyPaid))
            throw new InvalidOperationException(
                $"Cannot mark as paid from status {Status}.");

        PaidAmount = (PaidAmount ?? 0) + paidAmount;

        Status = PaidAmount >= PenaltyAmount
            ? InspectionStatus.Paid
            : InspectionStatus.PartiallyPaid;
    }

    /// <summary>Payment overdue. PenaltyIssued/PartiallyPaid → Overdue (set by background job).</summary>
    public void MarkOverdue()
    {
        if (Status is not (InspectionStatus.PenaltyIssued or InspectionStatus.PartiallyPaid))
            throw new InvalidOperationException(
                $"Cannot mark as overdue from status {Status}.");

        Status = InspectionStatus.Overdue;
    }

    /// <summary>Close the inspection. Paid/Overdue → Closed.</summary>
    public void Close(string? reason = null)
    {
        if (Status is not (InspectionStatus.Paid or InspectionStatus.Overdue))
            throw new InvalidOperationException(
                $"Cannot close from status {Status}. Must be Paid or Overdue.");

        Status = InspectionStatus.Closed;
        ClosedAt = DateTime.UtcNow;
        ClosedReason = reason;
    }

    /// <summary>Close without violation found. Draft → Closed. BR-INS-013.</summary>
    public void CloseNoViolation(string reason)
    {
        EnsureStatus(InspectionStatus.Draft);

        Status = InspectionStatus.Closed;
        ClosedAt = DateTime.UtcNow;
        ClosedReason = reason;
    }

    /// <summary>Update violation details while in Draft status.</summary>
    public void UpdateDetails(
        string? violationDescription = null,
        string? violatorName = null,
        string? violatorAddress = null,
        string? violatorIdentity = null)
    {
        EnsureStatus(InspectionStatus.Draft);

        if (violationDescription is not null) ViolationDescription = violationDescription;
        if (violatorName is not null) ViolatorName = violatorName;
        if (violatorAddress is not null) ViolatorAddress = violatorAddress;
        if (violatorIdentity is not null) ViolatorIdentity = violatorIdentity;
        UpdatedAt = DateTime.UtcNow;
    }

    // ────────────────────────────────────────────────────
    // Helpers
    // ────────────────────────────────────────────────────

    private void EnsureStatus(InspectionStatus expected)
    {
        if (Status != expected)
            throw new InvalidOperationException(
                $"Invalid state transition: expected {expected} but current is {Status}.");
    }
}
