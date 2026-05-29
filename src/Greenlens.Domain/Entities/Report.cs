using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;

namespace Greenlens.Domain.Entities;

/// <summary>
/// Aggregate root for pollution reports. Manages the full lifecycle from
/// submission through verification, dispatch, team assignment, and closure.
/// </summary>
/// <remarks>
/// Implements: BR-REP-001 → BR-REP-033.
/// Two-tier dispatch model (v2.0):
///   DEO flow:     SUBMITTED → VERIFIED → DISPATCHED (DEO sends to ward/commune)
///   LEO flow:     DISPATCHED → IN_PROGRESS (LEO assigns team) → RESOLVED → CLOSED
///   Reject:       SUBMITTED → REJECTED
///   Decline path: IN_PROGRESS → DISPATCHED (all teams decline — LEO re-assigns)
///   Re-dispatch:  DISPATCHED → DISPATCHED (DEO re-routes to different ward)
///   Reopen:       RESOLVED → IN_PROGRESS (max 2)
/// </remarks>
public sealed class Report : SoftDeletableEntity
{
    private Report() { }

    // ── Identity ──
    public string Code { get; private set; } = default!;

    // ── Reporter ──
    public Guid? ReporterId { get; private set; }
    public bool IsAnonymous { get; private set; }

    // ── Classification ──
    public Guid CategoryId { get; private set; }
    public Severity Severity { get; private set; } = Severity.Medium;
    public SeveritySource SeveritySetBy { get; private set; } = SeveritySource.User;
    public string? Description { get; private set; }

    // ── Location ──
    public decimal Latitude { get; private set; }
    public decimal Longitude { get; private set; }
    public string? Address { get; private set; }
    public string? WardCode { get; private set; }
    public string? ProvinceCode { get; private set; }

    // ── Status & Assignment ──
    public ReportStatus Status { get; private set; } = ReportStatus.Submitted;
    /// <summary>LEO phụ trách khu vực — set lúc DEO dispatch xuống LocalOffice.</summary>
    public Guid? AssignedOfficerId { get; private set; }
    /// <summary>LEO đã bấm Assign team — set lúc LEO gọi /assign (BR-OFF-011).</summary>
    public Guid? AssignedByOfficerId { get; private set; }
    /// <summary>Office (xã/phường) được DEO dispatch xuống. Null cho đến khi dispatch.</summary>
    public Guid? AssignedOfficeId { get; private set; }
    /// <summary>Department (tỉnh) — set lúc submit theo ProvinceCode. All reports start here.</summary>
    public Guid? AssignedDepartmentId { get; private set; }

    // ── Dispatch tracking ──
    /// <summary>DEO đã dispatch task xuống xã/phường.</summary>
    public Guid? DispatchedById { get; private set; }
    public DateTime? DispatchedAt { get; private set; }

    // ── Duplicate tracking ──
    public Guid? ParentReportId { get; private set; }
    public int ReporterCount { get; private set; } = 1;

    // ── AI Analysis ──
    public bool IsSuspicious { get; private set; }
    public string? SuspiciousReasons { get; private set; }
    public bool AiPending { get; private set; } = true;
    public string? AiClassifiedType { get; private set; }
    public decimal? AiConfidence { get; private set; }
    public Severity? AiEstimatedSeverity { get; private set; }

    // ── Priority ──
    public decimal PriorityScore { get; private set; }

