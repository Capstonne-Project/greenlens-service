using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Analytics.GetAdminRecentActivities;

/// <summary>Recent report lifecycle events (status transitions) across the whole system.</summary>
public sealed class GetAdminRecentActivitiesQueryHandler(
    IReportStatusHistoryRepository history)
    : IRequestHandler<GetAdminRecentActivitiesQuery, Result<List<RecentActivityItem>>>
{
    public async Task<Result<List<RecentActivityItem>>> Handle(
        GetAdminRecentActivitiesQuery request, CancellationToken ct)
    {
        var items = await history.QueryAsNoTracking()
            .Include(h => h.Report)
            .OrderByDescending(h => h.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(h => new RecentActivityItem(
                h.CreatedAt,
                DescribeType(h.ToStatus),
                $"Report #{h.Report.Code} chuyển sang trạng thái {h.ToStatus}"
                    + (h.Reason != null ? $" ({h.Reason})" : string.Empty)))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return items;
    }

    private static string DescribeType(ReportStatus status) => status switch
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
