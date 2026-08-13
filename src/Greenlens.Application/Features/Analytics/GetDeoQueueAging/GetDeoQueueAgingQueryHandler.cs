using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Application.Features.Analytics.GetAdminQueueAging;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Analytics.GetDeoQueueAging;

public sealed class GetDeoQueueAgingQueryHandler(
    IReportRepository reports,
    IUserRepository users,
    ICurrentUser currentUser,
    IDateTimeProvider clock,
    ILogger<GetDeoQueueAgingQueryHandler> logger)
    : IRequestHandler<GetDeoQueueAgingQuery, Result<List<QueueAgingBucket>>>
{
    private static readonly ReportStatus[] PendingStatuses =
        [ReportStatus.Submitted, ReportStatus.Verified, ReportStatus.InProgress, ReportStatus.Reopened];

    public async Task<Result<List<QueueAgingBucket>>> Handle(
        GetDeoQueueAgingQuery request, CancellationToken ct)
    {
        var scopeResult = await DepartmentContextResolver.ResolveAsync(users, currentUser, ct).ConfigureAwait(false);
        if (scopeResult.IsFailure)
            return scopeResult.Error!;

        var now = clock.UtcNow;

        var createdAtList = await DepartmentContextResolver
            .ApplyDepartmentScope(reports.QueryAsNoTracking(), scopeResult.Value.DepartmentId)
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

        logger.LogInformation("DEO queue aging: {PendingCount} pending reports", createdAtList.Count);
        return result;
    }
}
