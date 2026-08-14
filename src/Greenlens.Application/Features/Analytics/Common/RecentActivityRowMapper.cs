using Greenlens.Application.Features.Analytics.GetAdminRecentActivities;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.Features.Analytics.Common;

/// <summary>Maps report status history rows to dashboard activity items (client-side).</summary>
internal static class RecentActivityRowMapper
{
    internal sealed record Row(DateTime CreatedAt, ReportStatus ToStatus, string ReportCode, string? Reason);

    internal static List<RecentActivityItem> MapAdminRows(IReadOnlyList<Row> rows) =>
        rows.Select(r => new RecentActivityItem(
            r.CreatedAt,
            DescribeAdminType(r.ToStatus),
            BuildDescription(r.ReportCode, r.ToStatus, r.Reason)))
        .ToList();

    private static string BuildDescription(string reportCode, ReportStatus status, string? reason) =>
        $"Report #{reportCode} chuyển sang trạng thái {status}"
        + (reason != null ? $" ({reason})" : string.Empty);

    private static string DescribeAdminType(ReportStatus status) => status switch
    {
        ReportStatus.Verified => "OfficerVerified",
        ReportStatus.InProgress => "TeamAssigned",
        ReportStatus.Resolved => "ReportResolved",
        ReportStatus.Closed => "ReportClosed",
        ReportStatus.Rejected => "ReportRejected",
        ReportStatus.Duplicate => "ReportMarkedDuplicate",
        _ => "StatusChanged"
    };
}
