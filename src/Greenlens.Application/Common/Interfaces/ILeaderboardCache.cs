using Greenlens.Application.Features.Gamification.GetLeaderboard;

namespace Greenlens.Application.Common.Interfaces;

/// <summary>
/// BR-GAM-005: cache leaderboard responses (Redis production, in-memory dev fallback).
/// </summary>
public interface ILeaderboardCache
{
    Task<LeaderboardResponse?> GetAsync(string cacheKey, CancellationToken cancellationToken = default);

    Task SetAsync(string cacheKey, LeaderboardResponse response, CancellationToken cancellationToken = default);

    /// <summary>Xóa toàn bộ leaderboard cache sau khi điểm thay đổi.</summary>
    Task InvalidateAsync(CancellationToken cancellationToken = default);
}
