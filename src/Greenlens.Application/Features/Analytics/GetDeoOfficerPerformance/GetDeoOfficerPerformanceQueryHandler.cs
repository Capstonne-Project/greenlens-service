using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Application.Features.Analytics.GetAdminOfficerPerformance;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Analytics.GetDeoOfficerPerformance;

public sealed class GetDeoOfficerPerformanceQueryHandler(
    IReportRepository reports,
    IUserRepository users,
    ICurrentUser currentUser,
    IDateTimeProvider clock,
    ILogger<GetDeoOfficerPerformanceQueryHandler> logger)
    : IRequestHandler<GetDeoOfficerPerformanceQuery, Result<List<OfficerPerformanceItem>>>
{
    private const decimal SlaVerifyHours = 24m;

    public async Task<Result<List<OfficerPerformanceItem>>> Handle(
        GetDeoOfficerPerformanceQuery request, CancellationToken ct)
    {
        var scopeResult = await DepartmentContextResolver.ResolveAsync(users, currentUser, ct).ConfigureAwait(false);
        if (scopeResult.IsFailure)
            return scopeResult.Error!;

        var scope = scopeResult.Value!;
        var (from, to) = DateRangeDefaults.Resolve(request.From, request.To, clock.UtcNow);

        var verified = await DepartmentContextResolver
            .ApplyDepartmentScope(reports.QueryAsNoTracking(), scope.DepartmentId)
            .Where(r => r.VerifiedBy != null && r.VerifiedAt >= from && r.VerifiedAt <= to)
            .Select(r => new { OfficerId = r.VerifiedBy!.Value, r.CreatedAt, r.VerifiedAt, r.SlaVerifyBreached })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var officerIds = verified.Select(r => r.OfficerId).Distinct().ToList();
        var officerNames = await users.QueryAsNoTracking()
            .Where(u => officerIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName })
            .ToDictionaryAsync(u => u.Id, u => u.FullName, ct)
            .ConfigureAwait(false);

        var result = verified
            .GroupBy(r => r.OfficerId)
            .Select(g =>
            {
                var verifiedReports = g.Count();
                var averageHours = Math.Round(
                    (decimal)g.Average(r => (r.VerifiedAt!.Value - r.CreatedAt).TotalHours), 1);
                var slaRate = Math.Round(100m * g.Count(r => !r.SlaVerifyBreached) / verifiedReports, 1);
                var speedScore = Math.Clamp(100m - (averageHours / SlaVerifyHours * 100m), 0m, 100m);
                var score = Math.Round(0.7m * slaRate + 0.3m * speedScore, 1);

                return new OfficerPerformanceItem(
                    g.Key,
                    officerNames.GetValueOrDefault(g.Key, "Unknown"),
                    verifiedReports,
                    averageHours,
                    slaRate,
                    score);
            })
            .OrderByDescending(i => i.Score)
            .ToList();

        logger.LogInformation("DEO officer performance: {OfficerCount} officers", result.Count);
        return result;
    }
}
