# Security Patterns — GreenLens

> **Source:** OVERVIEW.md §13 Security, §14 Cloudflare Integration (v1.1)

## Defense in Depth — 3 Layers

```
Edge (Cloudflare)  →  App (ASP.NET Core)  →  DB (PostgreSQL)
  WAF + Rate Limit      Auth + CORS + Headers    TDE + Row-Level
  Turnstile CAPTCHA     Result pattern            Global query filters
  DDoS protection       Data Protection API       Check constraints
```

> Each layer must be independently secure. Edge bypass must NOT compromise the app.

## Authentication Hardening (§13.2)

```csharp
// ✅ bcrypt ≥ 12 rounds — NOT Identity's default PBKDF2
services.AddSingleton<IPasswordHasher<User>, BcryptPasswordHasher>();

// ✅ RS256 for production — fix algorithm to prevent alg=none attack
options.TokenValidationParameters = new()
{
    ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
    ValidIssuer = "greenlens-api",
    ValidAudience = "greenlens-client",
    IssuerSigningKey = rsaKey,
};

// ✅ Minimal JWT claims — NO PII (email, phone) in token
// Claims: sub (userId), role, iat, exp, jti only

// ✅ Refresh token rotation — hash before storing
var tokenHash = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
// Detect reuse → revoke ALL user sessions (credential theft signal)

// ✅ Token blacklist on logout — Redis TTL = remaining JWT lifetime
await cache.SetAsync($"blacklist:{jti}", true, remainingLifetime, ct);

// ✅ Account enumeration prevention
// Same message for both "email not found" and "wrong password":
return Errors.Auth.InvalidCredentials; // "Email hoặc mật khẩu không đúng"
```

## Authorization (§13.3)

```csharp
// ✅ Policy-based — centralized in Api/Authorization/Policies.cs
public static class Policies
{
    public const string CanVerifyReport = nameof(CanVerifyReport);
    public const string CanAssignTeam   = nameof(CanAssignTeam);
    public const string CanExportData   = nameof(CanExportData);
}

// ✅ Resource-based authorization for ownership (BR-REP-026)
// Use IAuthorizationService + IAuthorizationHandler<TRequirement, TResource>
// DON'T scatter report.ReporterId == currentUser.Id checks

// ✅ IDOR prevention — test is MANDATORY
[Fact]
public async Task GetReport_OtherUserReport_Returns403_BR_DAT_003() { ... }

// ✅ Segregation of duties (BR-OFF-004) — check in handler, NOT filter
if (report.CreatedBy == currentUser.UserId)
    return Errors.Officer.CannotVerifyOwnReport;
```

## HTTP Security Headers (§13.6)

```csharp
// ✅ Use OwaspHeaders.Core — one middleware, full OWASP set
app.UseSecureHeadersMiddleware(SecureHeadersMiddlewareBuilder
    .CreateBuilder()
    .UseHsts(maxAge: 31_536_000, includeSubDomains: true)
    .UseContentTypeOptions()                        // nosniff
    .UseContentSecurityPolicy(builder => builder
        .WithDefaultSrc(s => s.Self())
        .WithScriptSrc(s => s.Self()
            .From("https://challenges.cloudflare.com"))    // Turnstile JS
        .WithImgSrc(s => s.Self()
            .From("data:")
            .From("https://media.ecoreport.example")))     // R2 domain
    .UseXFrameOptions(XFrameOptions.Deny)
    .UseReferrerPolicy(ReferrerPolicy.NoReferrer)
    .UseCrossOriginResourcePolicy(CrossOriginResourcePolicy.SameOrigin)
    .Build());
```

**Required headers:**
- `Strict-Transport-Security` — force HTTPS (BR-DAT-001)
- `Content-Security-Policy` — anti-XSS, allow Turnstile + R2 domains
- `X-Content-Type-Options: nosniff` — anti-MIME sniffing
- `X-Frame-Options: DENY` — anti-clickjacking
- `Referrer-Policy: no-referrer` — no URL leaks

## CORS (§13.7)

```csharp
// ✅ Strict origin list per policy
services.AddCors(options =>
{
    options.AddPolicy("PublicApi", p => p
        .WithOrigins("https://ecoreport.example", "https://m.ecoreport.example")
        .WithMethods("GET")
        .WithHeaders("Content-Type"));

    options.AddPolicy("AuthedApi", p => p
        .WithOrigins("https://app.ecoreport.example")
        .AllowCredentials()
        .AllowAnyMethod()
        .AllowAnyHeader());
});

// ❌ NEVER: AllowAnyOrigin().AllowCredentials() — reflective CORS hole
```

## Rate Limiting — 2 Layers (§13.8)

```
Layer 1: Cloudflare WAF (edge) — blocks DDoS, known-bad bots
Layer 2: ASP.NET RateLimiter (app) — per-userId policies
```

