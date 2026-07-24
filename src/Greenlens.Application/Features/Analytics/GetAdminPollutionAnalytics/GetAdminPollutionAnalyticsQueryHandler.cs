using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Analytics.GetAdminPollutionAnalytics;

/// <summary>Report count per pollution category, for the admin dashboard category breakdown chart.</summary>
public sealed class GetAdminPollutionAnalyticsQueryHandler(
    IReportRepository reports,
    IDateTimeProvider clock)
    : IRequestHandler<GetAdminPollutionAnalyticsQuery, Result<List<PollutionAnalyticsItem>>>
{
    public async Task<Result<List<PollutionAnalyticsItem>>> Handle(
        GetAdminPollutionAnalyticsQuery request, CancellationToken ct)
    {
        var (from, to) = DateRangeDefaults.Resolve(request.From, request.To, clock.UtcNow);

        var result = await reports.QueryAsNoTracking()
            .Include(r => r.Category)
            .Where(r => r.CreatedAt >= from && r.CreatedAt <= to)
            .GroupBy(r => r.Category.NameVi)
            .Select(g => new PollutionAnalyticsItem(g.Key, g.Count()))
            .OrderByDescending(i => i.Count)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return result;
    }
}
