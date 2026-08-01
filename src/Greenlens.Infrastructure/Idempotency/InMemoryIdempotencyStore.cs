using System.Collections.Concurrent;
using Greenlens.Application.Common.Idempotency;
using Greenlens.Application.Common.Interfaces;

namespace Greenlens.Infrastructure.Idempotency;

/// <summary>Dev / single-node fallback when Redis is not configured.</summary>
internal sealed class InMemoryIdempotencyStore : IIdempotencyStore
{
    private sealed record Entry(
        string State,
        string BodyHash,
        int StatusCode,
        string BodyJson,
        DateTimeOffset ExpiresAt);

    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);

    public Task<IdempotencyAcquireResult> TryAcquireAsync(
        string scopeKey,
        string requestBodyHash,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        PurgeExpired();

        if (_entries.TryGetValue(scopeKey, out var existing) && existing.ExpiresAt > DateTimeOffset.UtcNow)
            return Task.FromResult(EvaluateExisting(existing, requestBodyHash));

        var expiresAt = DateTimeOffset.UtcNow.Add(ttl);
        var processing = new Entry("processing", requestBodyHash, 0, string.Empty, expiresAt);

        if (_entries.TryAdd(scopeKey, processing))
            return Task.FromResult(new IdempotencyAcquireResult(IdempotencyAcquireOutcome.Acquired));

        if (_entries.TryGetValue(scopeKey, out existing) && existing.ExpiresAt > DateTimeOffset.UtcNow)
            return Task.FromResult(EvaluateExisting(existing, requestBodyHash));

        _entries[scopeKey] = processing;
        return Task.FromResult(new IdempotencyAcquireResult(IdempotencyAcquireOutcome.Acquired));
    }

    public Task CompleteAsync(
        string scopeKey,
        int statusCode,
        string responseBodyJson,
        CancellationToken cancellationToken = default)
    {
        if (!_entries.TryGetValue(scopeKey, out var existing))
            return Task.CompletedTask;

        _entries[scopeKey] = existing with
        {
            State = "completed",
            StatusCode = statusCode,
            BodyJson = responseBodyJson
        };

        return Task.CompletedTask;
    }

    public Task ReleaseAsync(string scopeKey, CancellationToken cancellationToken = default)
    {
        if (_entries.TryGetValue(scopeKey, out var existing) && existing.State == "processing")
            _entries.TryRemove(scopeKey, out _);

        return Task.CompletedTask;
    }

    private static IdempotencyAcquireResult EvaluateExisting(Entry existing, string requestBodyHash)
    {
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

    private void PurgeExpired()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var key in _entries.Keys)
        {
            if (_entries.TryGetValue(key, out var entry) && entry.ExpiresAt <= now)
                _entries.TryRemove(key, out _);
        }
    }
}
