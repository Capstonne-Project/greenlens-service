---
name: greenlens-build
description: >
  Guides code changes and produces implementation notes for the GreenLens backend.
  Use this skill when writing or modifying C# code in the .NET 9 / ASP.NET Core / EF Core 9
  Clean Architecture solution. Covers Domain entities, Application CQRS handlers, Infrastructure
  adapters, and API controllers. Enforces project conventions from CLAUDE.md and 00_API_CONVENTIONS.md.
  Triggers: user asks to "implement", "code", "build", "create endpoint", "add feature", or "fix".
---

# GreenLens — Build Step

> **Goal:** Implement code changes following Clean Architecture, CQRS, and all project conventions. Produce implementation notes documenting what was changed and why.

## When to use this skill

- After the Plan step has been approved.
- When implementing a new feature, endpoint, or bugfix.
- When modifying existing handlers, entities, or infrastructure.

## How to use it

### 1. Pre-flight Checks

Before writing any code:

- [ ] Plan/scope document exists and is approved
- [ ] Business Rules (BR IDs) are identified
- [ ] Target vertical slice folder is determined

### 2. Implementation Order (Layer by Layer)

Always implement in this order to respect the dependency rule:

#### Step A: Domain Layer (`Greenlens.Domain/`)

**Rules:**
- NO references to other projects. NO `Microsoft.*`, NO `EntityFrameworkCore`.
- Entities use `class` (have behavior + identity), inherit `AuditableEntity`.
- DTOs/Commands/Queries/Events use `record` (immutable).
- All classes `sealed` by default unless abstract.
- State machine transitions via domain methods, NOT public setters:
  ```csharp
  public void Verify(Guid officerId)
  {
      if (Status != ReportStatus.Submitted)
          throw new DomainException("Invalid state transition");
      Status = ReportStatus.Verified;
      VerifiedBy = officerId;
      AddDomainEvent(new ReportVerifiedEvent(Id, officerId));
  }
  ```
- Value objects use `record struct` for small types (GeoLocation, Money).
- Soft delete: `DeletedAt` nullable + global query filter for User, Report, Comment.

#### Step B: Application Layer (`Greenlens.Application/`)

**Vertical Slice structure** — each use case = 1 folder with up to 4 files:
```
Features/<Module>/<UseCaseName>/
├── <Name>Command.cs           // record ... : IRequest<Result<T>>
├── <Name>CommandHandler.cs    // sealed class, XML doc with BR IDs
├── <Name>CommandValidator.cs  // FluentValidation
└── <Name>Response.cs          // if custom shape needed
```

**CQRS conventions:**
- Commands (mutate): `record XxxCommand(...) : IRequest<Result<T>>`
- Queries (read): `record XxxQuery(...) : IRequest<Result<TDto>>`
- Queries use `AsNoTracking()` or Mapster `.ProjectToType<>()` projection.
- Commands ALWAYS go through `TransactionBehavior`.

**Result Pattern** — NO exceptions for business logic:
```csharp
// ✅ Correct
return Result.Failure(Errors.Auth.AccountLocked);

// ❌ Wrong — don't throw for business rules
throw new AccountLockedException();
```

**Validation 2 layers:**
1. Input validation (FluentValidation) — format, length, range. Runs in pipeline behavior.
2. Business validation in handler — needs DB access (e.g., role check, duplicate check).

**Handler XML comments** — MANDATORY for BR traceability:
```csharp
/// <summary>
/// Submit a new pollution report.
/// </summary>
/// <remarks>
/// Implements: BR-REP-001 (photo required), BR-REP-003 (Vietnam GPS bounds),
/// BR-REP-005 (category required), BR-REP-010 (rate limit), BR-REP-013 (initial state).
/// </remarks>
public sealed class SubmitReportCommandHandler : IRequestHandler<SubmitReportCommand, Result<Guid>>
```

**Interfaces for infrastructure** — define in `Application/Common/Interfaces/`:
```csharp
public interface IFileStorage { ... }
public interface ICurrentUser { ... }  // wraps IHttpContextAccessor at infra layer
```
NEVER import `IHttpContextAccessor` in Application.

#### Step C: Infrastructure Layer (`Greenlens.Infrastructure/`)

**Database conventions:**
- Snake_case in DB, PascalCase in C# — via `EFCore.NamingConventions`.
- Entity configs in `Persistence/Configurations/` using `IEntityTypeConfiguration<>`.
- Geo: `NetTopologySuite.Geometries.Point` (SRID 4326). GIST index in migration.
- Required indexes: `Report(Status, CreatedAt)`, `Report.Location`(GIST), `User.Email`(unique), `User.PhoneNumber`(unique).
- One `SaveChanges()` per request — via `IUnitOfWork` or `TransactionBehavior`.

