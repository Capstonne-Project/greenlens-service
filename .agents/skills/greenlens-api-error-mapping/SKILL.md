---
name: greenlens-api-error-mapping
description: Extend the Result→IActionResult mapping (ToActionResult extension), add new ErrorType values, customize ProblemDetails output, or fix HTTP status mapping in the Greenlens .NET 9 API (project SU26SE049). Use this skill whenever the user mentions "ToActionResult", "error mapping", "ProblemDetails", "ErrorType", "RFC 7807", or asks to change how a Result becomes an HTTP response — including casual phrasings like "make X return 409 instead of 422", "add a Locked error type", "include validation field paths in the response", "localize error messages", or "the controller returns 500 when it should be 403". Trigger this BEFORE the user adds custom try/catch in controllers (which is what this skill exists to prevent). Updates ResultExtensions.cs in Greenlens.Api/Common/ and the ErrorType enum in Domain/Common/.
---

# Greenlens API Error Mapping

OVERVIEW.md §9 + the controller skill (`greenlens-controller-base`) define the project's error-handling contract: handlers return `Result<T>`, controllers call `ToActionResult()`, and that extension maps to RFC 7807 ProblemDetails. This skill keeps that mapping consistent when it needs to change.

## When to use

Trigger when the user mentions:
- "ToActionResult", "error mapping", "ProblemDetails"
- "ErrorType", "Error type", "Result.Failure code"
- "RFC 7807", "problem+json"
- A specific HTTP status mismatch: "this returns 500 but should be 422", "I want 409 here"
- "Localize errors", "error messages in Vietnamese"
- "Validation errors don't include field paths"
- "Custom error response shape"

## Step 0 — Push back on per-controller error handling

If the user says "let me add try/catch in this controller to return a custom error", stop:

> "We don't catch in controllers — error mapping is uniform across the API so the FE/mobile clients can rely on a single shape. If the current mapping is wrong, let's update `ToActionResult` and `ErrorType`. That fixes it everywhere at once."

Same for ad-hoc `BadRequest("...")` calls in actions — those bypass ProblemDetails. Push back and route through Result.

## Workflow

1. **Identify what needs to change:**
   - **New ErrorType case** (e.g. `RateLimited`, `Locked`, `PaymentRequired`)?
   - **Different HTTP status** for an existing case?
   - **Richer body** (validation field paths, retry-after header, error chain)?
   - **Localization** of message text?
   - **New convention** (e.g. all errors include a `traceId`)?

2. **Decide what to update:**
   | Change | Files to touch |
   |---|---|
   | Add ErrorType value | `Domain/Common/Error.cs` (enum) + `Api/Common/ResultExtensions.cs` (switch arm) |
   | Change status for existing type | `Api/Common/ResultExtensions.cs` (the `Problem(...)` switch) |
   | Add field-level validation details | Update Validation arm in `ResultExtensions.cs` to project errors as `errors: { field: [msg] }` |
   | Add retry-after header | New overload `ToActionResult` with `retryAfter` parameter; update RateLimit handler to pass it |
   | Localize messages | Move static `Error` instances to resource files; make `Errors.X.Y` a method that takes `IStringLocalizer` |
   | Custom shape | Replace `ProblemDetails` with a project-specific `GreenlensErrorResponse` record |

3. **Pick the template:**
   - `assets/result-extensions-extended.cs.template` — full updated `ResultExtensions.cs` with new arms
   - `assets/error-type-additions.cs.template` — adding values to the enum + per-type defaults table
   - `assets/validation-with-fields.cs.template` — projecting per-field validation errors into the response

4. **Verify backward compatibility.** FE clients depend on the wire shape. If you change the `code` format or move `extensions.code` elsewhere, that's a breaking change — flag it for the user.

## The current mapping (for reference)

### Layer 1: `ResultExtensions.ToHttp()` — `Api/Extensions/ResultExtensions.cs`

Maps `Result<T>` from handlers to HTTP responses with `ApiResponse` envelope:

| `ErrorType`     | HTTP | Code in response             | When |
|-----------------|------|------------------------------|------|
| `Validation`    | 400  | handler-specific code        | input shape (lengths, regex, ranges) |
| `NotFound`      | 404  | handler-specific code        | entity by id missing |
| `Conflict`      | 409  | handler-specific code        | unique key clash, optimistic concurrency |
| `Forbidden`     | 403  | handler-specific code        | authorization failure (auth ✓, perm ✗) |
| `BusinessRule`  | 422  | handler-specific code        | state machine, invariant violation |
| `Unexpected`    | 500  | handler-specific code        | catch-all |

