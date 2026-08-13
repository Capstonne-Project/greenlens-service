using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Application.Features.Analytics.GetAdminRecentActivities;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Analytics.GetAdminRecentActivities;

/// <summary>Recent report lifecycle events (status transitions) across the whole system.</summary>
public sealed class GetAdminRecentActivitiesQueryHandler(
    IReportStatusHistoryRepository history,
    ILogger<GetAdminRecentActivitiesQueryHandler> logger)
    : IRequestHandler<GetAdminRecentActivitiesQuery, Result<List<RecentActivityItem>>>
{
    public async Task<Result<List<RecentActivityItem>>> Handle(
        GetAdminRecentActivitiesQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting admin recent activities");

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var rows = await history.QueryAsNoTracking()
            .OrderByDescending(h => h.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(h => new
            {
                h.CreatedAt,
                h.ToStatus,
                ReportCode = h.Report.Code,
                h.Reason
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var items = RecentActivityRowMapper.MapAdminRows(
            rows.Select(r => new RecentActivityRowMapper.Row(r.CreatedAt, r.ToStatus, r.ReportCode, r.Reason)).ToList());

        logger.LogInformation("Admin recent activities retrieved successfully");

        return items;
    }
}
