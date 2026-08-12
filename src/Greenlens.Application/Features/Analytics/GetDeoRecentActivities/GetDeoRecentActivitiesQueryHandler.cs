using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Application.Features.Analytics.GetAdminRecentActivities;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Analytics.GetDeoRecentActivities;

public sealed class GetDeoRecentActivitiesQueryHandler(
    IReportStatusHistoryRepository history,
    IUserRepository users,
    ICurrentUser currentUser,
    ILogger<GetDeoRecentActivitiesQueryHandler> logger)
    : IRequestHandler<GetDeoRecentActivitiesQuery, Result<List<RecentActivityItem>>>
{
    public async Task<Result<List<RecentActivityItem>>> Handle(
        GetDeoRecentActivitiesQuery request, CancellationToken ct)
    {
        var scopeResult = await DepartmentContextResolver.ResolveAsync(users, currentUser, ct).ConfigureAwait(false);
        if (scopeResult.IsFailure)
            return scopeResult.Error!;

        var deptId = scopeResult.Value.DepartmentId;
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var items = await history.QueryAsNoTracking()
            .Include(h => h.Report)
            .Where(h => h.Report.AssignedDepartmentId == deptId)
            .OrderByDescending(h => h.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(h => new RecentActivityItem(
                h.CreatedAt,
                DescribeType(h.ToStatus),
                $"Report #{h.Report.Code} chuyển sang trạng thái {h.ToStatus}"
                    + (h.Reason != null ? $" ({h.Reason})" : string.Empty)))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        logger.LogInformation("DEO recent activities page {Page}: {Count} items", page, items.Count);
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
