---
name: greenlens-csharp-conventions
description: >
  C# 13 / .NET 9 coding standards and ASP.NET Core conventions for the GreenLens backend.
  Enforces nullable reference types, file-scoped namespaces, primary constructors,
  sealed-by-default, async patterns, Result pattern, and Clean Architecture rules.
  Apply to ALL code written in this workspace.
---

# GreenLens — C# & ASP.NET Core Conventions

> Apply these rules to every C# file in the GreenLens solution.

## C# 13 / .NET 9 Standards

- **Nullable reference types:** `enable` project-wide. No nullable warnings.
- **ImplicitUsings:** `enable`, but explicit when ambiguous.
- **File-scoped namespaces** everywhere.
- **Primary constructors** for DI injection.
- **Collection expressions** (`[1, 2, 3]`) where applicable.
- **`record`** for DTO / Command / Query / Event (immutable).
- **`class`** for Entity (behavior + identity).
- **`sealed`** by default for non-abstract classes.
- **`record struct`** for small value objects (GeoLocation, Money).

## Async / Await

- `async`/`await` throughout — **NO** `.Result` or `.Wait()`.
- **Every I/O method** accepts `CancellationToken`.
- `ConfigureAwait(false)` in library projects (Application, Infrastructure).
- `IAsyncEnumerable<T>` for streaming queries (CSV export — BR-OFF-022).

## Result Pattern

Business rule violations return `Result.Failure(...)`, NOT exceptions:

```csharp
// ✅ Business rule violation
return Result.Failure(Errors.Auth.AccountLocked);

// ❌ Don't throw for business logic
throw new AccountLockedException();
```

Exceptions are only for infrastructure failures (DB down, S3 timeout) or programmer bugs.

**Error types:** `Validation | NotFound | Conflict | Forbidden | BusinessRule | Unexpected`

## Clean Architecture Dependency Rule

```
Api ──► Application ──► Domain
 │           │
 └──► Infrastructure ──► Application (interfaces) ──► Domain
```

- **Domain:** NO references to other projects. NO `Microsoft.*`, NO `EntityFrameworkCore`.
- **Application:** References Domain only. Defines interfaces, Infrastructure implements.
- **Infrastructure:** References Application + Domain. All framework-specific code here.
- **Api:** References Application + Infrastructure (DI only). Keep thin.

**Hard rule:** If `using Microsoft.EntityFrameworkCore` appears in Domain or Application (except `IApplicationDbContext`) — **stop and fix**.

## Naming Conventions

| Item | Convention | Example |
|------|-----------|---------|
| Project | `GreenLens.<Layer>` PascalCase | `GreenLens.Domain` |
| Async method | `Async` suffix | `SubmitAsync()` |
| DTO output | `XxxDto` | `ReportDto` |
| HTTP input | `XxxRequest` | `SubmitReportRequest` |
| Application input | `XxxCommand` / `XxxQuery` | `SubmitReportCommand` |
| Enum | Singular, PascalCase | `ReportStatus` |
| Migration | `yyyyMMddHHmm_VerbNoun` | `202605091200_AddSlaColumns` |
| JSON keys | `camelCase` | `firstName` |
| URL paths | `kebab-case` | `/pollution-reports` |
| DB columns | `snake_case` | `first_name` |
| Enum values (JSON) | `UPPER_SNAKE_CASE` | `IN_PROGRESS` |

## Validation (2 Layers)

1. **Input validation** (FluentValidation) — format, length, range. Pipeline behavior.
2. **Business validation** in handler — needs DB (role check, duplicate check).

## Database Conventions

- Snake_case in DB via `EFCore.NamingConventions`.
- All entities inherit `AuditableEntity` (CreatedAt, CreatedBy, UpdatedAt, UpdatedBy).
- Soft delete for User, Report, Comment (`DeletedAt` nullable + global query filter).
- One `SaveChanges()` per request via `IUnitOfWork` / `TransactionBehavior`.
- Geo: `NetTopologySuite.Geometries.Point` (SRID 4326), GIST index.
- Pagination mandatory: cursor-based preferred, max pageSize=100.

## API Response Envelope

ALL responses use:
```json
{ "code": "SUCCESS", "message": "...", "status": 200, "data": { ... } }
```

Errors follow RFC 7807 Problem Details.

## Auth

- JWT Bearer + refresh token rotation.
- Policies, NOT role strings: `options.AddPolicy(Policies.CanVerifyReport, ...)`.
- `ICurrentUser` in Application, wraps `IHttpContextAccessor` in Infrastructure.

## Comments

- English in code. Vietnamese OK in XML doc for BR terms.
- Only "why" comments, never "what" comments.
- BR IDs in XML doc on every handler implementing business rules.

## Git

- Trunk-based. Branches: `feature/<ticket>-<slug>`, `fix/...`, `chore/...`.
- Conventional Commits: `feat(reports): submit report endpoint (BR-REP-001..013)`.
- PR template lists BR IDs implemented and tested.

## Before Adding Packages

> **Ask the user first.** Do not add new large dependencies without approval.
