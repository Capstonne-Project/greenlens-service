using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Application.Features.Analytics.GetAdminReportStatusDistribution;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Analytics.GetDeoReportStatusDistribution;

public sealed class GetDeoReportStatusDistributionQueryHandler(
    IReportRepository reports,
    IUserRepository users,
    ICurrentUser currentUser,
    IDateTimeProvider clock,
    ILogger<GetDeoReportStatusDistributionQueryHandler> logger)
    : IRequestHandler<GetDeoReportStatusDistributionQuery, Result<List<ReportStatusDistributionItem>>>
{
    public async Task<Result<List<ReportStatusDistributionItem>>> Handle(
        GetDeoReportStatusDistributionQuery request, CancellationToken ct)
    {
        var scopeResult = await DepartmentContextResolver.ResolveAsync(users, currentUser, ct).ConfigureAwait(false);
        if (scopeResult.IsFailure)
            return scopeResult.Error!;

        var (from, to) = DateRangeDefaults.Resolve(request.From, request.To, clock.UtcNow);

        var counts = await DepartmentContextResolver
            .ApplyDepartmentScope(reports.QueryAsNoTracking(), scopeResult.Value.DepartmentId)
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

        logger.LogInformation("DEO report status distribution: {Count} statuses", result.Count);
        return result;
    }
}
