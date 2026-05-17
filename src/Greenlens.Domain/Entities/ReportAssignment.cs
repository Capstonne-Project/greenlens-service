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

    // ── Navigation ──
    public Report? Report { get; private set; }
    public EnvironmentalTeam? Team { get; private set; }
    public User? AssignedByUser { get; private set; }

    /// <summary>BR-OFF-011: Create an assignment of a report to a team.</summary>
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
            AssignedAt = DateTime.UtcNow
        };
    }

    /// <summary>Team starts working on the task.</summary>
    public void Start()
    {
        if (Status != AssignmentStatus.Assigned)
            throw new InvalidOperationException(
                $"Cannot start from status {Status}. Must be Assigned.");

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
}
