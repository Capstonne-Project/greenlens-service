using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Analytics.GetCompanyRecentActivities;

/// <summary>Recent lifecycle events for reports dispatched to the caller's company.</summary>
public sealed class GetCompanyRecentActivitiesQueryHandler(
    IReportStatusHistoryRepository history,
    ICompanyStaffRepository companyStaff,
    ICurrentUser currentUser,
    IDateTimeProvider clock)
    : IRequestHandler<GetCompanyRecentActivitiesQuery, Result<List<CompanyRecentActivityItem>>>
{
    private const int MaxItems = 50;

    public async Task<Result<List<CompanyRecentActivityItem>>> Handle(
        GetCompanyRecentActivitiesQuery request, CancellationToken ct)
    {
        var companyIdResult = await CompanyContextResolver
            .ResolveCompanyIdAsync(companyStaff, currentUser.UserId, ct)
            .ConfigureAwait(false);
        if (companyIdResult.IsFailure)
            return companyIdResult.Error!;

        var companyId = companyIdResult.Value;
        var (from, to) = DateRangeDefaults.Resolve(request.From, request.To, clock.UtcNow);

        var items = await history.QueryAsNoTracking()
            .Include(h => h.Report)
            .Where(h => h.Report.AssignedCompanyId == companyId
                        && h.CreatedAt >= from && h.CreatedAt <= to)
            .OrderByDescending(h => h.CreatedAt)
            .Take(MaxItems)
            .Select(h => new CompanyRecentActivityItem(
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
        ReportStatus.InProgress => "TeamAssigned",
        ReportStatus.Resolved => "TaskResolved",
        ReportStatus.Closed => "TaskClosed",
        _ => "StatusChanged"
    };
}
