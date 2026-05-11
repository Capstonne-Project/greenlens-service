# Result Pattern — GreenLens

> **Source:** OVERVIEW.md §4.3 — Result Pattern (KHÔNG dùng exception cho luồng business)

## Core Types

```csharp
// Domain/Common/Result.cs
namespace Greenlens.Domain.Common;

public sealed class Result<T>
{
    private Result(T? value, Error? error, bool isSuccess)
    {
        Value = value;
        Error = error;
        IsSuccess = isSuccess;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public Error? Error { get; }

    public static Result<T> Success(T value) => new(value, null, true);
    public static Result<T> Failure(Error error) => new(default, error, false);

    // Implicit conversions for ergonomics
    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
}

// Non-generic for void operations
public sealed class Result
{
    private Result(Error? error, bool isSuccess)
    {
        Error = error;
        IsSuccess = isSuccess;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error? Error { get; }

    public static Result Success() => new(null, true);
    public static Result Failure(Error error) => new(error, false);
    public static implicit operator Result(Error error) => Failure(error);
}
```

## Error Type

```csharp
// Domain/Common/Error.cs
namespace Greenlens.Domain.Common;

public sealed record Error(string Code, string Message, ErrorType Type)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Unexpected);
}

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Forbidden,
    BusinessRule,
    Unexpected
}
```

## Centralized Error Definitions

```csharp
// Application/Common/Errors.cs — organized by module
public static class Errors
{
    public static class Auth
    {
        public static Error InvalidCredentials => new(
            "INVALID_CREDENTIALS",
            "Email hoặc mật khẩu không đúng",
            ErrorType.Validation);

        public static Error AccountLocked => new(
            "ACCOUNT_LOCKED",
            "Tài khoản đã bị khóa do đăng nhập sai quá nhiều lần (BR-AUTH-011)",
            ErrorType.BusinessRule);

        public static Error EmailTaken => new(
            "EMAIL_TAKEN",
            "Email đã được sử dụng",
            ErrorType.Conflict);

        public static Error TokenExpired => new(
            "TOKEN_EXPIRED",
            "Token đã hết hạn",
            ErrorType.Validation);
    }

    public static class Report
    {
        public static Error NotFound(Guid id) => new(
            "NOT_FOUND",
            $"Không tìm thấy báo cáo {id}",
            ErrorType.NotFound);

        public static Error InvalidStateTransition(string from, string to) => new(
            "INVALID_STATE_TRANSITION",
            $"Không thể chuyển trạng thái từ {from} sang {to} (BR-REP-021)",
            ErrorType.BusinessRule);

        public static Error InvalidGps => new(
            "INVALID_GPS",
            "Tọa độ GPS ngoài phạm vi Việt Nam (BR-REP-003)",
            ErrorType.Validation);

        public static Error RateLimitExceeded => new(
            "RATE_LIMIT_EXCEEDED",
            "Vượt giới hạn gửi báo cáo (BR-REP-010)",
            ErrorType.BusinessRule);

        public static Error DuplicateReport => new(
            "DUPLICATE_REPORT",
            "Báo cáo trùng lặp (BR-REP-030)",
            ErrorType.Conflict);
    }
}
```

## Usage in Handlers

```csharp
/// <remarks>Implements: BR-REP-001, BR-REP-003, BR-REP-010, BR-REP-013</remarks>
public sealed class SubmitReportCommandHandler(
    IReportRepository reports,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    ICacheService cache)
    : IRequestHandler<SubmitReportCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        SubmitReportCommand request,
        CancellationToken ct)
    {
        // Business validation — returns Result, NOT exception
        var rateCheck = await CheckRateLimitAsync(currentUser.UserId, ct)
            .ConfigureAwait(false);
        if (rateCheck.IsFailure)
            return rateCheck.Error!;  // implicit conversion

        var report = Report.Create(
            currentUser.UserId,
            new GeoLocation(request.Lat, request.Lng),
            request.Category,
            request.Description);

        reports.Add(report);
        await uow.SaveChangesAsync(ct);

        return report.Id;  // implicit conversion to Result<Guid>.Success
    }
}
```

## Mapping to HTTP — Api Layer

```csharp
// Api/Extensions/ResultExtensions.cs
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
            ErrorType.Validation   => (422, result.Error.Code),
            ErrorType.NotFound     => (404, result.Error.Code),
            ErrorType.Conflict     => (409, result.Error.Code),
            ErrorType.Forbidden    => (403, result.Error.Code),
            ErrorType.BusinessRule => (422, result.Error.Code),
            _                      => (500, "INTERNAL_ERROR"),
        };

        return new ObjectResult(new ApiResponse<object>
        {
            Code = code,
            Message = result.Error.Message,
            Status = statusCode,
            Data = null
        })
        { StatusCode = statusCode };
    }
}
```

## DO / DON'T

```csharp
// ✅ DO — Return Result for business failures
return Errors.Report.InvalidGps;

// ✅ DO — Check result before proceeding
var userResult = await GetUserAsync(id, ct);
if (userResult.IsFailure)
    return userResult.Error!;

// ❌ DON'T — Throw exceptions for business rules
throw new InvalidGpsException("Out of Vietnam");

// ❌ DON'T — Use exceptions for control flow
try { ... }
catch (ReportNotFoundException) { return NotFound(); }

// ✅ Exception ONLY for infrastructure failures
try
{
    await r2Client.PutObjectAsync(request, ct);
}
catch (AmazonS3Exception ex)
{
    logger.LogError(ex, "R2 upload failed");
    throw; // Infrastructure failure — let middleware handle
}
```
