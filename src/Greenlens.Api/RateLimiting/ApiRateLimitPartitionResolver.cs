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
        // Kiểm tra nếu bypass rate limit
        if (ApiRateLimitBypass.ShouldBypass(context))
            return RateLimitPartition.GetNoLimiter("bypass");

        // Lấy cấu hình rate limit
        var opts = options.Value;
        // Tính toán window size
        var window = TimeSpan.FromSeconds(opts.WindowSeconds);

        // Kiểm tra nếu user đã đăng nhập
        if (context.User.Identity?.IsAuthenticated == true)
        {
            // Lấy user ID
            var userId = context.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? "unknown-user";

            // Tạo limiter cho user
            return RateLimitPartition.GetSlidingWindowLimiter(
                $"user:{userId}",
                _ => new SlidingWindowRateLimiterOptions
                // Thêm cấu hình cho limiter
                {
                    PermitLimit = opts.AuthenticatedPermitLimit,
                    Window = window,
                    SegmentsPerWindow = opts.SegmentsPerWindow,
                    QueueLimit = 0
                });
        }

        // Lấy IP client
        var clientIp = ResolveClientIp(context);
        // Tạo limiter cho IP
        return RateLimitPartition.GetSlidingWindowLimiter(
            $"ip:{clientIp}",
            _ => new SlidingWindowRateLimiterOptions
            {
                // Thêm cấu hình cho limiter
                PermitLimit = opts.AnonymousPermitLimit,
                Window = window,
                SegmentsPerWindow = opts.SegmentsPerWindow,
                QueueLimit = 0
            });
    }

    private static string ResolveClientIp(HttpContext context)
    {
        // Lấy IP từ header X-Forwarded-For
        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            // Lấy IP đầu tiên
            var first = forwarded.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(first))
                // Trả về IP đầu tiên
                return first;
        }

        // Trả về IP remote
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
