using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;

namespace Greenlens.Domain.Entities;

/// <summary>
/// Aggregate root for pollution reports. Manages the full lifecycle from
/// submission through verification, team assignment, and closure.
/// </summary>
/// <remarks>
/// Implements: BR-REP-001 → BR-REP-033.
/// LEO direct verification model (v3.0):
///   Submit:   SUBMITTED (auto-routed to LocalOffice by GPS, fallback Department queue)
///   LEO:      SUBMITTED → VERIFIED → IN_PROGRESS → RESOLVED → CLOSED
///   Reject:   SUBMITTED → REJECTED (LEO, reason ≥ 20 chars)
///   Duplicate: SUBMITTED/VERIFIED → DUPLICATE
///   Reopen:   RESOLVED → IN_PROGRESS (max 2 times)
///
/// InspectionReport runs as a parallel sub-process (BR-INS-001).
/// </remarks>
public sealed class Report : SoftDeletableEntity
{
    private Report() { }

    // ── Identity ──
    public string Code { get; private set; } = default!;

    // ── Reporter ──
    public Guid? ReporterId { get; private set; }

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

    // ── Status ──
    public ReportStatus Status { get; private set; } = ReportStatus.Submitted;

    // ── Auto-routing (set at submit time) ──
    /// <summary>LocalOffice (xã/phường) auto-assigned by WardCode at submit. Null if ward not onboarded.</summary>
    public Guid? AssignedOfficeId { get; private set; }
    /// <summary>Department (tỉnh) auto-assigned by ProvinceCode at submit.</summary>
    public Guid? AssignedDepartmentId { get; private set; }

    // ── LEO assignment (set when LEO assigns team) ──
    /// <summary>LEO who verified this report.</summary>
    public Guid? VerifiedBy { get; private set; }
    /// <summary>LEO or CM who assigned team(s) to this report.</summary>
    public Guid? AssignedByOfficerId { get; private set; }

    // ── Company dispatch (set when LEO dispatches to company) ──
    /// <summary>Company assigned by LEO for cleanup. Null = community team handles it.</summary>
    public Guid? AssignedCompanyId { get; private set; }
    /// <summary>When LEO dispatched the report to the company.</summary>
    public DateTime? DispatchedToCompanyAt { get; private set; }

    // ── Duplicate tracking ──
    public Guid? ParentReportId { get; private set; }
    public int ReporterCount { get; private set; } = 1;

