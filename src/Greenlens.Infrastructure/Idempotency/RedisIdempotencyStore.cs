using System.Text.Json;
using Greenlens.Application.Common.Idempotency;
using Greenlens.Application.Common.Interfaces;
using StackExchange.Redis;

namespace Greenlens.Infrastructure.Idempotency;

internal sealed class RedisIdempotencyStore(IConnectionMultiplexer redis) : IIdempotencyStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private sealed record RedisEntry(
        string State,
        string BodyHash,
        int StatusCode,
        string BodyJson);

    public async Task<IdempotencyAcquireResult> TryAcquireAsync(
        string scopeKey,
        string requestBodyHash,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        var db = redis.GetDatabase();
        var redisKey = (RedisKey)$"idempotency:{scopeKey}";

        var existingJson = await db.StringGetAsync(redisKey).ConfigureAwait(false);
        if (!existingJson.IsNullOrEmpty)
        {
            var existing = JsonSerializer.Deserialize<RedisEntry>(existingJson!, JsonOptions)!;
            if (existing.State == "completed")
            {
                if (!string.Equals(existing.BodyHash, requestBodyHash, StringComparison.Ordinal))
                    return new IdempotencyAcquireResult(IdempotencyAcquireOutcome.BodyMismatch);

                return new IdempotencyAcquireResult(
                    IdempotencyAcquireOutcome.Replay,
                    new IdempotencyCachedResponse(existing.StatusCode, existing.BodyJson));
            }

            return new IdempotencyAcquireResult(IdempotencyAcquireOutcome.InProgress);
        }

        var processing = JsonSerializer.Serialize(new RedisEntry("processing", requestBodyHash, 0, string.Empty), JsonOptions);
        var acquired = await db.StringSetAsync(redisKey, processing, ttl, When.NotExists).ConfigureAwait(false);

        if (!acquired)
        {
            existingJson = await db.StringGetAsync(redisKey).ConfigureAwait(false);
            if (!existingJson.IsNullOrEmpty)
            {
                var existing = JsonSerializer.Deserialize<RedisEntry>(existingJson!, JsonOptions)!;
                if (existing.State == "completed")
                {
                    if (!string.Equals(existing.BodyHash, requestBodyHash, StringComparison.Ordinal))
                        return new IdempotencyAcquireResult(IdempotencyAcquireOutcome.BodyMismatch);

                    return new IdempotencyAcquireResult(
                        IdempotencyAcquireOutcome.Replay,
                        new IdempotencyCachedResponse(existing.StatusCode, existing.BodyJson));
                }
            }

            return new IdempotencyAcquireResult(IdempotencyAcquireOutcome.InProgress);
        }

        return new IdempotencyAcquireResult(IdempotencyAcquireOutcome.Acquired);
    }

    public async Task CompleteAsync(
        string scopeKey,
        int statusCode,
        string responseBodyJson,
        CancellationToken cancellationToken = default)
    {
        var db = redis.GetDatabase();
        var redisKey = (RedisKey)$"idempotency:{scopeKey}";

        var ttl = await db.KeyTimeToLiveAsync(redisKey).ConfigureAwait(false);
        if (ttl is null)
            ttl = TimeSpan.FromHours(24);

        var existingJson = await db.StringGetAsync(redisKey).ConfigureAwait(false);
        if (existingJson.IsNullOrEmpty)
            return;

        var existing = JsonSerializer.Deserialize<RedisEntry>(existingJson!, JsonOptions)!;
        var completed = new RedisEntry("completed", existing.BodyHash, statusCode, responseBodyJson);
        await db.StringSetAsync(
            redisKey,
            JsonSerializer.Serialize(completed, JsonOptions),
            ttl.Value).ConfigureAwait(false);
    }

    public async Task ReleaseAsync(string scopeKey, CancellationToken cancellationToken = default)
    {
        var db = redis.GetDatabase();
        var redisKey = (RedisKey)$"idempotency:{scopeKey}";

        var existingJson = await db.StringGetAsync(redisKey).ConfigureAwait(false);
        if (existingJson.IsNullOrEmpty)
            return;

        var existing = JsonSerializer.Deserialize<RedisEntry>(existingJson!, JsonOptions)!;
        if (existing.State == "processing")
            await db.KeyDeleteAsync(redisKey).ConfigureAwait(false);
    }
}
