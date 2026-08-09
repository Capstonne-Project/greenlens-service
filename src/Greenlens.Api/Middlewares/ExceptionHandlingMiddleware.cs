using System.Text.Json;
using FluentValidation;
using Greenlens.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Api.Middlewares;

/// <summary>
/// Global exception handler + catch-all for non-success status codes
/// that bypass controllers (404 unknown route, 405 method not allowed, etc.).
/// Guarantees every HTTP response uses the ApiResponse envelope.
/// </summary>
public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);

            // ── Catch-all for bare status codes that never hit a controller ──
            // 401/403 are already handled by JwtBearerEvents in DI config.
            // Here we handle: 404 (unknown route), 405 (wrong HTTP method), etc.
            if (!context.Response.HasStarted && context.Response.StatusCode >= 400
                && context.Response.ContentLength is null or 0
                && !context.Response.Headers.ContainsKey("Content-Type"))
            {
                await WriteStatusResponse(context, context.Response.StatusCode);
            }
        }
        catch (ValidationException ex)
        {
            await HandleValidationExceptionAsync(context, ex);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Model hiện KHÔNG dùng concurrency token (không RowVersion/xmin), nên exception này
            // hầu như luôn nghĩa là "UPDATE/DELETE khớp 0 dòng" do entity bị detach giữa chừng
            // (ChangeTracker.Clear() của side-effect) — không phải xung đột ghi đồng thời thật.
            // Log Error kèm entity để truy vết, đừng hạ xuống Warning và đổ lỗi cho user.
            var entries = string.Join(
                ", ",
                ex.Entries.Select(e => $"{e.Entity.GetType().Name}({e.State})"));
            logger.LogError(
                ex,
                "Concurrency/stale-entity conflict on {Method} {Path}. Affected entries: {Entries}",
                context.Request.Method,
                context.Request.Path,
                string.IsNullOrEmpty(entries) ? "(none)" : entries);
            await HandleConcurrencyExceptionAsync(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleGenericExceptionAsync(context);
        }
    }

    /// <summary>FluentValidation errors (thrown by ValidationBehavior pipeline).</summary>
    private static async Task HandleValidationExceptionAsync(HttpContext context, ValidationException ex)
    {
        var errors = ex.Errors.Select(e => new
        {
            field = e.PropertyName,
            code = e.ErrorCode,
            message = e.ErrorMessage
        });

        var response = new ApiResponse
        {
            Code = "VALIDATION_ERROR",
            Message = "Dữ liệu không hợp lệ.",
            Status = 422,
            Data = new { errors }
        };

        context.Response.StatusCode = 422;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }

    /// <summary>
    /// Ghi đè không khớp dòng nào (stale/detached entity, hoặc thao tác đã được xử lý trước đó).
    /// Thao tác có thể ĐÃ được lưu thành công — message hướng dẫn user tải lại để kiểm tra,
    /// thay vì khẳng định thất bại và mời thử lại (dễ gây ghi trùng ở endpoint không idempotent).
    /// </summary>
    private static async Task HandleConcurrencyExceptionAsync(HttpContext context)
    {
        var response = new ApiResponse
        {
            Code = "CONCURRENCY_CONFLICT",
            Message = "Thao tác có thể đã được ghi nhận. Vui lòng tải lại trang để kiểm tra trạng thái trước khi thử lại.",
            Status = 409,
            Data = null
        };

        context.Response.StatusCode = 409;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }

    /// <summary>Unhandled exception → 500.</summary>
    private static async Task HandleGenericExceptionAsync(HttpContext context)
    {
        var response = new ApiResponse
        {
            Code = "INTERNAL_ERROR",
            Message = "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.",
            Status = 500,
            Data = null
        };

        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }

    /// <summary>Bare status code responses (no controller matched).</summary>
    private static async Task WriteStatusResponse(HttpContext context, int statusCode)
    {
        var (code, message) = statusCode switch
        {
            404 => ("NOT_FOUND", "Không tìm thấy tài nguyên hoặc đường dẫn không tồn tại."),
            405 => ("METHOD_NOT_ALLOWED", "Phương thức HTTP không được hỗ trợ cho đường dẫn này."),
            406 => ("NOT_ACCEPTABLE", "Định dạng yêu cầu không được hỗ trợ."),
            415 => ("UNSUPPORTED_MEDIA_TYPE", "Content-Type không được hỗ trợ."),
            _   => ("ERROR", $"Lỗi HTTP {statusCode}.")
        };

        var response = new ApiResponse
        {
            Code = code,
            Message = message,
            Status = statusCode,
            Data = null
        };

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
