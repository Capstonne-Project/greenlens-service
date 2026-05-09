---
name: greenlens-release
description: >
  Produces rollout checklist and risk log for deploying GreenLens backend changes.
  Use before merging to develop/main or deploying to staging/production.
  Triggers: "release", "deploy", "rollout", "merge", "ship", "go live".
---

# GreenLens — Release Step

> **Goal:** Produce a rollout checklist and risk log to ensure safe deployment.

## When to use this skill

- Before merging a feature branch to `develop` or `main`.
- Before deploying to staging or production.
- After all tests pass from the Test step.

## Pre-Release Validation

- [ ] `dotnet build` — zero errors, zero warnings
- [ ] `dotnet test` — all tests pass
- [ ] No secrets committed (connection strings, JWT keys, S3 creds, FCM keys)
- [ ] `appsettings.json` contains only non-sensitive values
- [ ] Secrets managed via `dotnet user-secrets` (dev) or env vars / Key Vault (prod)

## Rollout Checklist Template

```markdown
# Release Checklist — [Feature/PR Name]

## PR Information
- **Branch:** `feature/<ticket>-<slug>`
- **Target:** `develop` / `main`
- **Author:** @name
- **Reviewers:** ≥ 1 approved

## Commit Convention
- Format: `feat(reports): submit report endpoint (BR-REP-001..013)`
- All BR IDs listed in commit message

## Code Quality
- [ ] Dependency rule respected (Domain → Application → Infrastructure → Api)
- [ ] No `using Microsoft.EntityFrameworkCore` in Domain/Application
- [ ] All handlers have BR ID XML comments
- [ ] Response envelope `{code, message, status, data}` on all endpoints
- [ ] CancellationToken on all I/O methods
- [ ] `ConfigureAwait(false)` in library projects
- [ ] Nullable reference types — no warnings

## Database
- [ ] Migration included: `yyyyMMddHHmm_VerbNoun`
- [ ] Migration has rollback test
- [ ] Migration is self-contained (no cross-migration dependencies)
- [ ] Production: uses `dotnet ef migrations bundle` (NOT auto-migrate at startup)
- [ ] Required indexes present (see OVERVIEW.md §4.6)
- [ ] Soft delete filter for User/Report/Comment

## API Contract
- [ ] Swagger/OpenAPI annotations complete
- [ ] Postman collection updated
- [ ] Response codes match 00_API_CONVENTIONS.md §3
- [ ] Business codes match 00_API_CONVENTIONS.md §4
- [ ] Rate limit headers present (X-RateLimit-*)
- [ ] Pagination follows standard format

## Auth & Security (§13)
- [ ] JWT RS256 for production (HS256 dev only)
- [ ] `ValidAlgorithms` fixed to prevent alg=none attack
- [ ] Authorization via policies, not role strings
- [ ] Resource-based authz for ownership (IDOR tests exist)
- [ ] bcrypt ≥ 12 rounds (`BcryptPasswordHasher`, NOT Identity default — BR-DAT-001)
- [ ] Refresh token: rotation + SHA-256 hash stored (no plaintext)
- [ ] JWT claims minimal: NO PII (email, phone)
- [ ] Rate limits 2 layers: Cloudflare WAF edge + ASP.NET app (BR-SYS-004)
- [ ] Turnstile integrated for login 3rd fail + register + forgot password (BR-AUTH-011)
- [ ] `OwaspHeaders.Core` configured: HSTS, CSP, X-Frame-Options, nosniff (§13.6)
- [ ] CSP allows `challenges.cloudflare.com` (Turnstile) + `media.greenlens.example` (R2)
- [ ] CORS strict origin list — NO `AllowAnyOrigin().AllowCredentials()` (§13.7)
- [ ] ForwardedHeaders reads `CF-Connecting-IP`, NOT `X-Forwarded-For` (§14.5)
- [ ] No PII in logs at Information level
- [ ] EXIF stripped from images before AI service (BR-AI-007)
- [ ] No secrets committed (connection strings, JWT keys, R2 keys, Turnstile secret, FCM)
- [ ] Secrets via Key Vault / Secrets Manager on prod (NOT env vars)
- [ ] `dotnet list package --vulnerable` — no Critical/High

## Performance (BR-SYS-001)
- [ ] API p95 < 2s verified at expected load
- [ ] Response compression (Brotli) enabled
- [ ] Pagination on all list endpoints
- [ ] No N+1 queries — `.Include()` or projection used
- [ ] Cache: Cloudflare edge (media 1yr) + Redis (API 1-10min)
- [ ] Media served via R2 custom domain + Cloudflare Cache (NOT *.r2.dev)
- [ ] Background jobs for heavy work (AI, notification, export)
- [ ] Rate limiter backed by Redis for horizontal scaling

## Observability
- [ ] Serilog structured logging configured
- [ ] OpenTelemetry tracing enabled
- [ ] Audit log for sensitive actions (BR-ADM-010)
- [ ] Correlation IDs on error responses (CF-Ray correlated)

## Cloudflare (§14)
- [ ] DNS orange-cloud (proxied) for public subdomains
- [ ] SSL/TLS mode: Full (strict)
- [ ] WAF managed rulesets enabled (OWASP + Cloudflare)
- [ ] Rate limiting rules at edge configured
- [ ] R2 bucket scoped API token (NOT account-wide)
- [ ] Turnstile allowed hostnames configured (block localhost in prod)
- [ ] Authenticated Origin Pulls / Cloudflare Tunnel active

## Environment-Specific
| Item | Dev | Staging | Production |
|------|-----|---------|------------|
| Migration | Auto at startup | Bundle | Bundle |
| Secrets | user-secrets | Env vars | Key Vault |
| JWT algo | HS256 | RS256 | RS256 |
| Turnstile | Test keys | Test keys | Prod keys |
| Logging | Debug | Information | Warning |
| Feature flags | All on | Selective | Controlled |
```

