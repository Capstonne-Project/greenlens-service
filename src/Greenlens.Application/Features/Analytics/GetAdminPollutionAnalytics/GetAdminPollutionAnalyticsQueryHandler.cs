using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Analytics.GetAdminPollutionAnalytics;

/// <summary>Report count per pollution category, for the admin dashboard category breakdown chart.</summary>
public sealed class GetAdminPollutionAnalyticsQueryHandler(
    IReportRepository reports,
    IDateTimeProvider clock,
    ILogger<GetAdminPollutionAnalyticsQueryHandler> logger)
    : IRequestHandler<GetAdminPollutionAnalyticsQuery, Result<List<PollutionAnalyticsItem>>>
{
    public async Task<Result<List<PollutionAnalyticsItem>>> Handle(
        GetAdminPollutionAnalyticsQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting admin pollution analytics");

        var (from, to) = DateRangeDefaults.Resolve(request.From, request.To, clock.UtcNow);

        // Project to anonymous type first — EF cannot translate
        // Select(new PollutionAnalyticsItem(...)).OrderByDescending(i => i.Count).
        var counts = await reports.QueryAsNoTracking()
            .Where(r => r.CreatedAt >= from && r.CreatedAt <= to)
            .GroupBy(r => r.Category.NameVi)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var result = counts
            .Select(c => new PollutionAnalyticsItem(c.Category, c.Count))
            .OrderByDescending(i => i.Count)
            .ToList();

        logger.LogInformation(
            "Admin pollution analytics retrieved successfully with {CategoryCount} categories",
            result.Count);

        return result;
    }
}
