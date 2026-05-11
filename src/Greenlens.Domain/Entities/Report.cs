using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;

namespace Greenlens.Domain.Entities;

/// <summary>
/// Aggregate root for pollution reports. Manages the full lifecycle from
/// submission through verification, cleanup, resolution, and closure.
/// </summary>
/// <remarks>
/// Implements: BR-REP-001 → BR-REP-033.
/// State machine: SUBMITTED → VERIFIED → IN_PROGRESS → RESOLVED → CLOSED
///                SUBMITTED → REJECTED
///                SUBMITTED/VERIFIED → DUPLICATE
///                RESOLVED → IN_PROGRESS (reopen, max 2)
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
    public Guid? AssignedTeamId { get; private set; }
    public Guid? AssignedOfficerId { get; private set; }

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

    public ICollection<ReportMedia> Media { get; private set; } = [];
    public ICollection<ReportStatusHistory> StatusHistory { get; private set; } = [];
    public ICollection<ReportFlag> Flags { get; private set; } = [];
    public ICollection<Report> DuplicateReports { get; private set; } = [];

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

    /// <summary>Officer verifies the report. BR-REP-020, 021.</summary>
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

    /// <summary>Officer rejects the report. BR-REP-022.</summary>
    public void Reject(string reason)
    {
        EnsureStatus(ReportStatus.Submitted);

        Status = ReportStatus.Rejected;
        RejectedReason = reason;
    }

    /// <summary>Assign cleanup team. VERIFIED → IN_PROGRESS. BR-OFF-011.</summary>
    public void Assign(Guid teamId, Guid officerId)
    {
        EnsureStatus(ReportStatus.Verified);

        Status = ReportStatus.InProgress;
        AssignedTeamId = teamId;
        AssignedOfficerId = officerId;
        StartedAt = DateTime.UtcNow;
    }

    /// <summary>Reassign to different team. BR-OFF-012.</summary>
    public void Reassign(Guid newTeamId)
    {
        AssignedTeamId = newTeamId;
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
        EnsureStatus(ReportStatus.Resolved);

        Status = ReportStatus.Closed;
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
        if (Status is not (ReportStatus.Submitted or ReportStatus.Verified))
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
}
