using System.Text.Json;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Features.Gamification.GetLeaderboard;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Greenlens.Infrastructure.Gamification;

/// <summary>BR-GAM-005: Redis string cache cho leaderboard, TTL 5 phút.</summary>
internal sealed class RedisLeaderboardCache(
    IConnectionMultiplexer redis,
    ILogger<RedisLeaderboardCache> logger) : ILeaderboardCache
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private const string KeyPrefix = "gamification:leaderboard:";

    public async Task<LeaderboardResponse?> GetAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        var json = await redis.GetDatabase()
            .StringGetAsync(cacheKey)
            .ConfigureAwait(false);

        if (json.IsNullOrEmpty)
            return null;

        return JsonSerializer.Deserialize<LeaderboardResponse>((string)json!, JsonOptions);
    }

    public async Task SetAsync(
        string cacheKey,
        LeaderboardResponse response,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(response, JsonOptions);
        await redis.GetDatabase()
            .StringSetAsync(cacheKey, json, Ttl)
            .ConfigureAwait(false);
    }

    public async Task InvalidateAsync(CancellationToken cancellationToken = default)
    {
        var db = redis.GetDatabase();
        var deleted = 0;

        // SCAN theo prefix — invalidation ít xảy ra (sau award/lock), chấp nhận được.
        foreach (var endpoint in redis.GetEndPoints())
        {
            var server = redis.GetServer(endpoint);
            if (!server.IsConnected)
                continue;

            await foreach (var key in server.KeysAsync(pattern: $"{KeyPrefix}*").WithCancellation(cancellationToken))
            {
                await db.KeyDeleteAsync(key).ConfigureAwait(false);
                deleted++;
            }
        }

        logger.LogDebug("Leaderboard Redis cache invalidated ({DeletedKeys} keys)", deleted);
    }
}
