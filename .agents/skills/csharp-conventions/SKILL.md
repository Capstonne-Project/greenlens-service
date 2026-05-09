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

Exceptions are only for infrastructure failures (DB down, R2 timeout) or programmer bugs.

**Error types:** `Validation | NotFound | Conflict | Forbidden | BusinessRule | Unexpected`

## Clean Architecture Dependency Rule

```
Api ──► Application ──► Domain
 │           │
 └──► Infrastructure ──► Application (interfaces) ──► Domain
```

- **Domain:** NO references to other projects. NO `Microsoft.*`, NO `EntityFrameworkCore`.
- **Application:** References Domain only. Defines interfaces (`IGenericRepository<T>`, `IUnitOfWork`, `IXxxRepository`). **NO** `DbContext`, **NO** `IApplicationDbContext`.
- **Infrastructure:** References Application + Domain. All framework-specific code here (EF, R2/S3 SDK, FCM, Turnstile…). `ApplicationDbContext` is `internal`.
- **Api:** References Application + Infrastructure (DI only). Keep thin.

**Hard rule:** If `using Microsoft.EntityFrameworkCore` appears in Domain or Application — **stop and fix**. Application must NEVER reference DbContext.

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

## Validation (3 Layers)

1. **Edge validation** (Cloudflare WAF / OWASP ManagedRuleset) — blocks known-bad payloads at edge.
2. **Input validation** (FluentValidation) — format, length, range. Pipeline behavior.
3. **Business validation** in handler — needs DB (role check, duplicate check).

## Data Access (Strict Repository — §4.12)

- All data access via `IXxxRepository : IGenericRepository<T>` + `IUnitOfWork`. **NEVER** `IApplicationDbContext` in Application.
- `IGenericRepository<T>`: `Query()`, `QueryAsNoTracking()`, `GetByIdAsync()`, `Add()`, `Remove()`, `ExistsAsync()`.
- Every entity has its own `IXxxRepository` (even CRUD-only with empty body).
- `GenericRepository<T>` is `internal abstract` in Infrastructure.
- Handler injects `IXxxRepository` + `IUnitOfWork`, **NEVER** `IGenericRepository<T>` directly.
- No repo has `SaveChangesAsync` — commit only via `IUnitOfWork`.
- DI: register each repo individually (`AddScoped<IReportRepository, ReportRepository>()`), NOT open generic.

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

## Auth & Security (§13)

- **JWT:** RS256 for production (HS256 dev only). Fix `ValidAlgorithms` to prevent `alg=none`.
- **Password:** bcrypt ≥ 12 rounds (`BcryptPasswordHasher`), NOT Identity default PBKDF2.
- **Refresh tokens:** rotation + SHA-256 hash in DB (no plaintext). Detect reuse → revoke all sessions.
- **JWT claims:** `sub`, `role`, `iat`, `exp`, `jti` only. NO PII (email, phone) in token.
- Policies, NOT role strings: `options.AddPolicy(Policies.CanVerifyReport, ...)`.
- Resource-based authorization for ownership (IDOR prevention).
- `ICurrentUser` in Application, wraps `IHttpContextAccessor` in Infrastructure.
- **Turnstile (CAPTCHA):** verify via `ITurnstileVerifier` BEFORE business logic (BR-AUTH-011).
- **HTTP headers:** `OwaspHeaders.Core` for HSTS, CSP, X-Frame-Options, nosniff (§13.6).
- **CORS:** strict origin list per policy. NEVER `AllowAnyOrigin().AllowCredentials()`.
- **Rate limiting:** 2 layers — Cloudflare WAF (edge) + ASP.NET `RateLimiterMiddleware` (app).
- **IP detection:** read `CF-Connecting-IP`, NOT `X-Forwarded-For` (spoofable).
- **Secrets:** Azure Key Vault / Vault for production. NO env vars (process listing risk).

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

## Resources

Detailed pattern guides with full code examples are in `resources/`:

| Resource | Description |
|----------|-------------|
| [folder-structure.md](resources/folder-structure.md) | Full solution tree, dependency rule, "where things go" decision table |
| [di-patterns.md](resources/di-patterns.md) | DI registration, lifetime guidelines, Options pattern, captive dependency fix |
| [async-patterns.md](resources/async-patterns.md) | Correct async/await, CancellationToken, ConfigureAwait, anti-patterns |
| [result-pattern.md](resources/result-pattern.md) | Result\<T\> implementation, Error definitions, HTTP mapping, DO/DON'T |
| [data-access-patterns.md](resources/data-access-patterns.md) | EF Core queries, projections, geo queries, auditing interceptor |
| [caching-patterns.md](resources/caching-patterns.md) | Multi-level cache (L1 Memory + L2 Redis), cache keys, invalidation, rate limiting |
| [security-patterns.md](resources/security-patterns.md) | Auth hardening, HTTP headers, CORS, Turnstile, R2, rate limiting, secrets rotation |
| [performance-patterns.md](resources/performance-patterns.md) | Compression, 3-level caching, R2 zero-egress, DB optimization, background jobs |
| [best-practices.md](resources/best-practices.md) | Consolidated DO/DON'T rules and common pitfalls with code examples |

## Sources & References

| Source | Path | Description |
|--------|------|-------------|
| OVERVIEW.md | `OVERVIEW.md` (v1.1) | Project conventions, architecture, security (§13), Cloudflare (§14) |
| API Conventions | `00_API_CONVENTIONS.md` | Response envelope, HTTP codes, field naming, pagination, auth |
| Business Rules | `SU26SE049_BusinessRules_v1_0.docx` | Source of truth for all BR-*-NNN rules |
| .NET 9 Docs | [learn.microsoft.com](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9) | C# 13 / .NET 9 features |
| EF Core 9 | [learn.microsoft.com](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-9.0/whatsnew) | EF Core 9 features and patterns |
| OwaspHeaders.Core | [github.com/GaProgMan/OwaspHeaders.Core](https://github.com/GaProgMan/OwaspHeaders.Core) | OWASP security headers middleware |
| Cloudflare R2 | [developers.cloudflare.com/r2](https://developers.cloudflare.com/r2/) | S3-compatible object storage |
| Cloudflare Turnstile | [developers.cloudflare.com/turnstile](https://developers.cloudflare.com/turnstile/) | CAPTCHA alternative |
| MediatR | [github.com/jbogard/MediatR](https://github.com/jbogard/MediatR) | CQRS pipeline behaviors |
| FluentValidation | [docs.fluentvalidation.net](https://docs.fluentvalidation.net) | Input validation rules |
| Mapster | [github.com/MapsterMapper/Mapster](https://github.com/MapsterMapper/Mapster) | Object mapping and projection |