    // ── Lifecycle timestamps ──
    public DateTime? VerifiedAt { get; private set; }
    public Guid? VerifiedBy { get; private set; }
    public string? RejectedReason { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public int ReopenedCount { get; private set; }

    // ── SLA ──
    public DateTime? SlaVerifyDueAt { get; private set; }
    public DateTime? SlaResolveDueAt { get; private set; }

    // ── Navigation properties ──
    public User? Reporter { get; private set; }
    public PollutionCategory Category { get; private set; } = default!;
    public Report? ParentReport { get; private set; }
    public User? VerifiedByUser { get; private set; }
    public User? DispatchedByUser { get; private set; }
    public LocalOffice? AssignedOffice { get; private set; }
    public Department? AssignedDepartment { get; private set; }

    public ICollection<ReportMedia> Media { get; private set; } = [];
    public ICollection<ReportStatusHistory> StatusHistory { get; private set; } = [];
    public ICollection<ReportFlag> Flags { get; private set; } = [];
    public ICollection<Report> DuplicateReports { get; private set; } = [];
    public ICollection<ReportAssignment> Assignments { get; private set; } = [];
    public ICollection<ReportWasteTag> WasteTags { get; private set; } = [];

    // ── AI-suggested waste tags (set by AI service, officer can override) ──
    /// <summary>Comma-separated tag codes suggested by AI, e.g. "HOUSEHOLD,MEDICAL,ANIMAL_CARCASS".</summary>
    public string? AiSuggestedWasteTagCodes { get; private set; }

    // ────────────────────────────────────────────────────
    // Factory
    // ────────────────────────────────────────────────────

    public static Report Create(
        string code,
        Guid? reporterId,
        bool isAnonymous,
        Guid categoryId,
        Severity severity,
        string? description,
        decimal latitude,
        decimal longitude,
        string? address,
        string? wardCode,
        string? provinceCode)
    {
        var report = new Report
        {
            Code = code,
            ReporterId = isAnonymous ? null : reporterId,
            IsAnonymous = isAnonymous,
            CategoryId = categoryId,
            Severity = severity,
            SeveritySetBy = SeveritySource.User,
            Description = description,
            Latitude = latitude,
            Longitude = longitude,
            Address = address,
            WardCode = wardCode,
            ProvinceCode = provinceCode,
            Status = ReportStatus.Submitted,
            AiPending = true,
            SlaVerifyDueAt = DateTime.UtcNow.AddHours(24),
            CreatedAt = DateTime.UtcNow
        };

        return report;
    }

    // ────────────────────────────────────────────────────
    // State machine transitions
    // ────────────────────────────────────────────────────

    /// <summary>DEO verifies the report. Submitted → Verified. BR-REP-020, 021.</summary>
    public void Verify(Guid officerId, Severity? overrideSeverity = null, Guid? overrideCategoryId = null)
    {
        EnsureStatus(ReportStatus.Submitted);

        Status = ReportStatus.Verified;
        VerifiedAt = DateTime.UtcNow;
        VerifiedBy = officerId;

        if (overrideSeverity.HasValue)
        {
            Severity = overrideSeverity.Value;
            SeveritySetBy = SeveritySource.Officer;
        }

        if (overrideCategoryId.HasValue)
            CategoryId = overrideCategoryId.Value;

        SlaResolveDueAt = ComputeSlaResolveDue(Severity);
    }

    /// <summary>DEO dispatches verified report to a ward/commune LocalOffice. Verified → Dispatched.</summary>
    public void Dispatch(Guid deoId, Guid targetOfficeId, Guid? targetOfficerId = null)
    {
        EnsureStatus(ReportStatus.Verified);

        Status = ReportStatus.Dispatched;
        DispatchedById = deoId;
        DispatchedAt = DateTime.UtcNow;
        AssignedOfficeId = targetOfficeId;

        if (targetOfficerId.HasValue)
            AssignedOfficerId = targetOfficerId;
    }

    /// <summary>DEO re-dispatches to a different ward. Dispatched → Dispatched.</summary>
    public void ReDispatch(Guid deoId, Guid newOfficeId, Guid? newOfficerId = null)
    {
        EnsureStatus(ReportStatus.Dispatched);

        DispatchedById = deoId;
        DispatchedAt = DateTime.UtcNow;
        AssignedOfficeId = newOfficeId;
        AssignedOfficerId = newOfficerId;
        // Reset any prior assignments when re-dispatching
        AssignedByOfficerId = null;
    }

    /// <summary>Officer rejects the report. BR-REP-022.</summary>
    public void Reject(string reason)
    {
        EnsureStatus(ReportStatus.Submitted);

        Status = ReportStatus.Rejected;
        RejectedReason = reason;
    }

    /// <summary>LEO assigns team(s). DISPATCHED → INPROGRESS. BR-OFF-011.</summary>
    public void Assign(Guid leoId)
    {
        EnsureStatus(ReportStatus.Dispatched);

        Status = ReportStatus.InProgress;
        AssignedByOfficerId = leoId;
        // StartedAt is set when the first team accepts (not at assign time)
    }

    /// <summary>All teams declined — revert to Dispatched so LEO can re-assign. BR-CLN-007.</summary>
    public void RevertToDispatched()
    {
        if (Status != ReportStatus.InProgress)
            throw new InvalidOperationException(
                $"Cannot revert to Dispatched from status {Status}.");

        Status = ReportStatus.Dispatched;
        AssignedByOfficerId = null;
        StartedAt = null;
    }

    /// <summary>Set StartedAt when first team accepts the assignment.</summary>
    public void MarkStarted()
    {
        StartedAt ??= DateTime.UtcNow;
    }

    /// <summary>BR-ORG-011: Route all reports to department queue on submit. DEO dispatches from here.</summary>
    public void RouteToDepartmentQueue(Guid departmentId)
    {
        AssignedDepartmentId = departmentId;
    }

    /// <summary>Cleanup team resolves the report. BR-REP-014, 023.</summary>
    public void Resolve()
    {
        EnsureStatus(ReportStatus.InProgress);

        Status = ReportStatus.Resolved;
        ResolvedAt = DateTime.UtcNow;
    }

    /// <summary>Auto-close or citizen confirms satisfaction. BR-REP-016.</summary>
    public void Close()
    {
        if (Status is not (ReportStatus.Resolved or ReportStatus.PenaltyIssued))
            throw new InvalidOperationException(
                $"Cannot close from status {Status}. Must be Resolved or PenaltyIssued.");

        Status = ReportStatus.Closed;
        ClosedAt = DateTime.UtcNow;
    }

    /// <summary>Inspection Team issues penalty decision. IN_PROGRESS → PENALTY_ISSUED. BR-INS-012.</summary>
    public void IssuePenalty()
    {
        EnsureStatus(ReportStatus.InProgress);

        Status = ReportStatus.PenaltyIssued;
        ResolvedAt = DateTime.UtcNow;
    }

    /// <summary>No violation found. IN_PROGRESS → CLOSED_NO_VIOLATION. BR-INS-013.</summary>
    public void CloseNoViolation()
    {
        EnsureStatus(ReportStatus.InProgress);

        Status = ReportStatus.ClosedNoViolation;
        ClosedAt = DateTime.UtcNow;
    }

    /// <summary>Citizen not satisfied — reopen. Max 2 times. BR-REP-015.</summary>
    public bool TryReopen()
    {
        if (Status != ReportStatus.Resolved || ReopenedCount >= 2)
            return false;

        Status = ReportStatus.InProgress;
        ReopenedCount++;
        ResolvedAt = null;
        return true;
    }

    /// <summary>Mark as duplicate of another report. BR-REP-030.</summary>
    public void MarkDuplicate(Guid primaryReportId)
    {
        if (Status is not (ReportStatus.Submitted or ReportStatus.Verified or ReportStatus.Assigned))
            throw new InvalidOperationException($"Cannot mark as duplicate from status {Status}.");

        Status = ReportStatus.Duplicate;
        ParentReportId = primaryReportId;
    }

    /// <summary>Increment reporter count when duplicates merge. BR-REP-032.</summary>
    public void IncrementReporterCount() => ReporterCount++;

    // ────────────────────────────────────────────────────
    // AI
    // ────────────────────────────────────────────────────

    public void ApplyAiResults(string classifiedType, decimal confidence, Severity estimatedSeverity)
    {
        AiClassifiedType = classifiedType;
        AiConfidence = confidence;
        AiEstimatedSeverity = estimatedSeverity;
        AiPending = false;
    }

    public void FlagSuspicious(string reasons)
    {
        IsSuspicious = true;
        SuspiciousReasons = reasons;
    }

    public void UpdatePriorityScore(decimal score) => PriorityScore = score;

    /// <summary>AI service sets suggested waste tag codes after image analysis.</summary>
    public void SetAiSuggestedWasteTagCodes(string? codes) => AiSuggestedWasteTagCodes = codes;

    // ────────────────────────────────────────────────────
    // Helpers
    // ────────────────────────────────────────────────────

    private void EnsureStatus(ReportStatus expected)
    {
        if (Status != expected)
            throw new InvalidOperationException(
                $"Invalid state transition: expected {expected} but current is {Status}.");
    }

    private static DateTime ComputeSlaResolveDue(Severity severity) => severity switch
    {
        Severity.Critical => DateTime.UtcNow.AddDays(3),
        Severity.High => DateTime.UtcNow.AddDays(5),
        Severity.Medium => DateTime.UtcNow.AddDays(7),
        Severity.Low => DateTime.UtcNow.AddDays(10),
        _ => DateTime.UtcNow.AddDays(7)
    };

    /// <summary>Admin-only: force status without state machine validation.</summary>
    public void ForceStatus(ReportStatus newStatus)
    {
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }
}
