using Greenlens.Domain.Enums;

namespace Greenlens.Application.Common.CitizenMap;

/// <summary>Report statuses considered "still active" (not yet resolved) for citizen map ward risk scoring.</summary>
public static class CitizenMapReportStatuses
{
    /// <summary>Same visibility rule as the public map (Verified+), used for ward-list pins.</summary>
    public static readonly ReportStatus[] Visible =
    [
        ReportStatus.Verified,
        ReportStatus.Reopened,
        ReportStatus.InProgress,
        ReportStatus.Resolved,
        ReportStatus.Closed
    ];

    /// <summary>Subset of <see cref="Visible"/> still unresolved — drives ward risk level (5-tier color).</summary>
    public static readonly ReportStatus[] Active =
    [
        ReportStatus.Verified,
        ReportStatus.Reopened,
        ReportStatus.InProgress
    ];
}
