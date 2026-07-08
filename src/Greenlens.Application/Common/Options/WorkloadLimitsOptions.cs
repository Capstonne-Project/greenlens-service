namespace Greenlens.Application.Common.Options;

/// <summary>
/// BR-OFF-013: Configurable workload limits for team assignment.
/// Default: max 6 tasks per team, warning at 5.
/// </summary>
public sealed class WorkloadLimitsOptions
{
    public const string SectionName = "WorkloadLimits";

    /// <summary>Max active tasks (Assigned + InProgress) per team. Default = 6.</summary>
    public int MaxTasksPerTeam { get; init; } = 6;

    /// <summary>Threshold to emit warning log. Default = 5.</summary>
    public int WarningThreshold { get; init; } = 5;
}
