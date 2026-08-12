using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Application.Features.Analytics.GetAdminPollutionAnalytics;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Analytics.GetDeoPollutionAnalytics;

public sealed class GetDeoPollutionAnalyticsQueryHandler(
    IReportRepository reports,
    IUserRepository users,
    ICurrentUser currentUser,
    IDateTimeProvider clock,
    ILogger<GetDeoPollutionAnalyticsQueryHandler> logger)
    : IRequestHandler<GetDeoPollutionAnalyticsQuery, Result<List<PollutionAnalyticsItem>>>
{
    public async Task<Result<List<PollutionAnalyticsItem>>> Handle(
        GetDeoPollutionAnalyticsQuery request, CancellationToken ct)
    {
        var scopeResult = await DepartmentContextResolver.ResolveAsync(users, currentUser, ct).ConfigureAwait(false);
        if (scopeResult.IsFailure)
            return scopeResult.Error!;

        var (from, to) = DateRangeDefaults.Resolve(request.From, request.To, clock.UtcNow);

        var counts = await DepartmentContextResolver
            .ApplyDepartmentScope(reports.QueryAsNoTracking(), scopeResult.Value.DepartmentId)
            .Where(r => r.CreatedAt >= from && r.CreatedAt <= to)
            .GroupBy(r => r.Category.NameVi)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var result = counts
            .Select(c => new PollutionAnalyticsItem(c.Category, c.Count))
            .OrderByDescending(i => i.Count)
            .ToList();

        logger.LogInformation("DEO pollution analytics: {CategoryCount} categories", result.Count);
        return result;
    }
}
