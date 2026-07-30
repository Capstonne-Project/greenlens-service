using System.IO.Compression;
using System.Text.Json;
using System.Threading.RateLimiting;
using Greenlens.Api.Configuration;
using Greenlens.Api.RateLimiting;
using Greenlens.Application.Common.Models;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.RateLimiting;

namespace Greenlens.Api.Extensions;

/// <summary>
/// P0 performance: global rate limit (BR-SYS-004) + response compression.
/// </summary>
public static class PerformanceServiceExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static IServiceCollection AddGreenlensPerformance(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<ApiRateLimitOptions>()
            .Bind(configuration.GetSection(ApiRateLimitOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<ApiRateLimitPartitionResolver>();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                var httpContext = context.HttpContext;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    httpContext.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
                }

                var response = new ApiResponse
                {
                    Code = "API_RATE_LIMIT_EXCEEDED",
                    Message = "Quá nhiều yêu cầu. Vui lòng thử lại sau.",
                    Status = 429,
                    Data = null
                };

                httpContext.Response.ContentType = "application/json";
                await httpContext.Response
                    .WriteAsync(JsonSerializer.Serialize(response, JsonOptions), cancellationToken)
                    .ConfigureAwait(false);
            };

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var resolver = httpContext.RequestServices.GetRequiredService<ApiRateLimitPartitionResolver>();
                var partition = resolver.Resolve(httpContext);
                return partition;
            });
        });

        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
            [
                "application/json",
                "application/problem+json"
            ]);
        });

        services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });

        services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });

        return services;
    }
}