Three overloads:
- `ToHttp()` → 200 OK on success
- `ToHttpCreated()` → 201 Created on success
- `ToHttpNoContent()` → 204 No Content for void mutations

### Layer 2: JWT Events — `Infrastructure/DependencyInjection.cs`

Intercepts ASP.NET Core auth middleware responses before they leave bare:

| Scenario | HTTP | Code | Message |
|----------|------|------|---------|
| No token / invalid token | 401 | `UNAUTHORIZED` | Bạn chưa đăng nhập hoặc token không hợp lệ. |
| Valid token, wrong role | 403 | `FORBIDDEN` | Bạn không có quyền truy cập tài nguyên này. |

### Layer 3: Model Binding — `Api/Program.cs` (`InvalidModelStateResponseFactory`)

When `[ApiController]` rejects malformed JSON, missing required fields, or type mismatches:

| Scenario | HTTP | Code | Data shape |
|----------|------|------|------------|
| Model binding failure | 400 | `VALIDATION_ERROR` | `{ errors: [{ field, message }] }` |

### Layer 4: `ExceptionHandlingMiddleware` — `Api/Middlewares/`

Catches anything that slips through + unknown routes:

| Scenario | HTTP | Code |
|----------|------|------|
| FluentValidation failures (`ValidationBehavior`) | 422 | `VALIDATION_ERROR` |
| Unhandled exceptions | 500 | `INTERNAL_ERROR` |
| Unknown route (no controller matched) | 404 | `NOT_FOUND` |
| Wrong HTTP method | 405 | `METHOD_NOT_ALLOWED` |
| Unsupported media type | 415 | `UNSUPPORTED_MEDIA_TYPE` |

### Standard response envelope

```json
{
  "code": "SUCCESS | ERROR_CODE",
  "message": "human-readable string (vi-VN default)",
  "status": 200,
  "data": { ... } | null
}
```

When extending, follow these patterns:
- `RateLimited` → 429, include `Retry-After` header.
- `Locked` → 423 (account locked, BR-AUTH-011 lockout).
- `PreconditionFailed` → 412 (If-Match mismatch on PUT).

## Conventions

- `Error.Code` is `SCREAMING_SNAKE_CASE`: `EMAIL_TAKEN`, `USER_NOT_FOUND`, `ACCOUNT_LOCKED`. Used as the stable contract for FE.
- `Error.Message` is human-readable Vietnamese (default culture vi-VN per BR-SYS-006).
- For validation, `data.errors` is an array of `{ field, code?, message }` objects — consistent between model binding (Layer 3) and FluentValidation (Layer 4).
- **Never** use `BadRequest("text")`, `NotFound()`, `Forbid()`, or raw `StatusCode(xxx)` in controllers — always go through `Result<T>` + `ToHttp()`.

## Self-check

- [ ] `ErrorType` enum and `ResultExtensions` switch are in lockstep — every type has a switch arm
- [ ] HTTP status follows convention table above
- [ ] `Code` is set on every error response (`SCREAMING_SNAKE_CASE`)
- [ ] No `try/catch` added to controllers (still funneling through Result)
- [ ] JWT `OnChallenge` and `OnForbidden` events return `ApiResponse` JSON
- [ ] `InvalidModelStateResponseFactory` is configured in `Program.cs`
- [ ] `ExceptionHandlingMiddleware` catches bare 404/405 for unknown routes

## Templates

- `assets/result-extensions-extended.cs.template` — full updated extension with all status arms
- `assets/error-type-additions.cs.template` — adding new types
- `assets/validation-with-fields.cs.template` — per-field validation projection

## Common pitfalls

| Pitfall | Why bad | Fix |
|---|---|---|
| Adding `ErrorType.Validation2` for a "different kind" of validation | Multiplies types; FE needs to switch on both | Use the existing type + different code (`INVALID_IMAGE_TYPE` vs `FILE_TOO_LARGE`) |
| Returning 500 for any unmatched ErrorType | Hides bugs | The `_` switch arm should log a warning — every new type should have an explicit arm |
| Putting localized message in `Error.Code` | Code is the contract | Code stays SCREAMING_SNAKE_CASE; message is Vietnamese |
| Wrapping known exceptions to map to status codes | Re-introduces try/catch in the path | Map at the source: handler returns Result.Failure with the right type |
| Using bare `BadRequest()` / `NotFound()` in controllers | Bypasses ApiResponse envelope | Always use `Result<T>` + `ToHttp()` |

