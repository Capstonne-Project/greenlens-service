namespace Greenlens.Api.Attributes;

/// <summary>
/// Marks an action as idempotency-aware when the client sends <c>Idempotency-Key</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SupportsIdempotencyAttribute : Attribute
{
    /// <summary>Record TTL in Redis/memory. Default 24h for mutations; use 1 for auth.</summary>
    public int TtlHours { get; init; } = 24;

    /// <summary>When true, missing header returns 422. Phase 1 keeps false (optional header).</summary>
    public bool Required { get; init; }
}
