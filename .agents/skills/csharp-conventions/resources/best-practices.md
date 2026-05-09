# Best Practices & Common Pitfalls — GreenLens

> Consolidated DO/DON'T rules and common traps from OVERVIEW.md (v1.1) and 00_API_CONVENTIONS.md.

---

## ✅ DO — Best Practices

### Architecture
- **DO** follow the dependency rule: Domain ← Application ← Infrastructure ← Api
- **DO** use vertical slices: 1 use case = 1 folder (Command + Handler + Validator + Response)
- **DO** keep controllers thin — only `sender.Send(cmd)` + `.ToHttp()`
- **DO** define interfaces in Application, implement in Infrastructure
- **DO** use `ICurrentUser` (Application) to wrap `IHttpContextAccessor` (Infrastructure)

### C# Language
- **DO** use `sealed` on all non-abstract classes
- **DO** use `record` for DTOs, Commands, Queries, Events (immutable)
- **DO** use `class` for entities (behavior + identity)
- **DO** use `record struct` for small value objects (GeoLocation, Money)
- **DO** use file-scoped namespaces everywhere
- **DO** use primary constructors for DI
- **DO** use collection expressions `[1, 2, 3]`
- **DO** enable nullable reference types project-wide

### Async
- **DO** use `async`/`await` for all I/O operations
- **DO** pass `CancellationToken` to every I/O method
- **DO** use `ConfigureAwait(false)` in Application and Infrastructure
- **DO** use `IAsyncEnumerable<T>` for streaming (CSV export)

### Data Access
- **DO** use `AsNoTracking()` for read-only queries
- **DO** use Mapster `.ProjectToType<>()` for DTO projection
- **DO** use one `SaveChanges()` per request via `TransactionBehavior`
- **DO** inherit all entities from `AuditableEntity`
- **DO** use global query filter for soft delete

### Error Handling
- **DO** use `Result<T>` for business rule violations
- **DO** use exceptions only for infrastructure failures (DB down, R2 timeout)
- **DO** use RFC 7807 Problem Details for error responses
- **DO** map `ErrorType` to HTTP status in the Api layer
- **DO** use centralized `Errors.Module.ErrorName` definitions

### Validation
- **DO** use FluentValidation for input validation (format, length, range)
- **DO** use handler-level validation for business rules (needs DB)
- **DO** return field-level errors in response

### Testing
- **DO** name tests with BR IDs: `Method_Scenario_Expected_BR_XXX_NNN`
- **DO** use Testcontainers for integration tests (never mock DbContext)
- **DO** use Respawn for schema reset between test classes
- **DO** write happy path + ≥ 1 error case per endpoint

### Security (§13)
- **DO** use authorization policies, not role strings
- **DO** use RS256 for JWT in production (HS256 only dev)
- **DO** fix `ValidAlgorithms` on verifier (prevent `alg=none` attack)
- **DO** use bcrypt ≥ 12 rounds (`BcryptPasswordHasher`), NOT Identity default PBKDF2
- **DO** hash refresh tokens with SHA-256 before storing
- **DO** use `OwaspHeaders.Core` for HSTS, CSP, X-Frame-Options, nosniff
- **DO** use `CF-Connecting-IP` for real IP (whitelist Cloudflare IPs)
- **DO** verify Turnstile token BEFORE business logic (BR-AUTH-011)
- **DO** validate Turnstile `action` + `hostname` fields
- **DO** use strict CORS origin lists per policy
- **DO** strip EXIF before sending images to AI service
- **DO** validate content-type by magic bytes, not file extension
- **DO** re-encode images via ImageSharp (remove malicious payloads)
- **DO** use resource-based authorization for ownership (IDOR prevention)
- **DO** write IDOR tests for every endpoint accepting an ID
- **DO** use Azure Key Vault / Vault for secrets in production
- **DO** rotate R2 keys every 90 days, JWT RS256 key every 90 days

### API Convention
- **DO** use response envelope `{code, message, status, data}` on ALL responses
- **DO** use `camelCase` for JSON keys, `kebab-case` for URL paths
- **DO** paginate all list endpoints (max pageSize=100)
- **DO** include rate limit headers on every response

### Documentation
- **DO** add BR ID XML comments on every handler implementing business rules
- **DO** comment "why", never "what"
- **DO** use Conventional Commits with BR IDs

---

## ❌ DON'T — Anti-Patterns

### Architecture
- **DON'T** import `Microsoft.EntityFrameworkCore` in Domain or Application
- **DON'T** import `IHttpContextAccessor` in Application
- **DON'T** create monolithic "Service" classes (e.g., `ReportService` with 20 methods)
- **DON'T** put business logic in controllers
- **DON'T** inject repositories directly into controllers

### C# Language
- **DON'T** use `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()` (deadlock)
- **DON'T** use `async void` (except event handlers)
- **DON'T** fire-and-forget async calls (exceptions lost)
- **DON'T** use public setters on entity state (use domain methods)

### Data Access
- **DON'T** mock `DbContext` in tests — use Testcontainers
- **DON'T** call `SaveChanges()` multiple times in one request
- **DON'T** use tracking for read-only queries
- **DON'T** create N+1 queries (load in loop)
- **DON'T** auto-migrate in production (use migration bundle)
- **DON'T** delete a merged migration — add a new reverting one