```csharp
// ✅ App-layer rate limiter (BR-SYS-004)
services.AddRateLimiter(options =>
{
    // 60 rpm/IP anonymous
    options.AddPolicy("anon-ip", ctx => RateLimitPartition.GetSlidingWindowLimiter(
        partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
        factory: _ => new() { PermitLimit = 60, Window = TimeSpan.FromMinutes(1) }));

    // 300 rpm/user authenticated
    options.AddPolicy("user", ctx => RateLimitPartition.GetSlidingWindowLimiter(
        partitionKey: ctx.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anon",
        factory: _ => new() { PermitLimit = 300, Window = TimeSpan.FromMinutes(1) }));
});
// ⚠️ Production: back with Redis for horizontal scaling
```

## Cloudflare Turnstile (§14.4)

```csharp
// Application/Common/Interfaces/ITurnstileVerifier.cs
public interface ITurnstileVerifier
{
    Task<TurnstileResult> VerifyAsync(
        string token, string? remoteIp, string? expectedAction, CancellationToken ct);
}

// Infrastructure/Security/TurnstileVerifier.cs — calls Siteverify endpoint
// DI: services.AddHttpClient<ITurnstileVerifier, TurnstileVerifier>()
//         .AddStandardResilienceHandler();
```

**Rules:**
- Verify token BEFORE business logic
- Token is single-use, 5-min TTL
- Validate `action` + `hostname` fields
- Use dummy keys in CI (test keys from Cloudflare docs)

## Cloudflare R2 File Storage (§14.2)

```csharp
// Infrastructure/Storage/R2FileStorage.cs
// Uses AWSSDK.S3 with R2 endpoint — S3-compatible
var config = new AmazonS3Config
{
    ServiceURL = "https://<account-id>.r2.cloudflarestorage.com",
    ForcePathStyle = true,
};

// ⚠️ R2-specific: DisablePayloadSigning + DisableDefaultChecksumValidation
var req = new PutObjectRequest
{
    DisablePayloadSigning = true,
    DisableDefaultChecksumValidation = true,
};

// Public URL via custom domain, NOT *.r2.dev
public string GetPublicUrl(string key) => $"https://media.ecoreport.example/{key}";
```

## Forwarded Headers (§14.5)

```csharp
// ✅ Read real IP from CF-Connecting-IP, NOT X-Forwarded-For
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedForHeaderName = "CF-Connecting-IP";
    options.ForwardLimit = 1;
    // Whitelist Cloudflare IP ranges only
    foreach (var cidr in CloudflareIpRanges.V4)
        options.KnownNetworks.Add(IPNetwork.Parse(cidr));
});

// Middleware order: UseForwardedHeaders() BEFORE UseAuthentication/UseRateLimiter
```

## Secrets Rotation (§13.4)

| Secret | Cadence | Method |
|--------|---------|--------|
| JWT signing key (RS256) | 90 days | Dual-key window |
| R2 access key | 90 days | Create new → config → revoke old |
| Turnstile secret | On suspected leak | Rotate FE+BE simultaneously |
| DB password | 180 days | Rolling restart |
| bcrypt cost | Annual review | Increase factor (12→13) |

## Input Security (§13.5)

```csharp
// ✅ 3-layer input validation
// 1. Cloudflare WAF (OWASP ManagedRuleset)
// 2. FluentValidation (shape, length, range)
// 3. Domain entity (business invariants)

// ✅ File upload: magic bytes check, NOT extension
// Use MimeDetective or read first 8-16 bytes
// Image bomb protection: max 8000×8000 dimensions
// Re-encode via ImageSharp before public serving

// ✅ Mass-assignment guard: Command record has ONLY user-settable fields
// Status, CreatedAt, ReporterId → set in handler, NOT in Command

// ❌ NEVER: FromSqlRaw with user input
// ❌ NEVER: UnsafeRelaxedJsonEscaping for user-facing output
// ❌ NEVER: string concat in email templates (use Razor/Scriban)
```

## Vulnerability Management (§13.10)

```bash
# Weekly in CI — block merge on Critical/High
dotnet list package --vulnerable --include-transitive

# SAST: SonarQube or GitHub CodeQL per PR
# DAST: OWASP ZAP baseline scan per release on staging
# Secret scanning: GitGuardian/TruffleHog in CI
```

## DO / DON'T Summary

```csharp
// ✅ DO — RS256 for prod JWT, HS256 only in dev
// ✅ DO — bcrypt ≥ 12, NOT Identity default PBKDF2
// ✅ DO — OwaspHeaders.Core for all security headers
// ✅ DO — Whitelist Cloudflare IPs for CF-Connecting-IP
// ✅ DO — Verify Turnstile token BEFORE business logic
// ✅ DO — Resource-based authz for ownership checks
// ✅ DO — IDOR tests for every endpoint accepting id

// ❌ DON'T — Put PII in JWT claims
// ❌ DON'T — AllowAnyOrigin().AllowCredentials()
// ❌ DON'T — Trust X-Forwarded-For raw (spoofable)
// ❌ DON'T — Use *.r2.dev for production media
// ❌ DON'T — Store refresh tokens as plaintext
// ❌ DON'T — FromSqlRaw with user input
// ❌ DON'T — Env vars for secrets on production (use Vault)
```
