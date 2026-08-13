using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;

namespace Greenlens.Domain.Entities;

/// <summary>
/// InspectionReport — sub-process running parallel to Report umbrella lifecycle.
/// Created by LEO when a violation is identified during verification.
/// Inspection Team handles penalty enforcement for ALL pollution types.
/// </summary>
/// <remarks>
/// Implements: BR-INS-001 → BR-INS-031.
/// State machine: Draft → PenaltyIssued → (Paid / PartiallyPaid / Overdue) → Closed.
/// Also: Draft → ClosedNoViolation (BR-INS-013).
/// </remarks>
public sealed class InspectionReport : SoftDeletableEntity
{
    private InspectionReport() { } // EF Core constructor

    // ── Link to parent Report ──
    public Guid ReportId { get; private set; }

    // ── Status ──
    public InspectionStatus Status { get; private set; } = InspectionStatus.Draft;

    // ── Team assignment ──
    /// <summary>Inspection Team assigned to this report. One team per InspectionReport.</summary>
    public Guid? AssignedTeamId { get; private set; }

    // ── Violation details (filled by Inspection Team after field investigation) ──
    public string? ViolationDescription { get; private set; }
    public string? ViolatorName { get; private set; }
    public string? ViolatorAddress { get; private set; }
    /// <summary>Tax ID or business registration number of the violator.</summary>
    public string? ViolatorIdentity { get; private set; }

    // ── Linked violating entity (BR-INS-010, BR-INS-022) ──
    /// <summary>FK to ViolatingEntity for repeat offender detection. Null until linked.</summary>
    public Guid? ViolatingEntityId { get; private set; }

    // ── Violation level & Penalty ──
    /// <summary>BR-INS-011: Minor / Moderate / Severe / Critical.</summary>
    public ViolationLevel? ViolationLevel { get; private set; }
    public decimal? PenaltyAmount { get; private set; }
    public string? PenaltyDecisionNumber { get; private set; }
    public DateTime? PenaltyIssuedAt { get; private set; }
    public DateTime? PenaltyDueDate { get; private set; }
    public decimal? PaidAmount { get; private set; }
    /// <summary>Additional penalty measures (e.g. temporary suspension).</summary>
    public string? AdditionalPenaltyMeasures { get; private set; }

    // ── Repeat offender flag (BR-INS-022) ──
    public bool IsRepeatOffender { get; private set; }

    // ── Officers ──
    /// <summary>LEO who created this inspection report.</summary>
    public Guid CreatedByOfficerId { get; private set; }
    /// <summary>Inspector (Team Leader) who issued the penalty decision.</summary>
    public Guid? IssuedByInspectorId { get; private set; }

    // ── Lifecycle timestamps ──
    public DateTime? ClosedAt { get; private set; }
    public string? ClosedReason { get; private set; }

    // ── SLA (BR-INS-030) ──
    /// <summary>SLA deadline computed from report severity at creation time.</summary>
    public DateTime? SlaInspectionDueAt { get; private set; }
    /// <summary>Flagged by SlaBreachInspectionJob when deadline exceeded.</summary>
    public bool SlaInspectionBreached { get; private set; }

    // ── Check-in (legacy BR-INS-004 — deprecated, use ConfirmArrival) ──
    public DateTime? CheckedInAt { get; private set; }
    public decimal? CheckedInLatitude { get; private set; }
    public decimal? CheckedInLongitude { get; private set; }
    public string? CheckedInNote { get; private set; }

    // ── Progress tracking (legacy BR-INS-031 — deprecated) ──
    public int ProgressPercent { get; private set; }
    public string? ProgressNote { get; private set; }
    public DateTime? ProgressUpdatedAt { get; private set; }

