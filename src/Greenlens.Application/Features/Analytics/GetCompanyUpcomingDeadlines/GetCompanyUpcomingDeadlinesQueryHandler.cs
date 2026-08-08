using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace Greenlens.Application.Features.Analytics.GetCompanyUpcomingDeadlines;

/// <summary>
/// Open tasks for the caller's company whose SLA resolve deadline falls within the window.
/// Default window: now → now+7d (deadlines already passed are excluded — see queue-aging for overdue tasks).
/// </summary>
public sealed class GetCompanyUpcomingDeadlinesQueryHandler(
    IReportRepository reports,
    ICompanyStaffRepository companyStaff,
    ICurrentUser currentUser,
    IDateTimeProvider clock,
    ILogger<GetCompanyUpcomingDeadlinesQueryHandler> logger)
    : IRequestHandler<GetCompanyUpcomingDeadlinesQuery, Result<List<UpcomingDeadlineItem>>>
{
    private const int DefaultWindowDays = 7;
    private static readonly ReportStatus[] OpenStatuses =
        [ReportStatus.Verified, ReportStatus.InProgress];

    public async Task<Result<List<UpcomingDeadlineItem>>> Handle(
        GetCompanyUpcomingDeadlinesQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting company upcoming deadlines");

        var companyIdResult = await CompanyContextResolver
            .ResolveCompanyIdAsync(companyStaff, currentUser.UserId, ct)
            .ConfigureAwait(false);
        logger.LogInformation("Company ID: {CompanyId}", companyIdResult.Value);
        if (companyIdResult.IsFailure)
            {
                logger.LogError("Failed to resolve company ID: {Error}", companyIdResult.Error);
                return companyIdResult.Error!;
            }

        var companyId = companyIdResult.Value;
        logger.LogInformation("Company ID: {CompanyId}", companyId);
        var now = clock.UtcNow;
        logger.LogInformation("Now: {Now}", now);
        var from = (request.From ?? now).ToUniversalTime();
        logger.LogInformation("From: {From}", from);
        var to = (request.To ?? now.AddDays(DefaultWindowDays)).ToUniversalTime();
        logger.LogInformation("To: {To}", to);

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

        logger.LogInformation("Items: {Items}", items);

        var result = items
            .Select(i => i with { HoursRemaining = Math.Round((decimal)(i.SlaResolveDueAt - now).TotalHours, 1) })
            .ToList();

        logger.LogInformation("Company upcoming deadlines retrieved successfully");

        return result;
    }
}
