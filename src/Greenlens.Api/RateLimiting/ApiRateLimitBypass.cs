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
        // Lấy path từ request
        var path = context.Request.Path;
        // Kiểm tra nếu path là health
        if (path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase))
            return true;

        // Kiểm tra nếu path có prefix trong Prefixes
        foreach (var prefix in Prefixes)
        {
            // Kiểm tra nếu path có prefix trong Prefixes
            if (path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase))
                // Trả về true
                return true;
        }

        // Trả về false
        return false;
    }
}
