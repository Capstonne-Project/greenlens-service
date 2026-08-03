namespace Greenlens.Application.Common.Idempotency;

public enum IdempotencyAcquireOutcome
{
    Acquired,
    Replay,
    InProgress,
    BodyMismatch
}

public sealed record IdempotencyCachedResponse(int StatusCode, string BodyJson);

public sealed record IdempotencyAcquireResult(
    IdempotencyAcquireOutcome Outcome,
    IdempotencyCachedResponse? Cached = null);
