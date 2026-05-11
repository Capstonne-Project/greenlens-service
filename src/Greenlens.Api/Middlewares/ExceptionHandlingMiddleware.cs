using System.Text.Json;
using FluentValidation;
using Greenlens.Application.Common.Models;

namespace Greenlens.Api.Middlewares;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            await HandleValidationExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleGenericExceptionAsync(context, ex);
        }
    }

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
        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }

    private static async Task HandleGenericExceptionAsync(HttpContext context, Exception ex)
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
        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
