using Greenlens.Application.Common.Idempotency;

namespace Greenlens.Application.Common.Interfaces;

/// <summary>
/// Stores idempotency records for safe HTTP replay (double-submit protection).
/// </summary>
public interface IIdempotencyStore
{
    Task<IdempotencyAcquireResult> TryAcquireAsync(
        string scopeKey,
        string requestBodyHash,
        TimeSpan ttl,
        CancellationToken cancellationToken = default);

    Task CompleteAsync(
        string scopeKey,
        int statusCode,
        string responseBodyJson,
        CancellationToken cancellationToken = default);

    /// <summary>Removes in-flight lock when the handler returned a non-success HTTP result.</summary>
    Task ReleaseAsync(string scopeKey, CancellationToken cancellationToken = default);
}