    // ── Checklist workflow (BR-INS-033) ──
    public DateTime? AcceptedAt { get; private set; }
    public Guid? AcceptedByUserId { get; private set; }
    public DateTime? ArrivalConfirmedAt { get; private set; }
    public decimal? ArrivalLatitude { get; private set; }
    public decimal? ArrivalLongitude { get; private set; }
    public string? ArrivalNote { get; private set; }
    public DateTime? FieldInvestigationSubmittedAt { get; private set; }
    public Guid? FieldInvestigationSubmittedByUserId { get; private set; }

    // ── Navigation ──
    public Report? Report { get; private set; }
    public ICollection<InspectionEvidence> Evidences { get; private set; } = [];
    public User? CreatedByOfficer { get; private set; }
    public User? IssuedByInspector { get; private set; }
    public EnvironmentalTeam? AssignedTeam { get; private set; }
    public ViolatingEntity? ViolatingEntity { get; private set; }
    public ICollection<PenaltyPayment> Payments { get; private set; } = [];

    // ────────────────────────────────────────────────────
    // Factory
    // ────────────────────────────────────────────────────

    /// <summary>
    /// BR-INS-001, BR-OFF-005: LEO raises an InspectionReport linked to a verified Report.
    /// SLA deadline computed from report severity (BR-INS-030).
    /// </summary>
    public static InspectionReport Create(
        Guid reportId,
        Guid leoId,
        Severity reportSeverity,
        Guid? assignedTeamId = null,
        string? violationDescription = null,
        string? violatorName = null,
        string? violatorAddress = null,
        string? violatorIdentity = null)
    {
        return new InspectionReport
        {
            ReportId = reportId,
            CreatedByOfficerId = leoId,
            AssignedTeamId = assignedTeamId,
            Status = InspectionStatus.Draft,
            ViolationDescription = violationDescription,
            ViolatorName = violatorName,
            ViolatorAddress = violatorAddress,
            ViolatorIdentity = violatorIdentity,
            SlaInspectionDueAt = ComputeSlaDeadline(reportSeverity),
            CreatedAt = DateTime.UtcNow
        };
    }

    // ────────────────────────────────────────────────────
    // State machine transitions (return Result)
    // ────────────────────────────────────────────────────

