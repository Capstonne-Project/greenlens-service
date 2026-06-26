using Greenlens.Application.Common.Interfaces.Persistence;
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
    LeaderboardPeriod Period = LeaderboardPeriod.Monthly,
    int Top = 10) : IRequest<Result<LeaderboardResponse>>;

public sealed record LeaderboardResponse(
    LeaderboardPeriod Period,
    DateTime PeriodStart,
    DateTime PeriodEnd,
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
        var (periodStart, periodEnd) = GetPeriodRange(request.Period);

        // Query: sum points in period, only unlocked users, ranked
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
            Level: e.TotalPoints switch
            {
                >= 5000 => 5,
                >= 1500 => 4,
                >= 500 => 3,
                >= 100 => 2,
                _ => 1
            })).ToList();

        return new LeaderboardResponse(request.Period, periodStart, periodEnd, ranked);
    }

    private static (DateTime Start, DateTime End) GetPeriodRange(LeaderboardPeriod period)
    {
        var now = DateTime.UtcNow;
        return period switch
        {
            LeaderboardPeriod.Weekly => (
                now.AddDays(-(int)now.DayOfWeek).Date,
                now.AddDays(7 - (int)now.DayOfWeek).Date),
            LeaderboardPeriod.Monthly => (
                new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1)),
            LeaderboardPeriod.Yearly => (
                new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(now.Year + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            _ => throw new ArgumentOutOfRangeException(nameof(period))
        };
    }
}
