using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;

namespace Greenlens.Domain.Entities;

/// <summary>
/// Tracks assignment of a report to a team. Supports multi-team
/// assignments where each team has independent status tracking.
/// </summary>
/// <remarks>Implements: BR-OFF-011, BR-OFF-012, BR-CLN-007, BR-INS-003.</remarks>
public sealed class ReportAssignment : BaseEntity
{
    private ReportAssignment() { } // EF Core constructor

    public Guid ReportId { get; private set; }
    public Guid TeamId { get; private set; }
    public Guid AssignedById { get; private set; }
    public AssignmentStatus Status { get; private set; } = AssignmentStatus.Assigned;
    public string? Note { get; private set; }
    public string? DeclineReason { get; private set; }
    public DateTime AssignedAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    // ── Progress tracking (updated by team leader mid-task) ──
    public int ProgressPercent { get; private set; }
    public string? ProgressNote { get; private set; }
    public DateTime? ProgressUpdatedAt { get; private set; }
    public Guid? ProgressUpdatedByUserId { get; private set; }

    // ── Navigation ──
    public Report? Report { get; private set; }
    public EnvironmentalTeam? Team { get; private set; }
    public User? AssignedByUser { get; private set; }

    /// <summary>BR-OFF-011: Create an assignment. Starts Assigned (pending accept).</summary>
    public static ReportAssignment Create(
        Guid reportId,
        Guid teamId,
        Guid assignedById,
        string? note = null)
    {
        return new ReportAssignment
        {
            ReportId = reportId,
            TeamId = teamId,
            AssignedById = assignedById,
            Note = note,
            Status = AssignmentStatus.Assigned,
            AssignedAt = DateTime.UtcNow,
            StartedAt = null
        };
    }

    /// <summary>Team leader accepts the assignment. Assigned → InProgress. StartedAt set here.</summary>
    public void Accept()
    {
        if (Status != AssignmentStatus.Assigned)
            throw new InvalidOperationException(
                $"Cannot accept from status {Status}. Must be Assigned.");

        Status = AssignmentStatus.InProgress;
        StartedAt = DateTime.UtcNow;
    }

    /// <summary>Team completes their part of the task.</summary>
    public void Complete()
    {
        if (Status != AssignmentStatus.InProgress)
            throw new InvalidOperationException(
                $"Cannot complete from status {Status}. Must be InProgress.");

        Status = AssignmentStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>BR-CLN-007, BR-INS-003: Team declines task within 2h window.</summary>
    public void Decline(string reason)
    {
        if (Status != AssignmentStatus.Assigned)
            throw new InvalidOperationException(
                $"Cannot decline from status {Status}. Must be Assigned.");

        Status = AssignmentStatus.Declined;
        DeclineReason = reason;
    }

    /// <summary>Team leader updates progress mid-task. Status must be InProgress.</summary>
    public void UpdateProgress(int percent, string? note, Guid updatedByUserId)
    {
        if (Status != AssignmentStatus.InProgress)
            throw new InvalidOperationException(
                $"Cannot update progress from status {Status}. Must be InProgress.");

        if (percent < 0 || percent > 100)
            throw new ArgumentOutOfRangeException(nameof(percent), "Percent must be 0–100.");

        ProgressPercent = percent;
        ProgressNote = note;
        ProgressUpdatedAt = DateTime.UtcNow;
        ProgressUpdatedByUserId = updatedByUserId;
    }
}
