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

| `ErrorType`     | HTTP | Title                       | When |
|-----------------|------|-----------------------------|------|
| `Validation`    | 400  | "Validation failed"         | input shape (lengths, regex, ranges) |
| `NotFound`      | 404  | "Resource not found"        | entity by id missing |
| `Conflict`      | 409  | "Conflict"                  | unique key clash, optimistic concurrency |
| `Forbidden`     | 403  | "Forbidden"                 | authorization failure (auth ✓, perm ✗) |
| `BusinessRule`  | 422  | "Business rule violated"    | state machine, invariant violation |
| `Unexpected`    | 500  | "Unexpected error"          | catch-all |

When extending, follow these patterns:
- `RateLimited` → 429, include `Retry-After` header.
- `Locked` → 423 (account locked, BR-AUTH-011 lockout).
- `PreconditionFailed` → 412 (If-Match mismatch on PUT).
- `Unauthenticated` → 401 (separate from `Forbidden` which is 403). Most projects fold this into `Forbidden` because authn is checked at middleware — but if it slips through, having a separate type helps.

## Conventions

- `Error.Code` is `snake_case_with_dots`: `reports.not_found`, `auth.account_locked`. Used as the stable contract for FE.
- `Error.Message` is human-readable Vietnamese (default culture vi-VN per BR-SYS-006). English fallback via `IStringLocalizer` if user has `Accept-Language: en`.
- `ProblemDetails.Type` is a URL pointing to docs page describing the code: `https://greenlens.example/errors/{code}`. The docs page may not exist yet, but the convention reserves the slot.
- `extensions.code` carries the `Error.Code` for FE switch logic. **Don't put the code only in `Title`** — title is for display.
- For validation, `extensions.errors` is a `Dictionary<string, string[]>` (ASP.NET Core's standard) — field path → list of messages.

## Self-check

- [ ] `ErrorType` enum and `ResultExtensions` switch are in lockstep — every type has a switch arm
- [ ] HTTP status follows convention table above
- [ ] `extensions.code` is set on every error response
- [ ] `Error.Code` follows `snake_case_with_dots` format
- [ ] No `try/catch` added to controllers (still funneling through Result)
- [ ] If breaking change: user is informed, FE version bumped if needed
- [ ] If localization touched: tests cover both vi-VN and en-US

## Templates

- `assets/result-extensions-extended.cs.template` — full updated extension with all 7-9 status arms
- `assets/error-type-additions.cs.template` — adding new types
- `assets/validation-with-fields.cs.template` — per-field validation projection

## Common pitfalls

| Pitfall | Why bad | Fix |
|---|---|---|
| Adding `ErrorType.Validation2` for a "different kind" of validation | Multiplies types; FE needs to switch on both | Use the existing type + different code (`reports.geo.out_of_bounds` vs `reports.media.too_many`) |
| Returning 500 for any unmatched ErrorType | Hides bugs | The `_` switch arm should log a warning — every new type should have an explicit arm |
| Putting localized message in `Error.Code` | Code is the contract | Code stays English snake_case; message goes through IStringLocalizer |
| Wrapping known exceptions to map to status codes | Re-introduces try/catch in the path | Map at the source: handler returns Result.Failure with the right type |
| Forgetting `Retry-After` on 429 | Clients can't back off correctly | New `ToActionResult` overload taking `TimeSpan retryAfter` for rate-limit cases |