### Error Handling
- **DON'T** throw exceptions for business rule violations
- **DON'T** use try/catch for control flow
- **DON'T** swallow exceptions silently

### Security (§13)
- **DON'T** commit connection strings, JWT keys, R2 keys, Turnstile secrets, FCM keys
- **DON'T** use env vars for secrets in production (process listing leaks)
- **DON'T** log PII (email, phone, detailed GPS) at Information level
- **DON'T** put PII in JWT claims (email, phone) — they leak via logs
- **DON'T** trust file extensions for content-type validation
- **DON'T** use role strings directly — use policies
- **DON'T** use `AllowAnyOrigin().AllowCredentials()` — reflective CORS hole
- **DON'T** trust `X-Forwarded-For` raw — anyone can spoof it
- **DON'T** use `*.r2.dev` for production media (rate-limited)
- **DON'T** use Identity default `PasswordHasher<TUser>` (prefer bcrypt)
- **DON'T** use HS256 for JWT in production (use RS256)
- **DON'T** use `FromSqlRaw` with user input (SQL injection)
- **DON'T** use `UnsafeRelaxedJsonEscaping` for user-facing output
- **DON'T** concat strings in email templates (XSS — use Razor/Scriban)
- **DON'T** call Turnstile Siteverify from frontend (secret key leak)
- **DON'T** skip hostname/action validation on Turnstile tokens

### Packages
- **DON'T** add new large dependencies without asking the user
- **DON'T** change the tech stack (ORM, framework) without approval

---

## ⚠️ Common Pitfalls

### 1. Captive Dependency
```csharp
// ❌ Singleton captures Scoped → memory leak
services.AddSingleton<MyService>(); // MyService depends on IApplicationDbContext (Scoped)

// ✅ Fix: Use IServiceScopeFactory
public sealed class MyService(IServiceScopeFactory scopeFactory) { ... }
```

### 2. Forgetting Global Query Filter
```csharp
// ❌ Soft-deleted records still visible
var users = await db.Users.ToListAsync(ct);  // Only if filter is configured!

// ✅ Ensure filter exists in configuration
builder.HasQueryFilter(u => u.DeletedAt == null);

// ✅ When you need deleted records (admin only)
var allUsers = await db.Users.IgnoreQueryFilters().ToListAsync(ct);
```

### 3. SRID Mismatch in Geo Queries
```csharp
// ❌ Missing SRID — wrong results
var point = new Point(lng, lat);  // SRID defaults to 0!

// ✅ Always set SRID 4326
var point = new Point(lng, lat) { SRID = 4326 };
```

### 4. State Machine Bypass
```csharp
// ❌ Direct property set — bypasses rules
report.Status = ReportStatus.Verified;  // No validation, no events!

// ✅ Use domain methods
report.Verify(officerId);  // Validates transition, raises event
```

### 5. Missing Transaction on Commands
```csharp
// ❌ Partial save if second operation fails
db.Reports.Add(report);
await db.SaveChangesAsync(ct);  // Saved
await externalService.NotifyAsync(report, ct);  // Fails → inconsistent state

// ✅ Use outbox pattern + TransactionBehavior
db.Reports.Add(report);
db.OutboxMessages.Add(new OutboxMessage("notify", report.Id));
await db.SaveChangesAsync(ct);  // All or nothing
```

### 6. Leaking Tracking Context
```csharp
// ❌ Returning tracked entity from handler — EF may cache stale data
return await db.Reports.FindAsync(id, ct);

// ✅ Project to DTO immediately
return await db.Reports.AsNoTracking()
    .Where(r => r.Id == id)
    .ProjectToType<ReportDto>()
    .FirstOrDefaultAsync(ct);
```

### 7. GPS Precision Leak (BR-MAP-004)
```csharp
// ❌ Exposing full GPS precision publicly
return new { lat = report.Latitude, lng = report.Longitude };

// ✅ Round to 10m precision for public responses
return new {
    lat = Math.Round(report.Latitude, 4),   // ≈11m precision
    lng = Math.Round(report.Longitude, 4)
};
```

### 8. Rate Limit Not Enforced Server-Side
```csharp
// ❌ Relying only on client-side throttling
// Client can bypass — always enforce on server

// ✅ Redis sliding window (BR-REP-010, BR-SYS-004)
if (await rateLimiter.IsRateLimitedAsync(userId, "submit", 5, TimeSpan.FromHours(1), ct))
    return Errors.Report.RateLimitExceeded;

// ✅ 2-layer: Cloudflare WAF at edge + ASP.NET app layer
// Edge blocks DDoS; app enforces per-userId limits
```

### 9. Using R2 Dev URL in Production
```csharp
// ❌ *.r2.dev is rate-limited, NOT for production
return $"https://pub-xxx.r2.dev/{key}";

// ✅ Custom domain via Cloudflare
return $"https://media.ecoreport.example/{key}";
```

### 10. Reflective CORS
```csharp
// ❌ Security hole — reflects any origin with credentials
services.AddCors(o => o.AddDefaultPolicy(p => p
    .AllowAnyOrigin()
    .AllowCredentials()));

// ✅ Strict origin list
services.AddCors(o => o.AddPolicy("AuthedApi", p => p
    .WithOrigins("https://app.ecoreport.example")
    .AllowCredentials()
    .AllowAnyMethod()
    .AllowAnyHeader()));
```
