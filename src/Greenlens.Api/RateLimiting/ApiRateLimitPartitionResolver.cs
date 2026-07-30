using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Greenlens.Api.Configuration;
using Microsoft.Extensions.Options;

namespace Greenlens.Api.RateLimiting;

internal sealed class ApiRateLimitPartitionResolver(IOptions<ApiRateLimitOptions> options)
{
    public RateLimitPartition<string> Resolve(HttpContext context)
    {
        if (ApiRateLimitBypass.ShouldBypass(context))
            return RateLimitPartition.GetNoLimiter("bypass");

        var opts = options.Value;
        var window = TimeSpan.FromSeconds(opts.WindowSeconds);

        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? "unknown-user";

            return RateLimitPartition.GetSlidingWindowLimiter(
                $"user:{userId}",
                _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = opts.AuthenticatedPermitLimit,
                    Window = window,
                    SegmentsPerWindow = opts.SegmentsPerWindow,
                    QueueLimit = 0
                });
        }

        var clientIp = ResolveClientIp(context);
        return RateLimitPartition.GetSlidingWindowLimiter(
            $"ip:{clientIp}",
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = opts.AnonymousPermitLimit,
                Window = window,
                SegmentsPerWindow = opts.SegmentsPerWindow,
                QueueLimit = 0
            });
    }

    private static string ResolveClientIp(HttpContext context)
    {
        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            var first = forwarded.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(first))
                return first;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
