using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Application.Features.Analytics.GetAdminRecentActivities;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Analytics.GetDeoRecentActivities;

/// <summary>Recent report lifecycle events scoped to the DEO's department.</summary>
/// <remarks>Implements: BR-OFF-010 (monitoring), BR-SYS-001.</remarks>
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

        var rows = await history.QueryAsNoTracking()
            .Where(h => h.Report.AssignedDepartmentId == deptId)
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

        logger.LogInformation("DEO recent activities page {Page}: {Count} items", page, items.Count);
        return items;
    }
}
