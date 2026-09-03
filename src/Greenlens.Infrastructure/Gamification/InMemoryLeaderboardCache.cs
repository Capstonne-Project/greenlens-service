using System.Collections.Concurrent;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Features.Gamification.GetLeaderboard;

namespace Greenlens.Infrastructure.Gamification;

/// <summary>BR-GAM-005 fallback khi không có Redis (local dev / single-node tests).</summary>
internal sealed class InMemoryLeaderboardCache : ILeaderboardCache
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    private readonly ConcurrentDictionary<string, CacheEntry> _entries = new();

    private sealed record CacheEntry(LeaderboardResponse Value, DateTimeOffset ExpiresAt);

    public Task<LeaderboardResponse?> GetAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        if (!_entries.TryGetValue(cacheKey, out var entry))
            return Task.FromResult<LeaderboardResponse?>(null);

        if (entry.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _entries.TryRemove(cacheKey, out _);
            return Task.FromResult<LeaderboardResponse?>(null);
        }

        return Task.FromResult<LeaderboardResponse?>(entry.Value);
    }

    public Task SetAsync(
        string cacheKey,
        LeaderboardResponse response,
        CancellationToken cancellationToken = default)
    {
        _entries[cacheKey] = new CacheEntry(response, DateTimeOffset.UtcNow.Add(Ttl));
        return Task.CompletedTask;
    }

    public Task InvalidateAsync(CancellationToken cancellationToken = default)
    {
        _entries.Clear();
        return Task.CompletedTask;
    }
}