**Migration naming:** `yyyyMMddHHmm_VerbNoun` (e.g., `202605091200_AddReportSlaColumns`)

**File upload:** Presigned URL pattern (client → S3 direct), backend validates + stores metadata.

**Background jobs:** Register in Hangfire with correct schedule per CLAUDE.md §4.11.

#### Step D: API Layer (`Greenlens.Api/`)

**Controller pattern:**
```csharp
[ApiController]
[Route("v1/[controller]")]
public sealed class ReportsController : ControllerBase
{
    private readonly ISender _sender;

    public ReportsController(ISender sender) => _sender = sender;

    /// <summary>Submit a new pollution report</summary>
    [HttpPost]
    [Authorize(Policy = Policies.CanSubmitReport)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 201)]
    public async Task<IActionResult> SubmitAsync(
        [FromBody] SubmitReportCommand cmd,
        CancellationToken ct)
        => (await _sender.Send(cmd, ct)).ToHttp();
}
```

**Response envelope** — ALL responses use:
```json
{ "code": "SUCCESS", "message": "...", "status": 200, "data": { ... } }
```

**Auth:** JWT Bearer. Policies, not role strings:
```csharp
options.AddPolicy(Policies.CanVerifyReport, p => p.RequireRole("Officer", "Admin"));
```

**Error mapping** — `ExceptionHandlingMiddleware`:
- `ValidationException` → 400
- `NotFoundException` → 404
- `ForbiddenException` → 403
- `BusinessRuleViolationException` → 422
- Others → 500 + correlation ID
- Format: RFC 7807 Problem Details

### 3. Coding Standards Checklist

- [ ] **Nullable reference types** enabled project-wide
- [ ] **File-scoped namespaces**, **primary constructors**, **collection expressions**
- [ ] **`async`/`await` throughout** — NO `.Result` or `.Wait()`
- [ ] **Every I/O method** accepts `CancellationToken`
- [ ] **`ConfigureAwait(false)`** in library projects (Application, Infrastructure)
- [ ] **`IAsyncEnumerable<T>`** for streaming queries (CSV export)
- [ ] Naming: `XxxDto` (output), `XxxRequest` (HTTP input), `XxxCommand`/`XxxQuery` (Application)
- [ ] Async methods end with `Async` suffix
- [ ] Enum: singular, PascalCase, `[JsonStringEnumConverter]` on JSON output
- [ ] Comments in English. Vietnamese allowed in XML doc for BR terms.
- [ ] Only "why" comments, never "what" comments.

### 4. Field Naming Conventions

| Type | Convention | Example |
|------|-----------|---------|
| JSON keys | `camelCase` | `firstName`, `createdAt` |
| URL paths | `kebab-case` | `/pollution-reports` |
| Query params | `camelCase` | `?pageSize=20&sortBy=createdAt` |
| DB columns | `snake_case` | `first_name`, `created_at` |
| Enum values | `UPPER_SNAKE_CASE` | `IN_PROGRESS` |
| IDs | UUID v4 | `550e8400-e29b-...` |
| Timestamps | ISO 8601 UTC | `2026-05-09T10:15:30Z` |
| Coordinates | decimal degrees | `lat: 10.7626, lng: 106.6602` |

### 5. Pagination Convention

All list endpoints MUST use pagination:
```csharp
// Query params: ?page=1&pageSize=20&sortBy=createdAt&sortOrder=desc
// pageSize max = 100, default = 20, page is 1-indexed
```

Response:
```json
{
  "code": "SUCCESS",
  "data": {
    "items": [...],
    "pagination": {
      "page": 1, "pageSize": 20, "totalItems": 245,
      "totalPages": 13, "hasNext": true, "hasPrev": false
    }
  }
}
```

### 6. Produce Implementation Notes

After coding, create notes documenting:

```markdown
## Implementation Notes — [Feature Name]

### Files Changed
| File | Change Type | Description |
|------|------------|-------------|
| `Domain/Entities/Report.cs` | Modified | Added `Verify()` transition method |

### BR Coverage
| BR ID | Status | Location |
|-------|--------|----------|
| BR-REP-001 | ✅ Implemented | SubmitReportCommandHandler |

### Design Decisions
- **Why X:** explanation of non-obvious choices
- **Trade-off:** what was considered and why this approach was chosen

### Migration
- Name: `202605091200_AddReportSlaColumns`
- Reversible: Yes/No

### New Dependencies
- None (or list with justification — user must approve)
```

### 7. Post-Build Verification

- [ ] `dotnet build` passes with zero warnings
- [ ] No `using Microsoft.EntityFrameworkCore` leaked into Domain/Application
- [ ] All handlers have BR ID XML comments
- [ ] Response envelope matches `{code, message, status, data}`
- [ ] CancellationToken passed to all I/O calls
- [ ] No new packages added without user approval
