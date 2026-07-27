using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Gamification;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Gamification.GetLeaderboard;

/// <summary>
/// Public leaderboard query. Only shows users who have NOT opted for anonymous reporting (BR-GAM-002/005).
/// </summary>
/// <remarks>Implements: BR-GAM-005.</remarks>
public sealed record GetLeaderboardQuery(
    LeaderboardPeriod Period = LeaderboardPeriod.AllTime,
    int Top = 10,
    int? Year = null,
    int? Month = null) : IRequest<Result<LeaderboardResponse>>;

public sealed record LeaderboardResponse(
    LeaderboardPeriod Period,
    int? Year,
    int? Month,
    DateTime? PeriodStart,
    DateTime? PeriodEnd,
    IReadOnlyList<LeaderboardEntry> Entries);

public sealed record LeaderboardEntry(
    int Rank,
    Guid UserId,
    string DisplayName,
    string? AvatarUrl,
    int Points,
    int Level);

public sealed class GetLeaderboardQueryHandler(
    IUserPointsRepository userPointsRepo)
    : IRequestHandler<GetLeaderboardQuery, Result<LeaderboardResponse>>
{
    public async Task<Result<LeaderboardResponse>> Handle(
        GetLeaderboardQuery request, CancellationToken ct)
    {
        if (request.Period == LeaderboardPeriod.AllTime)
        {
            var allTimeEntries = await userPointsRepo.QueryAsNoTracking()
                .Where(up => !up.IsLocked && up.TotalPoints > 0)
                .OrderByDescending(up => up.TotalPoints)
                .Take(request.Top)
                .Select(up => new
                {
                    up.UserId,
                    up.User!.FullName,
                    up.User.AvatarUrl,
                    up.TotalPoints
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var allTimeRanked = allTimeEntries.Select((e, i) => new LeaderboardEntry(
                Rank: i + 1,
                e.UserId,
                e.FullName,
                e.AvatarUrl,
                e.TotalPoints,
                GamificationHelpers.CalculateLevel(e.TotalPoints))).ToList();

            return new LeaderboardResponse(
                LeaderboardPeriod.AllTime,
                Year: null,
                Month: null,
                PeriodStart: null,
                PeriodEnd: null,
                allTimeRanked);
        }

        var (periodStart, periodEnd, year, month) = GamificationHelpers.GetPeriodRange(
            request.Period,
            request.Year,
            request.Month);

        var entries = await userPointsRepo.QueryAsNoTracking()
            .Where(up => !up.IsLocked)
            .Select(up => new
            {
                up.UserId,
                up.User!.FullName,
                up.User.AvatarUrl,
                up.TotalPoints,
                PeriodPoints = up.Transactions
                    .Where(t => t.CreatedAt >= periodStart && t.CreatedAt < periodEnd)
                    .Sum(t => t.Points)
            })
            .Where(x => x.PeriodPoints > 0)
            .OrderByDescending(x => x.PeriodPoints)
            .Take(request.Top)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var ranked = entries.Select((e, i) => new LeaderboardEntry(
            Rank: i + 1,
            e.UserId,
            e.FullName,
            e.AvatarUrl,
            e.PeriodPoints,
            GamificationHelpers.CalculateLevel(e.TotalPoints))).ToList();

        return new LeaderboardResponse(request.Period, year, month, periodStart, periodEnd, ranked);
    }
}
