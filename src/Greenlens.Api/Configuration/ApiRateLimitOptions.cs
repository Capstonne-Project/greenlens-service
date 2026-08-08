using System.ComponentModel.DataAnnotations;

namespace Greenlens.Api.Configuration;

/// <summary>
/// Global API rate limits (BR-SYS-004).
/// </summary>
public sealed class ApiRateLimitOptions
{
    public const string SectionName = "ApiRateLimit";

    /// <summary>Anonymous requests per IP per window (default 60/min).</summary>
    [Range(1, 10_000)]
    public int AnonymousPermitLimit { get; init; } = 60;

    /// <summary>Authenticated requests per user per window (default 300/min).</summary>
    [Range(1, 10_000)]
    public int AuthenticatedPermitLimit { get; init; } = 300;

    /// <summary>Sliding window size in seconds (default 60).</summary>
    [Range(1, 3600)]
    public int WindowSeconds { get; init; } = 60;

    /// <summary>Sliding window segments (default 6 → 10s buckets).</summary>
    [Range(1, 60)]
    public int SegmentsPerWindow { get; init; } = 6;
}
