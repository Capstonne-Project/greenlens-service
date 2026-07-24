using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Analytics.GetAdminResolutionDistribution;

/// <summary>Histogram of resolution time (VerifiedAt → ResolvedAt) for reports resolved in the range.</summary>
public sealed class GetAdminResolutionDistributionQueryHandler(
    IReportRepository reports,
    IDateTimeProvider clock)
    : IRequestHandler<GetAdminResolutionDistributionQuery, Result<List<ResolutionDistributionBucket>>>
{
    private static readonly ReportStatus[] ResolvedStatuses =
        [ReportStatus.Resolved, ReportStatus.Closed];

    public async Task<Result<List<ResolutionDistributionBucket>>> Handle(
        GetAdminResolutionDistributionQuery request, CancellationToken ct)
    {
        var (from, to) = DateRangeDefaults.Resolve(request.From, request.To, clock.UtcNow);

        var samples = await reports.QueryAsNoTracking()
            .Where(r => ResolvedStatuses.Contains(r.Status)
                        && r.VerifiedAt != null && r.ResolvedAt != null
                        && r.ResolvedAt >= from && r.ResolvedAt <= to)
            .Select(r => new { r.VerifiedAt, r.ResolvedAt })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var hours = samples
            .Select(s => (s.ResolvedAt!.Value - s.VerifiedAt!.Value).TotalHours)
            .ToList();

        var buckets = new (string Range, Func<double, bool> Match)[]
        {
            ("<2h", h => h < 2),
            ("2-24h", h => h is >= 2 and < 24),
            ("1-3d", h => h is >= 24 and < 72),
            ("3-7d", h => h is >= 72 and < 168),
            (">7d", h => h >= 168)
        };

        var result = buckets
            .Select(b => new ResolutionDistributionBucket(b.Range, hours.Count(h => b.Match(h))))
            .ToList();

        return result;
    }
}
