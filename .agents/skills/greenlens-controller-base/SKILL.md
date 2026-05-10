---
name: ecoreport-controller-base
description: Scaffold an ASP.NET Core API controller (deriving from ControllerBase) for the EcoReport .NET 9 backend (project SU26SE049). Use this skill whenever the user asks to add an HTTP endpoint, controller, route, action, or REST API surface — including casual phrasings like "add an endpoint to submit a report", "create the ReportsController", "I need a route for verifying", "expose this command over HTTP", "add a GET for nearby reports". Trigger this even if the user only says "add the route" or "wire up the API" because the controller, the action signature, the [Authorize] policy, the route convention, and the Result→IActionResult mapping must all be done together. Produces a controller class under EcoReport.Api/Controllers/ with versioned routes, attribute-based authorization, MediatR dispatch, ProducesResponseType for OpenAPI, and the shared ToActionResult() extension that maps Result<T> to the right HTTP status.
---

# EcoReport Controller (ControllerBase)

This skill scaffolds **controller-based** API endpoints for the EcoReport backend. The project's default per the workspace `CLAUDE.md` was Minimal API — but the user has chosen Controllers, so this skill enforces the Controller convention end-to-end.

> **Note for Claude:** if you also see a `ecoreport-minimal-api` skill in this workspace, the user has migrated. Ask which is current before scaffolding. Don't mix styles in the same project.

## When to use

Trigger when the user mentions any of:
- "controller", "ControllerBase", "Web API"
- "endpoint", "route", "action", "API surface"
- "add a GET/POST/PUT/DELETE for ..."
- "expose ... over HTTP"
- "[ApiController]", "[Route]"

## Workflow

1. **Check for existing controller.** Before creating a new file, ask: "Does `EcoReport.Api/Controllers/<Module>Controller.cs` already exist?" If yes, **add the action to the existing controller** — one controller per resource (`ReportsController`, `AuthController`, `MapController`, `OfficerController`…), not one per use case.
2. **Confirm with the user:**
   - Resource name (becomes controller + route, e.g. `Reports` → `/api/v1/reports`)
   - The MediatR Command/Query the action dispatches (must already exist — if not, hand off to `ecoreport-feature-slice` first)
   - HTTP verb + sub-route (e.g. `POST /` or `POST /{id}/verify` or `GET /nearby`)
   - Authorization policy (anonymous, Citizen, Officer, CleanupTeam, Admin, or a named policy)
   - Success status (`200 OK` for queries, `201 Created` for creates, `204 NoContent` for void mutations)
3. **Pick the template:**
   - `assets/controller.cs.template` for a brand-new controller
   - `assets/action-snippet.cs.template` for adding an action to an existing controller
4. **Substitute placeholders** (same `__DOUBLE_UNDERSCORE__` convention as other skills).
5. **Make sure `ToActionResult()` exists.** It's the extension that maps `Result<T>` → `IActionResult`. Check `src/EcoReport.Api/Common/ResultExtensions.cs`. If missing, materialize it from `assets/result-extensions.cs.template` — once per project.

## Controller conventions

These match `CLAUDE.md` and ASP.NET Core 9 defaults:

- Class name: `<Resource>Controller` (plural noun: `ReportsController`, not `ReportController`).
- Inherits **`ControllerBase`** — not `Controller` (we don't render views).
- Class attributes: `[ApiController]`, `[Route("api/v{version:apiVersion}/[controller]")]`, `[ApiVersion("1.0")]`.
- DI via primary constructor: `public sealed class ReportsController(ISender mediator) : ControllerBase`.
- Each action:
  - Returns `Task<IActionResult>` (not `ActionResult<T>` — keeps the `Result<T>` mapping uniform).
  - Decorates with `[HttpPost]` / `[HttpGet("{id:guid}")]` etc.
  - Has `[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(...))]` for the success shape **and** for `Status400BadRequest`, `Status404NotFound`, `Status401Unauthorized`, `Status403Forbidden`, `Status422UnprocessableEntity` as appropriate. OpenAPI / Swagger reads these.
  - Decorates authorization: `[Authorize(Roles = "Officer,Admin")]` or `[Authorize(Policy = Policies.CanVerifyReport)]`. **Never use bare `[Authorize]`** — be explicit so reviewers can audit who can hit each endpoint against the BR role matrix (BR doc §2).
  - **Never** receives `IFormFile` for media uploads — those go through pre-signed S3 URLs (CLAUDE.md §4.10). The action only accepts the metadata + media IDs.
  - Body parameter is the Command/Query record from `Application.Features.<Module>.<UseCase>`. Bind it from `[FromBody]` for POST/PUT, `[FromQuery]` for GET/DELETE. Don't create a separate "Request" DTO unless the wire shape really differs from the Command shape.
- Routes are kebab-case at the URL level (`/api/v1/cleanup-teams`), but C# names stay PascalCase. Use `[Route("api/v{version:apiVersion}/cleanup-teams")]` to override the conventional `[controller]` token when needed.

## Result → HTTP mapping

The `ToActionResult()` extension maps:

| `Error.Type`   | HTTP status |
|----------------|-------------|
| `Validation`   | 400 Bad Request (Problem Details) |
| `NotFound`     | 404 Not Found |
| `Conflict`     | 409 Conflict |
| `Forbidden`    | 403 Forbidden |
| `BusinessRule` | 422 Unprocessable Entity |
| `Unexpected`   | 500 Internal Server Error |
| (success)      | 200 OK with the value, OR whatever the caller passes via `onSuccess` |

For `201 Created`, use the overload that takes a `Func<T, IActionResult>`:

```csharp
var result = await mediator.Send(cmd, ct);
return result.ToActionResult(value => CreatedAtAction(nameof(GetById), new { id = value }, value));
```

## Self-check

- [ ] Controller is `sealed`
- [ ] Class has `[ApiController]` AND `[Route(...)]` AND `[ApiVersion("1.0")]`
- [ ] Each action has explicit authorization (role list or policy) — no bare `[Authorize]`
- [ ] Each action has `[ProducesResponseType]` for at least success + 400 + 401/403 + (404 if it loads by id)
- [ ] No business logic in the action body — only `mediator.Send(...)` + `ToActionResult()`
- [ ] No `try/catch` in the action — exceptions flow to `ExceptionHandlingMiddleware` (CLAUDE.md §9)
- [ ] No `IFormFile` for report/comment media — pre-signed URL flow
- [ ] `CancellationToken` parameter is on every action

## Templates

- `assets/controller.cs.template` — full controller class with 3 example actions (POST create, GET by id, GET list)
- `assets/action-snippet.cs.template` — single action to drop into an existing controller
- `assets/result-extensions.cs.template` — the `ToActionResult()` extension; create once per project

## Example

**User:** "Add a POST /verify action on ReportsController."

**Your response:**
1. Confirm: it's `POST /api/v1/reports/{id}/verify`, dispatches `VerifyReportCommand`, requires Officer or Admin role, returns `204 NoContent` on success.
2. Confirm `VerifyReportCommand` exists in `Application/Features/Reports/VerifyReport/`. If not, stop and hand off.
3. Use `action-snippet.cs.template`, substitute placeholders, splice into the existing `ReportsController.cs`.
4. Show the diff, not the whole file.
