using Greenlens.Domain.Enums;

namespace Greenlens.Application.Features.Gamification;

/// <summary>
/// Report statuses that count as "verified by LEO" for milestone badges (BR-GAM-004).
/// Excludes Submitted, Rejected, and Duplicate.
/// </summary>
internal static class VerifiedReportStatusFilter
{
    internal static readonly ReportStatus[] CountedStatuses =
    [
        ReportStatus.Verified,
        ReportStatus.InProgress,
        ReportStatus.Resolved,
        ReportStatus.Reopened,
        ReportStatus.Closed
    ];

    internal static bool IsVerifiedForBadge(ReportStatus status) =>
        CountedStatuses.Contains(status);
}
