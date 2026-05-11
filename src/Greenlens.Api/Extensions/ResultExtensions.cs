using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Greenlens.Api.Extensions;

public static class ResultExtensions
{
    /// <summary>Returns 200 OK with standard ApiResponse envelope.</summary>
    public static IActionResult ToHttp<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(new ApiResponse<T>
            {
                Code = "SUCCESS",
                Message = "OK",
                Status = 200,
                Data = result.Value
            });
        }

        return ToErrorResult(result.Error!);
    }

    /// <summary>Returns 201 Created with standard envelope for successful POST create.</summary>
    public static IActionResult ToHttpCreated<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return new ObjectResult(new ApiResponse<T>
            {
                Code = "SUCCESS",
                Message = "Created",
                Status = 201,
                Data = result.Value
            })
            { StatusCode = 201 };
        }

        return ToErrorResult(result.Error!);
    }

    /// <summary>Returns 204 No Content for successful void mutations.</summary>
    public static IActionResult ToHttpNoContent(this Result result)
    {
        if (result.IsSuccess)
        {
            return new NoContentResult();
        }

        return ToErrorResult(result.Error!);
    }

    /// <summary>
    /// Central error → HTTP mapping. Every ErrorType maps to exactly one HTTP status code.
    /// Keep in sync with ErrorType enum — add a switch arm for every new value.
    /// </summary>
    private static IActionResult ToErrorResult(Error error)
    {
        var (statusCode, code) = error.Type switch
        {
            ErrorType.Validation   => (400, error.Code),
            ErrorType.NotFound     => (404, error.Code),
            ErrorType.Conflict     => (409, error.Code),
            ErrorType.Forbidden    => (403, error.Code),
            ErrorType.BusinessRule => (422, error.Code),
            ErrorType.Unexpected   => (500, error.Code),
            _ => (500, "INTERNAL_ERROR"),
        };

        return new ObjectResult(new ApiResponse
        {
            Code = code,
            Message = error.Message,
            Status = statusCode,
            Data = null
        })
        { StatusCode = statusCode };
    }
}
