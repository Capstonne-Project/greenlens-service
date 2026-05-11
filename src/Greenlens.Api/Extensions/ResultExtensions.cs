using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Greenlens.Api.Extensions;

public static class ResultExtensions
{
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

        var (statusCode, code) = result.Error!.Type switch
        {
            ErrorType.Validation => (422, result.Error.Code),
            ErrorType.NotFound => (404, result.Error.Code),
            ErrorType.Conflict => (409, result.Error.Code),
            ErrorType.Forbidden => (403, result.Error.Code),
            ErrorType.BusinessRule => (422, result.Error.Code),
            _ => (500, "INTERNAL_ERROR"),
        };

        return new ObjectResult(new ApiResponse
        {
            Code = code,
            Message = result.Error.Message,
            Status = statusCode,
            Data = null
        })
        { StatusCode = statusCode };
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

        return result.ToHttp();
    }
}