    /// <summary>Assign an Inspection Team to this report.</summary>
    public Result AssignTeam(Guid teamId)
    {
        if (Status is not (InspectionStatus.Draft or InspectionStatus.InProgress))
            return Result.Failure(new Error(
                "INSPECTION_INVALID_STATE",
                $"Cannot assign team in status {Status}. Must be Draft or InProgress.",
                ErrorType.BusinessRule));

        AssignedTeamId = teamId;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// BR-INS-033: Team accepts assigned task. Draft → InProgress.
    /// </summary>
    public Result AcceptTask(Guid userId)
    {
        if (Status != InspectionStatus.Draft)
            return Result.Failure(new Error(
                "INSPECTION_INVALID_STATE",
                $"Cannot accept task from status {Status}. Must be Draft.",
                ErrorType.BusinessRule));

        if (AssignedTeamId is null)
            return Result.Failure(new Error(
                "INSPECTION_NO_TEAM",
                "Cannot accept task without an assigned team.",
                ErrorType.BusinessRule));

        Status = InspectionStatus.InProgress;
        AcceptedAt = DateTime.UtcNow;
        AcceptedByUserId = userId;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// BR-INS-033: Optional soft GPS arrival confirmation while InProgress.
    /// Distance validation (note required when &gt; 200m) is done in the handler.
    /// </summary>
    public Result ConfirmArrival(decimal latitude, decimal longitude, string? note = null)
    {
        if (Status != InspectionStatus.InProgress)
            return Result.Failure(new Error(
                "INSPECTION_INVALID_STATE",
                $"Cannot confirm arrival from status {Status}. Must be InProgress.",
                ErrorType.BusinessRule));

        ArrivalConfirmedAt = DateTime.UtcNow;
        ArrivalLatitude = latitude;
        ArrivalLongitude = longitude;
        ArrivalNote = note;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// BR-INS-033: Team Leader submits completed field investigation checklist.
    /// </summary>
    public Result SubmitFieldInvestigation(Guid leaderId)
    {
        if (Status != InspectionStatus.InProgress)
            return Result.Failure(new Error(
                "INSPECTION_INVALID_STATE",
                $"Cannot submit field report from status {Status}. Must be InProgress.",
                ErrorType.BusinessRule));

        if (FieldInvestigationSubmittedAt.HasValue)
            return Result.Failure(new Error(
                "INSPECTION_FIELD_REPORT_ALREADY_SUBMITTED",
                "Field investigation report was already submitted.",
                ErrorType.BusinessRule));

        FieldInvestigationSubmittedAt = DateTime.UtcNow;
        FieldInvestigationSubmittedByUserId = leaderId;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// BR-INS-004 (legacy): Team checks in at the site. Draft → InProgress.
    /// Deprecated — use AcceptTask + ConfirmArrival.
    /// </summary>
    public Result CheckIn(decimal latitude, decimal longitude, string? note = null)
    {
        if (Status != InspectionStatus.Draft)
            return Result.Failure(new Error(
                "INSPECTION_INVALID_STATE",
                $"Cannot check in from status {Status}. Must be Draft.",
                ErrorType.BusinessRule));

        if (AssignedTeamId is null)
            return Result.Failure(new Error(
                "INSPECTION_NO_TEAM",
                "Cannot check in without an assigned team.",
                ErrorType.BusinessRule));

        Status = InspectionStatus.InProgress;
        CheckedInAt = DateTime.UtcNow;
        CheckedInLatitude = latitude;
        CheckedInLongitude = longitude;
        CheckedInNote = note;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>BR-INS-031: Update progress while InProgress.</summary>
    public Result UpdateProgress(int percent, string? note)
    {
        if (Status != InspectionStatus.InProgress)
            return Result.Failure(new Error(
                "INSPECTION_INVALID_STATE",
                $"Cannot update progress from status {Status}. Must be InProgress.",
                ErrorType.BusinessRule));

        if (percent < 0 || percent > 100)
            return Result.Failure(new Error(
                "INVALID_PROGRESS",
                "Progress must be 0–100.",
                ErrorType.Validation));

        if (percent < ProgressPercent)
            return Result.Failure(new Error(
                "INSPECTION_PROGRESS_CANNOT_DECREASE",
                $"Không thể cập nhật tiến độ thấp hơn {ProgressPercent}% đã lưu trước đó.",
                ErrorType.Validation));

        ProgressPercent = percent;
        ProgressNote = note;
        ProgressUpdatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// BR-INS-012: Inspector (Team Leader) issues penalty.
    /// Draft → PenaltyIssued.
    /// </summary>
    public Result IssuePenalty(
        Guid inspectorId,
        ViolationLevel violationLevel,
        decimal amount,
        string decisionNumber,
        DateTime dueDate,
        string? additionalMeasures = null,
        bool isRepeatOffender = false)
    {
        if (Status != InspectionStatus.InProgress)
            return Result.Failure(new Error(
                "INSPECTION_INVALID_STATE",
                $"Cannot issue penalty from status {Status}. Must be InProgress.",
                ErrorType.BusinessRule));

        if (!FieldInvestigationSubmittedAt.HasValue)
            return Result.Failure(new Error(
                "INSPECTION_FIELD_REPORT_REQUIRED",
                "Field investigation report must be submitted before issuing penalty (BR-INS-033).",
                ErrorType.BusinessRule));

        if (amount <= 0)
            return Result.Failure(new Error(
                "INVALID_PENALTY_AMOUNT",
                "Penalty amount must be greater than zero.",
                ErrorType.Validation));

        Status = InspectionStatus.PenaltyIssued;
        IssuedByInspectorId = inspectorId;
        ViolationLevel = violationLevel;
        PenaltyAmount = amount;
        PenaltyDecisionNumber = decisionNumber;
        PenaltyIssuedAt = DateTime.UtcNow;
        PenaltyDueDate = dueDate;
        AdditionalPenaltyMeasures = additionalMeasures;
        IsRepeatOffender = isRepeatOffender;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new PenaltyIssuedEvent(
            Id,
            ReportId,
            amount,
            decisionNumber,
            violationLevel));

        return Result.Success();
    }

    /// <summary>
    /// BR-INS-020: Record payment via PenaltyPayment entity.
    /// PenaltyIssued/Overdue → Paid. Chỉ nhận đúng 1 lần, đủ toàn bộ số tiền còn lại
    /// (không chấp nhận nộp thiếu hoặc dư) — không còn hỗ trợ nộp từng phần.
    /// </summary>
    public Result RecordPayment(PenaltyPayment payment)
    {
        if (Status is not (InspectionStatus.PenaltyIssued or InspectionStatus.Overdue))
            return Result.Failure(new Error(
                "INSPECTION_INVALID_STATE",
                $"Cannot record payment from status {Status}.",
                ErrorType.BusinessRule));

        var remaining = PenaltyAmount - (PaidAmount ?? 0);
        if (payment.Amount != remaining)
            return Result.Failure(new Error(
                "PAYMENT_AMOUNT_MUST_MATCH_REMAINING",
                $"Số tiền nộp phải đúng bằng số tiền còn lại ({remaining:N0}).",
                ErrorType.Validation));

        Payments.Add(payment);
        PaidAmount = (PaidAmount ?? 0) + payment.Amount;
        Status = InspectionStatus.Paid;

        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Reverse a payment (e.g. soft delete). Adjusts PaidAmount and Status.
    /// </summary>
    public Result RemovePayment(PenaltyPayment payment)
    {
        if (!Payments.Contains(payment))
            return Result.Failure(new Error(
                "INSPECTION_PAYMENT_NOT_FOUND",
                "Payment does not belong to this inspection report.",
                ErrorType.NotFound));

        Payments.Remove(payment);
        PaidAmount = Math.Max(0, (PaidAmount ?? 0) - payment.Amount);

        if (PaidAmount == 0)
        {
            Status = DateTime.UtcNow > PenaltyDueDate ? InspectionStatus.Overdue : InspectionStatus.PenaltyIssued;
        }
        else if (PaidAmount < PenaltyAmount)
        {
            Status = InspectionStatus.PartiallyPaid;
        }
        else
        {
            Status = InspectionStatus.Paid;
        }

        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// BR-INS-021: Payment overdue. PenaltyIssued/PartiallyPaid → Overdue (set by background job).
    /// </summary>
    public Result MarkOverdue()
    {
        if (Status is not (InspectionStatus.PenaltyIssued or InspectionStatus.PartiallyPaid))
            return Result.Failure(new Error(
                "INSPECTION_INVALID_STATE",
                $"Cannot mark as overdue from status {Status}.",
                ErrorType.BusinessRule));

        Status = InspectionStatus.Overdue;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>Close the inspection. Paid → Closed.</summary>
    public Result Close(string? reason = null)
    {
        if (Status != InspectionStatus.Paid)
            return Result.Failure(new Error(
                "INSPECTION_INVALID_STATE",
                $"Cannot close from status {Status}. Must be Paid.",
                ErrorType.BusinessRule));

        Status = InspectionStatus.Closed;
        ClosedAt = DateTime.UtcNow;
        ClosedReason = reason;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>BR-INS-013: Close without violation found. InProgress → ClosedNoViolation.</summary>
    public Result CloseNoViolation(string reason)
    {
        if (Status != InspectionStatus.InProgress)
            return Result.Failure(new Error(
                "INSPECTION_INVALID_STATE",
                $"Cannot close from status {Status}. Must be InProgress.",
                ErrorType.BusinessRule));

        if (!FieldInvestigationSubmittedAt.HasValue)
            return Result.Failure(new Error(
                "INSPECTION_FIELD_REPORT_REQUIRED",
                "Field investigation report must be submitted before closing (BR-INS-033).",
                ErrorType.BusinessRule));

        if (string.IsNullOrWhiteSpace(reason) || reason.Length < 50)
            return Result.Failure(new Error(
                "REASON_TOO_SHORT",
                "Close reason must be at least 50 characters (BR-INS-013).",
                ErrorType.Validation));

        Status = InspectionStatus.ClosedNoViolation;
        ClosedAt = DateTime.UtcNow;
        ClosedReason = reason;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// BR-INS-030: System auto-close when SLA expires without inspector conclusion.
    /// Does not require field investigation submission.
    /// </summary>
    public Result ForceCloseNoViolation(string reason)
    {
        if (Status is InspectionStatus.Closed
            or InspectionStatus.ClosedNoViolation
            or InspectionStatus.PenaltyIssued
            or InspectionStatus.PartiallyPaid
            or InspectionStatus.Overdue
            or InspectionStatus.Paid)
        {
            return Result.Failure(new Error(
                "INSPECTION_INVALID_STATE",
                $"Cannot force-close from status {Status}.",
                ErrorType.BusinessRule));
        }

        if (string.IsNullOrWhiteSpace(reason) || reason.Length < 50)
            return Result.Failure(new Error(
                "REASON_TOO_SHORT",
                "Close reason must be at least 50 characters (BR-INS-013).",
                ErrorType.Validation));

        Status = InspectionStatus.ClosedNoViolation;
        ClosedAt = DateTime.UtcNow;
        ClosedReason = reason;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>Update violation details while in Draft status. BR-INS-010.</summary>
    public Result UpdateDetails(
        string? violationDescription = null,
        string? violatorName = null,
        string? violatorAddress = null,
        string? violatorIdentity = null)
    {
        if (Status is not (InspectionStatus.Draft or InspectionStatus.InProgress))
            return Result.Failure(new Error(
                "INSPECTION_INVALID_STATE",
                $"Cannot update details in status {Status}. Must be Draft or InProgress.",
                ErrorType.BusinessRule));

        if (violationDescription is not null) ViolationDescription = violationDescription;
        if (violatorName is not null) ViolatorName = violatorName;
        if (violatorAddress is not null) ViolatorAddress = violatorAddress;
        if (violatorIdentity is not null) ViolatorIdentity = violatorIdentity;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    // ────────────────────────────────────────────────────
    // Helpers
    // ────────────────────────────────────────────────────

    /// <summary>BR-INS-030: SLA deadline based on report severity.</summary>
    private static DateTime ComputeSlaDeadline(Severity severity) => severity switch
    {
        Severity.Critical => DateTime.UtcNow.AddDays(3),
        Severity.High => DateTime.UtcNow.AddDays(5),
        Severity.Medium => DateTime.UtcNow.AddDays(7),
        Severity.Low => DateTime.UtcNow.AddDays(10),
        _ => DateTime.UtcNow.AddDays(7)
    };

    /// <summary>BR-INS-030: Flag by SlaBreachInspectionJob when deadline exceeded.</summary>
    public void MarkSlaInspectionBreached()
    {
        SlaInspectionBreached = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>BR-INS-003: Clear team assignment for LEO re-assignment after decline.</summary>
    public void ClearTeam()
    {
        AssignedTeamId = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>BR-INS-010/022: Link a ViolatingEntity for repeat offender tracking.</summary>
    public Result LinkViolatingEntity(Guid violatingEntityId)
    {
        if (Status is InspectionStatus.Closed or InspectionStatus.ClosedNoViolation)
            return Result.Failure(new Error(
                "INSPECTION_INVALID_STATE",
                $"Cannot link violating entity in status {Status}.",
                ErrorType.BusinessRule));

        ViolatingEntityId = violatingEntityId;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }
}
