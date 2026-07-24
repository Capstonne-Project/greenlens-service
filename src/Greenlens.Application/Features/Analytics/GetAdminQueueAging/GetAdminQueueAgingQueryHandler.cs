using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Analytics.GetAdminQueueAging;

/// <summary>
/// Age distribution (time since creation) of reports still pending (Submitted/Verified/InProgress).
/// </summary>
public sealed class GetAdminQueueAgingQueryHandler(
    IReportRepository reports,
    IDateTimeProvider clock)
    : IRequestHandler<GetAdminQueueAgingQuery, Result<List<QueueAgingBucket>>>
{
    private static readonly ReportStatus[] PendingStatuses =
        [ReportStatus.Submitted, ReportStatus.Verified, ReportStatus.InProgress];

    public async Task<Result<List<QueueAgingBucket>>> Handle(
        GetAdminQueueAgingQuery request, CancellationToken ct)
    {
        var now = clock.UtcNow;

        var createdAtList = await reports.QueryAsNoTracking()
            .Where(r => PendingStatuses.Contains(r.Status))
            .Select(r => r.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var buckets = new (string Range, Func<double, bool> Match)[]
        {
            ("0-6h", h => h < 6),
            ("6-24h", h => h is >= 6 and < 24),
            ("24-72h", h => h is >= 24 and < 72),
            (">72h", h => h >= 72)
        };

        var ageHours = createdAtList.Select(c => (now - c).TotalHours).ToList();

        var result = buckets
            .Select(b => new QueueAgingBucket(b.Range, ageHours.Count(h => b.Match(h))))
            .ToList();

        return result;
    }
}