## Risk Log Template

```markdown
# Risk Log — [Feature Name]

| # | Risk | Impact | Likelihood | Mitigation | Owner | Status |
|---|------|--------|-----------|------------|-------|--------|
| 1 | Migration fails on prod data | High | Low | Tested against staging clone | DevOps | ⬜ Open |
| 2 | AI service timeout spikes | Medium | Medium | Fallback queue (BR-AI-006) | Backend | ✅ Mitigated |
| 3 | Rate limit too aggressive | Low | Low | Feature flag to adjust | Backend | ✅ Mitigated |
```

### Risk Categories
- **Data:** Migration failures, data loss, corruption
- **Performance:** Latency regression, resource exhaustion
- **Security:** Auth bypass, PII exposure, injection, secret leak
- **Integration:** External service (AI, R2, FCM, Cloudflare) failures
- **Rollback:** Can this change be safely reverted?

## Rollback Plan

```markdown
## Rollback Procedure
1. Revert merge commit: `git revert <sha>`
2. If migration was applied:
   - Run reverse migration: `dotnet ef database update <previous-migration>`
   - NEVER delete a merged migration — add a new reverting migration
3. Redeploy previous build
4. Verify health checks pass
5. Notify team in #greenlens-ops
```

## Post-Deploy Verification

- [ ] Health check endpoint returns 200
- [ ] Smoke test critical flows (login, submit report, map query, media upload)
- [ ] Monitor error rates for 30 minutes
- [ ] Check audit logs for new action types
- [ ] Verify background jobs are running on schedule
- [ ] Verify Cloudflare WAF not blocking legitimate traffic

## Definition of Done (from 00_API_CONVENTIONS.md §12)

- [ ] Endpoint code merged
- [ ] Request/response match spec 100%
- [ ] Response envelope correct
- [ ] Validation complete (field-level errors)
- [ ] Authorization check (role + ownership)
- [ ] Audit log for sensitive actions
- [ ] Unit test happy path + ≥1 error case
- [ ] Integration test with DB
- [ ] Swagger/OpenAPI annotation complete
- [ ] Postman collection updated
- [ ] BR IDs in commit message
- [ ] PR review ≥ 1 approve
- [ ] QA sign-off

## Sources & References

| Source | Path | Description |
|--------|------|-------------|
| OVERVIEW.md | `OVERVIEW.md §6-§14` (v1.1) | Coding standards, config, performance, security (§13), Cloudflare (§14) |
| API Conventions | `00_API_CONVENTIONS.md §12` | Definition of Done checklist |
| Business Rules | `SU26SE049_BusinessRules_v1_0.docx` | BR-SYS-*, BR-DAT-*, BR-ADM-* for ops |
| Security Patterns | `../csharp-conventions/resources/security-patterns.md` | Auth, headers, CORS, Turnstile, R2, secrets rotation |
| Performance Patterns | `../csharp-conventions/resources/performance-patterns.md` | Compression, caching, R2 zero-egress, DB optimization |
| Best Practices | `../csharp-conventions/resources/best-practices.md` | Security, performance, data access pitfalls |