    // ── Duplicate detection (BR-REP-030/031) ──
    /// <summary>AI/geo flagged this report as a possible duplicate awaiting LEO decision.</summary>
    public bool IsPossibleDuplicate { get; private set; }
    /// <summary>The oldest candidate report this one likely duplicates.</summary>
    public Guid? PossibleDuplicateOfReportId { get; private set; }
    /// <summary>How the duplicate was detected: "geo_time" (Tier 1) or "geo_time_ai" (Tier 2 CLIP/DINOv2).</summary>
    public string? DuplicateDetectionSource { get; private set; }
    /// <summary>Image similarity score 0.0–1.0 from the AI compare service (Tier 2 only).</summary>
    public decimal? AiSimilarityScore { get; private set; }

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
    public string? RejectedReason { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public int ReopenedCount { get; private set; }

    // ── SLA ──
    public DateTime? SlaVerifyDueAt { get; private set; }
    public DateTime? SlaResolveDueAt { get; private set; }
    public bool SlaVerifyBreached { get; private set; }
    public bool SlaResolveBreached { get; private set; }

    // ── Overdue ──
    /// <summary>BR-REP-008: Report pending > 72h.</summary>
    public bool IsOverdue { get; private set; }

    // ── Content Moderation (BR-ADM-006) ──
    /// <summary>Admin-hidden content. Reversible soft-hide, separate from soft-delete.</summary>
    public bool IsHidden { get; private set; }
    public DateTime? HiddenAt { get; private set; }
    public Guid? HiddenBy { get; private set; }
    public string? HiddenReason { get; private set; }

    // ── Navigation properties ──
    public User? Reporter { get; private set; }
    public PollutionCategory Category { get; private set; } = default!;
    public Report? ParentReport { get; private set; }
    public User? VerifiedByUser { get; private set; }
    public LocalOffice? AssignedOffice { get; private set; }
    public Department? AssignedDepartment { get; private set; }
    public EnvironmentalServiceCompany? AssignedCompany { get; private set; }

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
            ReporterId = reporterId,
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
    // Auto-routing (called at submit time)
    // ────────────────────────────────────────────────────

    /// <summary>BR-ORG-011: Route report directly to LocalOffice by WardCode.</summary>
    public void RouteToLocalOffice(Guid officeId, Guid departmentId)
    {
        AssignedOfficeId = officeId;
        AssignedDepartmentId = departmentId;
    }

    /// <summary>Fallback: route to Department queue when ward has no onboarded LocalOffice.</summary>
    public void RouteToDepartment(Guid departmentId)
    {
        AssignedDepartmentId = departmentId;
        // AssignedOfficeId stays null — DEO will handle manually
    }

    // ────────────────────────────────────────────────────
    // State machine transitions
    // ────────────────────────────────────────────────────

    /// <summary>LEO verifies the report. Submitted → Verified. BR-REP-020, 021.</summary>
    public void Verify(Guid leoId, Severity? overrideSeverity = null, Guid? overrideCategoryId = null)
    {
        EnsureStatus(ReportStatus.Submitted);

        Status = ReportStatus.Verified;
        VerifiedAt = DateTime.UtcNow;
        VerifiedBy = leoId;

        if (overrideSeverity.HasValue)
        {
            Severity = overrideSeverity.Value;
            SeveritySetBy = SeveritySource.Officer;
        }

        if (overrideCategoryId.HasValue)
            CategoryId = overrideCategoryId.Value;

        SlaResolveDueAt = ComputeSlaResolveDue(Severity);

        if (ReporterId.HasValue)
            AddDomainEvent(new ReportVerifiedEvent(Id, ReporterId.Value));
    }

    /// <summary>
    /// BR-ORG-015: LEO rejects the report. Report stays Submitted and is re-queued
    /// to the Department Common Queue for DEO to re-assign.
    /// BR-REP-022: reason ≥ 20 chars.
    /// </summary>
    public void Reject(string reason)
    {
        EnsureStatus(ReportStatus.Submitted);

        RejectedReason = reason;
        // Status stays Submitted — report goes back to Department queue
        // Clear office assignment so DEO sees it in the common queue
        AssignedOfficeId = null;
        // AssignedDepartmentId is kept — DEO of the same province handles it

        if (ReporterId.HasValue)
            AddDomainEvent(new ReportRejectedEvent(Id, ReporterId.Value));
    }

    /// <summary>LEO assigns community team(s). Verified → InProgress. BR-OFF-011.</summary>
    public void Assign(Guid leoId)
    {
        EnsureStatus(ReportStatus.Verified);

        Status = ReportStatus.InProgress;
        AssignedByOfficerId = leoId;
        // StartedAt is set when the first team accepts (not at assign time)
    }

    /// <summary>
    /// LEO dispatches report to a company for cleanup. Report stays Verified.
    /// CompanyManager will assign specific company teams later.
    /// </summary>
    public void DispatchToCompany(Guid companyId, Guid leoId)
    {
        EnsureStatus(ReportStatus.Verified);

        AssignedCompanyId = companyId;
        DispatchedToCompanyAt = DateTime.UtcNow;
        // Status stays Verified — transitions to InProgress when CM assigns team
    }

    /// <summary>
    /// CompanyManager assigns company team(s). Verified → InProgress.
    /// Only valid when report was dispatched to a company (AssignedCompanyId set).
    /// </summary>
    public void AssignByCompanyManager(Guid companyManagerId)
    {
        EnsureStatus(ReportStatus.Verified);

        if (!AssignedCompanyId.HasValue)
            throw new InvalidOperationException("Report must be dispatched to a company before CM can assign teams.");

        Status = ReportStatus.InProgress;
        AssignedByOfficerId = companyManagerId;
    }

    /// <summary>Set StartedAt when first team accepts the assignment.</summary>
    public void MarkStarted()
    {
        StartedAt ??= DateTime.UtcNow;
    }

    /// <summary>Cleanup team resolves the report. InProgress → Resolved. BR-REP-014, 023.</summary>
    public void Resolve()
    {
        EnsureStatus(ReportStatus.InProgress);

        Status = ReportStatus.Resolved;
        ResolvedAt = DateTime.UtcNow;

        if (ReporterId.HasValue)
            AddDomainEvent(new ReportResolvedEvent(Id, ReporterId.Value));
    }

    /// <summary>Auto-close or citizen confirms satisfaction. Resolved → Closed. BR-REP-016.</summary>
    public void Close()
    {
        EnsureStatus(ReportStatus.Resolved);

        Status = ReportStatus.Closed;
        ClosedAt = DateTime.UtcNow;
    }

    /// <summary>Citizen not satisfied — reopen. Max 2 times, within 7 days. Resolved → InProgress. BR-REP-015.</summary>
    public bool TryReopen()
    {
        if (Status != ReportStatus.Resolved || ReopenedCount >= 2)
            return false;

        // BR-REP-015: only reopen within 7 days of Resolved
        if (ResolvedAt.HasValue && DateTime.UtcNow - ResolvedAt.Value > TimeSpan.FromDays(7))
            return false;

        Status = ReportStatus.InProgress;
        ReopenedCount++;
        ResolvedAt = null;
        return true;
    }

    /// <summary>Mark as duplicate of another report. BR-REP-030, BR-REP-032.</summary>
    public void MarkDuplicate(Guid primaryReportId)
    {
        if (Status is not (ReportStatus.Submitted or ReportStatus.Verified))
            throw new InvalidOperationException($"Cannot mark as duplicate from status {Status}.");

        Status = ReportStatus.Duplicate;
        ParentReportId = primaryReportId;
        IsPossibleDuplicate = false;

        if (ReporterId.HasValue)
            AddDomainEvent(new ReportMarkedDuplicateEvent(Id, ReporterId.Value, primaryReportId));
    }

    /// <summary>Increment reporter count when duplicates merge. BR-REP-032.</summary>
    public void IncrementReporterCount() => ReporterCount++;

    // ── Duplicate detection (BR-REP-030/031) ──

    /// <summary>
    /// Tier 1 flag: same category + within 50m + within 24h of an existing report.
    /// Raises an event so Tier 2 (AI image compare) can run in the background.
    /// </summary>
    /// <remarks>Implements: BR-REP-030 (definition), BR-REP-031 (possible_duplicate flag).</remarks>
    public void MarkPossibleDuplicate(Guid candidateReportId, string source, decimal? aiScore = null)
    {
        IsPossibleDuplicate = true;
        PossibleDuplicateOfReportId = candidateReportId;
        DuplicateDetectionSource = source;
        AiSimilarityScore = aiScore;

        AddDomainEvent(new ReportPossibleDuplicateFlaggedEvent(Id, candidateReportId));
    }

    /// <summary>Clear the possible-duplicate flag (LEO dismiss, or AI says different scene). BR-REP-031.</summary>
    public void DismissDuplicate()
    {
        IsPossibleDuplicate = false;
        PossibleDuplicateOfReportId = null;
        DuplicateDetectionSource = null;
        AiSimilarityScore = null;
    }

    /// <summary>
    /// Tier 2 result from the AI image compare service (BR-AI-002).
    /// Same scene → upgrade source to "geo_time_ai" and record score; different scene → dismiss.
    /// </summary>
    public void ApplyDuplicateAiResult(bool isSameScene, decimal confidence)
    {
        if (!IsPossibleDuplicate)
            return;

        // No-op once LEO confirmed duplicate or report was rejected/dismissed concurrently.
        if (Status is ReportStatus.Duplicate or ReportStatus.Rejected)
            return;

        if (!isSameScene)
        {
            DismissDuplicate();
            return;
        }

        DuplicateDetectionSource = "geo_time_ai";
        AiSimilarityScore = confidence;
    }

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

    /// <summary>BR-OFF-002: Flag SLA verification breach (Submitted > 24h).</summary>
    public void MarkSlaVerifyBreached() => SlaVerifyBreached = true;

    /// <summary>
    /// BR-ORG-014: Escalate report to Department queue when SLA verification breached.
    /// Clears LocalOffice assignment so DEO sees it in common queue.
    /// </summary>
    public void EscalateToDepartment()
    {
        AssignedOfficeId = null;
        // AssignedDepartmentId preserved — report stays in same province
    }

    /// <summary>BR-OFF-020: Flag SLA resolution breach (InProgress > severity deadline).</summary>
    public void MarkSlaResolveBreached() => SlaResolveBreached = true;

    /// <summary>
    /// BR-CMP-013: Revert InProgress → Verified when company deactivated and
    /// all its assignments are declined. LEO will reassign to another team.
    /// </summary>
    public void RevertToVerified()
    {
        EnsureStatus(ReportStatus.InProgress);
        Status = ReportStatus.Verified;
        AssignedCompanyId = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>BR-REP-008: Mark report as overdue (pending > 72h).</summary>
    public void MarkOverdue() => IsOverdue = true;

    /// <summary>
    /// BR-REP-017: Citizen can only delete report at Submitted status
    /// with no AI classification or officer verification.
    /// </summary>
    public bool CanDelete()
        => Status == ReportStatus.Submitted
           && VerifiedBy is null
           && AiClassifiedType is null;

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

    // ────────────────────────────────────────────────────
    // Content Moderation (BR-ADM-006)
    // ────────────────────────────────────────────────────

    /// <summary>Admin hides this report from public view. Reversible.</summary>
    /// <remarks>Implements: BR-ADM-006.</remarks>
    public void Hide(Guid adminId, string reason)
    {
        IsHidden = true;
        HiddenAt = DateTime.UtcNow;
        HiddenBy = adminId;
        HiddenReason = reason;
    }

    /// <summary>Admin unhides this report, making it visible again.</summary>
    /// <remarks>Implements: BR-ADM-006.</remarks>
    public void Unhide()
    {
        IsHidden = false;
        HiddenAt = null;
        HiddenBy = null;
        HiddenReason = null;
    }
}
