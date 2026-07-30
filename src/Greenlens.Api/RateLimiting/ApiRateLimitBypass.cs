namespace Greenlens.Api.RateLimiting;

/// <summary>
/// Paths excluded from global API rate limiting (health, docs, SignalR, Hangfire).
/// </summary>
internal static class ApiRateLimitBypass
{
    private static readonly string[] Prefixes =
    [
        "/health",
        "/swagger",
        "/hangfire",
        "/hubs/"
    ];

    internal static bool ShouldBypass(HttpContext context)
    {
        var path = context.Request.Path;
        if (path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase))
            return true;

        foreach (var prefix in Prefixes)
        {
            if (path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
