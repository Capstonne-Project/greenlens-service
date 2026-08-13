using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Application.Features.Analytics.GetAdminResolutionDistribution;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Analytics.GetDeoResolutionDistribution;

public sealed class GetDeoResolutionDistributionQueryHandler(
    IReportRepository reports,
    IUserRepository users,
    ICurrentUser currentUser,
    IDateTimeProvider clock,
    ILogger<GetDeoResolutionDistributionQueryHandler> logger)
    : IRequestHandler<GetDeoResolutionDistributionQuery, Result<List<ResolutionDistributionBucket>>>
{
    private static readonly ReportStatus[] ResolvedStatuses =
        [ReportStatus.Resolved, ReportStatus.Closed];

    public async Task<Result<List<ResolutionDistributionBucket>>> Handle(
        GetDeoResolutionDistributionQuery request, CancellationToken ct)
    {
        var scopeResult = await DepartmentContextResolver.ResolveAsync(users, currentUser, ct).ConfigureAwait(false);
        if (scopeResult.IsFailure)
            return scopeResult.Error!;

        var scope = scopeResult.Value!;
        var (from, to) = DateRangeDefaults.Resolve(request.From, request.To, clock.UtcNow);

        var samples = await DepartmentContextResolver
            .ApplyDepartmentScope(reports.QueryAsNoTracking(), scope.DepartmentId)
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

        logger.LogInformation("DEO resolution distribution: {SampleCount} samples", samples.Count);
        return result;
    }
}
