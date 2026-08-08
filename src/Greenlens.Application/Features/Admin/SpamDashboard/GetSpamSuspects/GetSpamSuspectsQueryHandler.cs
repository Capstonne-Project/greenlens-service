using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace Greenlens.Application.Features.Admin.SpamDashboard.GetSpamSuspects;

/// <summary>
/// Runs heuristic SQL queries to identify spam suspect users.
/// No real-time AI calls — reads pre-computed AI flags from DB.
/// </summary>
/// <remarks>Implements: BR-ADM-007.</remarks>
public sealed class GetSpamSuspectsQueryHandler(IApplicationDbContext db, ILogger<GetSpamSuspectsQueryHandler> logger)
    : IRequestHandler<GetSpamSuspectsQuery, Result<GetSpamSuspectsResponse>>
{
    public async Task<Result<GetSpamSuspectsResponse>> Handle(
        GetSpamSuspectsQuery request,
        CancellationToken ct)
    {
        logger.LogInformation("Getting spam suspects");

        var oneHourAgo = DateTime.UtcNow.AddHours(-1);
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

        logger.LogInformation("One hour ago: {OneHourAgo}", oneHourAgo);
        logger.LogInformation("Seven days ago: {SevenDaysAgo}", sevenDaysAgo);

        // Build a raw query that groups reports by reporter
        var suspectData = await db.Set<Report>()
            .AsNoTracking()
            .Where(r => r.ReporterId != null)
            .GroupBy(r => r.ReporterId!.Value)
            .Select(g => new
            {
                UserId = g.Key,
                ReportsLastHour = g.Count(r => r.CreatedAt >= oneHourAgo),
                RejectedLast7Days = g.Count(r => r.Status == ReportStatus.Rejected && r.CreatedAt >= sevenDaysAgo),
                AiFlaggedCount = g.Count(r => r.IsSuspicious)
            })
            .Where(x => x.ReportsLastHour >= request.MinReportsPerHour
                      || x.RejectedLast7Days >= request.MinRejected7Days
                      || x.AiFlaggedCount >= request.MinAiFlagged)
            .OrderByDescending(x => x.ReportsLastHour + x.RejectedLast7Days + x.AiFlaggedCount)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        logger.LogInformation("Suspect data: {SuspectData}", suspectData);

        var totalCount = suspectData.Count;
        logger.LogInformation("Total count: {TotalCount}", totalCount);

        var pagedData = suspectData
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        logger.LogInformation("Paged data: {PagedData}", pagedData);

        // Load user details for the paged suspects
        var userIds = pagedData.Select(x => x.UserId).ToList();
        var users = await db.Set<User>()
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName, u.Email, u.IsBanned })
            .ToDictionaryAsync(u => u.Id, ct)
            .ConfigureAwait(false);

        var items = pagedData.Select(x =>
        {
            var user = users.GetValueOrDefault(x.UserId);
            var reasons = new List<string>();
            if (x.ReportsLastHour >= request.MinReportsPerHour) reasons.Add($"Submit ≥ {x.ReportsLastHour}/h");
            if (x.RejectedLast7Days >= request.MinRejected7Days) reasons.Add($"Rejected ≥ {x.RejectedLast7Days}/7d");
            if (x.AiFlaggedCount >= request.MinAiFlagged) reasons.Add($"AI flagged: {x.AiFlaggedCount}");

            return new SpamSuspectItem(
                x.UserId,
                user?.FullName ?? "Unknown",
                user?.Email ?? "Unknown",
                user?.IsBanned ?? false,
                x.ReportsLastHour,
                x.RejectedLast7Days,
                x.AiFlaggedCount,
                string.Join("; ", reasons));
        }).ToList();

        logger.LogInformation("Spam suspects retrieved successfully");

        return new GetSpamSuspectsResponse(items, totalCount);
    }
}
