using Greenlens.Domain.Enums;

namespace Greenlens.Application.Common.Map;

/// <summary>Report statuses visible on the public map and viewport summary (Verified+).</summary>
public static class PublicMapReportStatuses
{
    public static readonly ReportStatus[] Visible =
    [
        ReportStatus.Verified,
        ReportStatus.Reopened,
        ReportStatus.InProgress,
        ReportStatus.Resolved,
        ReportStatus.Closed
    ];
}
