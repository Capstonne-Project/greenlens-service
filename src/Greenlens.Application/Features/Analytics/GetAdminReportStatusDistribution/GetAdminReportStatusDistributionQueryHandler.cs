using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Analytics.GetAdminReportStatusDistribution;

/// <summary>Distribution of reports by lifecycle status, for the admin dashboard pie/bar chart.</summary>
public sealed class GetAdminReportStatusDistributionQueryHandler(
    IReportRepository reports,
    IDateTimeProvider clock)
    : IRequestHandler<GetAdminReportStatusDistributionQuery, Result<List<ReportStatusDistributionItem>>>
{
    public async Task<Result<List<ReportStatusDistributionItem>>> Handle(
        GetAdminReportStatusDistributionQuery request, CancellationToken ct)
    {
        var (from, to) = DateRangeDefaults.Resolve(request.From, request.To, clock.UtcNow);

        var counts = await reports.QueryAsNoTracking()
            .Where(r => r.CreatedAt >= from && r.CreatedAt <= to)
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var total = counts.Sum(c => c.Count);

        var result = counts
            .Select(c => new ReportStatusDistributionItem(
                c.Status,
                c.Count,
                total == 0 ? 0m : Math.Round(100m * c.Count / total, 1)))
            .OrderByDescending(i => i.Count)
            .ToList();

        return result;
    }
}
