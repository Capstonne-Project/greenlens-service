# Caching Patterns — Multi-Level Cache with Redis

> **Source:** CLAUDE.md §4.6 (BR-MAP-012), §10 Performance & Scaling

## Architecture

```
Request → L1 (In-Memory) → L2 (Redis) → Database
              5s TTL           10min TTL      Source of truth
```

## Cache Interface (Application Layer)

```csharp
// Application/Common/Interfaces/ICacheService.cs
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);
}
```

## Redis Implementation

```csharp
// Infrastructure/Caching/RedisCacheService.cs
public sealed class RedisCacheService(
    IDistributedCache distributedCache,
    IMemoryCache memoryCache,
    ILogger<RedisCacheService> logger) : ICacheService
{
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan L1Expiry = TimeSpan.FromSeconds(5);

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        // L1: In-Memory (fast path)
        if (memoryCache.TryGetValue(key, out T? cached))
            return cached;

        // L2: Redis
        var bytes = await distributedCache
            .GetAsync(key, ct)
            .ConfigureAwait(false);

        if (bytes is null)
            return default;

        var value = JsonSerializer.Deserialize<T>(bytes);

        // Promote to L1
        if (value is not null)
            memoryCache.Set(key, value, L1Expiry);

        return value;
    }

    public async Task SetAsync<T>(
        string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        var ttl = expiry ?? DefaultExpiry;

        // L1
        memoryCache.Set(key, value, L1Expiry);

        // L2
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
        await distributedCache.SetAsync(key, bytes,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
            ct).ConfigureAwait(false);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        memoryCache.Remove(key);
        await distributedCache.RemoveAsync(key, ct).ConfigureAwait(false);
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        // Note: Requires Redis SCAN — use StackExchange.Redis IConnectionMultiplexer
        // for prefix-based invalidation
        logger.LogWarning("RemoveByPrefix called for {Prefix} — implement with SCAN", prefix);
    }
}
```

## Cache Key Conventions

| Resource | Key Pattern | TTL | BR |
|----------|-----------|-----|-----|
| Map nearby | `map:nearby:{lat}:{lng}:{radius}` | 10 min | BR-MAP-012 |
| Map hotspots | `map:hotspots:{bbox}:{filters}` | 10 min | BR-MAP-012 |
| Map heatmap | `map:heatmap:{bbox}` | 10 min | BR-MAP-012 |
| Leaderboard | `gamification:leaderboard:{period}` | 1 hour | BR-GAM-005 |
| Report detail | `report:{id}` | 5 min | — |
| User profile | `user:{id}:profile` | 5 min | — |
| Rate limit | `ratelimit:{userId}:{action}` | 1-24h | BR-REP-010 |

## Caching Behavior (MediatR Pipeline)

```csharp
// Application/Common/Behaviors/CachingBehavior.cs
public sealed class CachingBehavior<TRequest, TResponse>(
    ICacheService cache,
    ILogger<CachingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICacheableQuery<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var cacheKey = request.CacheKey;
        var cached = await cache.GetAsync<TResponse>(cacheKey, ct)
            .ConfigureAwait(false);

        if (cached is not null)
        {
            logger.LogDebug("Cache HIT for {Key}", cacheKey);
            return cached;
        }

        logger.LogDebug("Cache MISS for {Key}", cacheKey);
        var response = await next().ConfigureAwait(false);

        await cache.SetAsync(cacheKey, response, request.CacheDuration, ct)
            .ConfigureAwait(false);

        return response;
    }
}

// Marker interface for cacheable queries
public interface ICacheableQuery<TResponse> : IRequest<TResponse>
{
    string CacheKey { get; }
    TimeSpan? CacheDuration { get; }
}
```

## Usage in Query

```csharp
public sealed record GetHotspotsQuery(double MinLat, double MinLng, double MaxLat, double MaxLng)
    : ICacheableQuery<Result<List<HotspotDto>>>
{
    public string CacheKey => $"map:hotspots:{MinLat}:{MinLng}:{MaxLat}:{MaxLng}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);  // BR-MAP-012
}
```

## Cache Invalidation

```csharp
// In command handlers — invalidate after mutation
public async Task<Result> Handle(VerifyReportCommand request, CancellationToken ct)
{
    // ... verify report ...

    // Invalidate related caches
    await cache.RemoveAsync($"report:{request.ReportId}", ct).ConfigureAwait(false);
    await cache.RemoveByPrefixAsync("map:", ct).ConfigureAwait(false);

    return Result.Success();
}
```

## Rate Limiting with Redis (BR-REP-010)

```csharp
// Sliding window rate limit using Redis sorted set
public async Task<bool> IsRateLimitedAsync(
    Guid userId, string action, int maxCount, TimeSpan window, CancellationToken ct)
{
    var key = $"ratelimit:{userId}:{action}";
    var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    var windowStart = now - (long)window.TotalMilliseconds;

    // Using StackExchange.Redis directly for sorted set operations
    var db = redis.GetDatabase();
    await db.SortedSetRemoveRangeByScoreAsync(key, 0, windowStart);
    var count = await db.SortedSetLengthAsync(key);

    if (count >= maxCount)
        return true;  // Rate limited

    await db.SortedSetAddAsync(key, now.ToString(), now);
    await db.KeyExpireAsync(key, window);
    return false;
}
```

## DO / DON'T

```csharp
// ✅ DO — Cache read-heavy, rarely-changing data
// Map data, leaderboards, public reports list

// ✅ DO — Set appropriate TTL per use case
await cache.SetAsync(key, data, TimeSpan.FromMinutes(10), ct);

// ✅ DO — Invalidate on write
await cache.RemoveAsync($"report:{id}", ct);

// ❌ DON'T — Cache user-specific mutable data with long TTL
await cache.SetAsync($"user:{id}", userData, TimeSpan.FromHours(24), ct); // Too long!

// ❌ DON'T — Cache without considering staleness
// Ask: "Is it OK if this data is 10 minutes old?"

// ❌ DON'T — Use cache as primary data store
// Redis is cache, PostgreSQL is source of truth
```
