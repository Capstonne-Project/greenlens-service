using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace Greenlens.Application.Features.Analytics.GetAdminOfficerPerformance;

/// <summary>
/// LEO verification KPIs: report volume, average time-to-verify, SLA-verify compliance rate.
/// Score: 70% SlaRate + 30% normalized speed (faster than 24h SLA-verify window scores higher).
/// </summary>
public sealed class GetAdminOfficerPerformanceQueryHandler(
    IReportRepository reports,
    IUserRepository users,
    IDateTimeProvider clock,
    ILogger<GetAdminOfficerPerformanceQueryHandler> logger)
    : IRequestHandler<GetAdminOfficerPerformanceQuery, Result<List<OfficerPerformanceItem>>>
{
    private const decimal SlaVerifyHours = 24m;

    public async Task<Result<List<OfficerPerformanceItem>>> Handle(
        GetAdminOfficerPerformanceQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting admin officer performance");

        var (from, to) = DateRangeDefaults.Resolve(request.From, request.To, clock.UtcNow);

        var verified = await reports.QueryAsNoTracking()
            .Where(r => r.VerifiedBy != null && r.VerifiedAt >= from && r.VerifiedAt <= to)
            .Select(r => new { OfficerId = r.VerifiedBy!.Value, r.CreatedAt, r.VerifiedAt, r.SlaVerifyBreached })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        logger.LogInformation("Verified: {Verified}", verified);

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

        logger.LogInformation("Admin officer performance retrieved successfully");

        return result;
    }
}
