using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Greenlens.Api.Attributes;
using Greenlens.Application.Common.Idempotency;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace Greenlens.Api.Filters;

/// <summary>
/// Replays cached API envelope for duplicate <c>Idempotency-Key</c> requests (double-submit protection).
/// </summary>
internal sealed class IdempotencyActionFilter(IIdempotencyStore store) : IAsyncActionFilter
{
    public const string HeaderName = "Idempotency-Key";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var attribute = context.ActionDescriptor.EndpointMetadata
            .OfType<SupportsIdempotencyAttribute>()
            .FirstOrDefault();

        if (attribute is null)
        {
            await next().ConfigureAwait(false);
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var keyValues)
            || string.IsNullOrWhiteSpace(keyValues.ToString()))
        {
            if (attribute.Required)
            {
                context.Result = ErrorResult(422, "IDEMPOTENCY_KEY_REQUIRED", "Thiếu header Idempotency-Key.");
                return;
            }

            await next().ConfigureAwait(false);
            return;
        }

        var clientKey = keyValues.ToString().Trim();
        if (clientKey.Length > 128)
        {
            context.Result = ErrorResult(422, "IDEMPOTENCY_KEY_INVALID", "Idempotency-Key tối đa 128 ký tự.");
            return;
        }

        var bodyHash = await ComputeRequestBodyHashAsync(context.HttpContext.Request).ConfigureAwait(false);
        var scopeKey = BuildScopeKey(context, clientKey);
        var ttl = TimeSpan.FromHours(attribute.TtlHours);

        var acquire = await store.TryAcquireAsync(scopeKey, bodyHash, ttl, context.HttpContext.RequestAborted)
            .ConfigureAwait(false);

        switch (acquire.Outcome)
        {
            case IdempotencyAcquireOutcome.Replay:
                context.HttpContext.Items[IdempotencyHttpItems.IsReplayKey] = true;
                context.Result = new ContentResult
                {
                    StatusCode = acquire.Cached!.StatusCode,
                    ContentType = "application/json; charset=utf-8",
                    Content = acquire.Cached.BodyJson
                };
                return;

            case IdempotencyAcquireOutcome.InProgress:
                context.Result = ErrorResult(
                    409,
                    "IDEMPOTENCY_IN_PROGRESS",
                    "Yêu cầu trước đang xử lý. Vui lòng thử lại sau vài giây với cùng Idempotency-Key.");
                return;

            case IdempotencyAcquireOutcome.BodyMismatch:
                context.Result = ErrorResult(
                    422,
                    "IDEMPOTENCY_KEY_REUSED",
                    "Idempotency-Key đã dùng với payload khác.");
                return;
        }

        context.HttpContext.Items[IdempotencyHttpItems.ScopeKey] = scopeKey;

        var executed = await next().ConfigureAwait(false);

        if (executed.Result is ObjectResult objectResult)
        {
            var statusCode = objectResult.StatusCode ?? 200;
            if (statusCode is >= 200 and < 300)
            {
                var json = JsonSerializer.Serialize(objectResult.Value, JsonOptions);
                await store.CompleteAsync(scopeKey, statusCode, json, context.HttpContext.RequestAborted)
                    .ConfigureAwait(false);
            }
            else
            {
                await store.ReleaseAsync(scopeKey, context.HttpContext.RequestAborted).ConfigureAwait(false);
            }
        }
        else
        {
            await store.ReleaseAsync(scopeKey, context.HttpContext.RequestAborted).ConfigureAwait(false);
        }
    }

    private static IActionResult ErrorResult(int status, string code, string message) =>
        new ObjectResult(new ApiResponse
        {
            Code = code,
            Message = message,
            Status = status,
            Data = null
        })
        { StatusCode = status };

    private static async Task<string> ComputeRequestBodyHashAsync(HttpRequest request)
    {
        if (request.ContentLength is 0 or null
            && !HttpMethods.IsPost(request.Method)
            && !HttpMethods.IsPut(request.Method)
            && !HttpMethods.IsPatch(request.Method))
        {
            return Convert.ToHexString(SHA256.HashData(Array.Empty<byte>()));
        }

        request.EnableBuffering();
        request.Body.Position = 0;

        using var ms = new MemoryStream();
        await request.Body.CopyToAsync(ms).ConfigureAwait(false);
        request.Body.Position = 0;

        return Convert.ToHexString(SHA256.HashData(ms.ToArray()));
    }

    private static string BuildScopeKey(ActionExecutingContext context, string clientKey)
    {
        var http = context.HttpContext;
        var routePattern = (http.GetEndpoint()?.Metadata.GetMetadata<RouteEndpoint>()?.RoutePattern.RawText)
            ?? http.Request.Path.Value
            ?? "unknown";

        var actor = http.User.Identity?.IsAuthenticated == true
            && Guid.TryParse(http.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? $"user:{userId:N}"
            : $"anon:{HashActor(http.Connection.RemoteIpAddress?.ToString() ?? "unknown")}";

        return $"{actor}:{http.Request.Method}:{routePattern}:{clientKey}".ToLowerInvariant();
    }

    private static string HashActor(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash)[..16];
    }
}
