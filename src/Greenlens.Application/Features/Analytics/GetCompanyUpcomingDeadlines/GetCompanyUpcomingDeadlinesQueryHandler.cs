using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Analytics.GetCompanyUpcomingDeadlines;

/// <summary>
/// Open tasks for the caller's company whose SLA resolve deadline falls within the window.
/// Default window: now → now+7d (deadlines already passed are excluded — see queue-aging for overdue tasks).
/// </summary>
public sealed class GetCompanyUpcomingDeadlinesQueryHandler(
    IReportRepository reports,
    ICompanyStaffRepository companyStaff,
    ICurrentUser currentUser,
    IDateTimeProvider clock)
    : IRequestHandler<GetCompanyUpcomingDeadlinesQuery, Result<List<UpcomingDeadlineItem>>>
{
    private const int DefaultWindowDays = 7;
    private static readonly ReportStatus[] OpenStatuses =
        [ReportStatus.Verified, ReportStatus.InProgress];

    public async Task<Result<List<UpcomingDeadlineItem>>> Handle(
        GetCompanyUpcomingDeadlinesQuery request, CancellationToken ct)
    {
        var companyIdResult = await CompanyContextResolver
            .ResolveCompanyIdAsync(companyStaff, currentUser.UserId, ct)
            .ConfigureAwait(false);
        if (companyIdResult.IsFailure)
            return companyIdResult.Error!;

        var companyId = companyIdResult.Value;
        var now = clock.UtcNow;
        var from = (request.From ?? now).ToUniversalTime();
        var to = (request.To ?? now.AddDays(DefaultWindowDays)).ToUniversalTime();

        var items = await reports.QueryAsNoTracking()
            .Include(r => r.Category)
            .Where(r => r.AssignedCompanyId == companyId
                        && OpenStatuses.Contains(r.Status)
                        && r.SlaResolveDueAt != null
                        && r.SlaResolveDueAt >= from && r.SlaResolveDueAt <= to)
            .OrderBy(r => r.SlaResolveDueAt)
            .Select(r => new UpcomingDeadlineItem(
                r.Id,
                r.Code,
                r.Category.NameVi,
                r.Severity,
                r.SlaResolveDueAt!.Value,
                0m))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var result = items
            .Select(i => i with { HoursRemaining = Math.Round((decimal)(i.SlaResolveDueAt - now).TotalHours, 1) })
            .ToList();

        return result;
    }
}
