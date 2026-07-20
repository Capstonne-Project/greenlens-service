namespace Greenlens.Domain.Enums;

/// <summary>
/// Status of a team assignment to a report.
/// Tracks individual team progress in multi-team assignments.
/// </summary>
public enum AssignmentStatus
{
    Assigned,
    InProgress,
    Completed,
    Declined,

    /// <summary>Team escalated to LEO — beyond their capability. BR-CLN-006.</summary>
    Escalated
}
