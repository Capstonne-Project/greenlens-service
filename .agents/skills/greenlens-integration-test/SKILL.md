---
name: greenlens-integration-test
description: Scaffold an integration test (handler-level with Testcontainers Postgres+PostGIS) or functional test (HTTP-level with WebApplicationFactory) for the Greenlens .NET 9 backend (project SU26SE049). Use this skill whenever the user asks for tests touching the database or HTTP layer — including casual phrasings like "test the SubmitReport handler", "write integration tests for ReportRepository", "test the API endpoint for verifying a report", "set up Testcontainers", "add a test that hits Postgres", or "test the auth flow end-to-end". DO NOT use for pure unit tests (Domain entities, validators, value objects) — those don't need this scaffolding. Trigger this even when the user just says "test this" if the thing under test touches DB or HTTP. Produces test classes with shared Testcontainer fixture, Respawn for DB reset, WebApplicationFactory override, and BR-tagged test names.
---

# Greenlens Integration & Functional Tests

OVERVIEW.md §7 sets the testing pyramid: ~70% unit, ~25% integration (Testcontainers Postgres), ~5% functional (WebApplicationFactory). This skill covers the bottom two tiers — anything that needs a real DB or a real HTTP pipeline.

## When to use

Trigger when the user mentions:
- "integration test", "functional test", "end-to-end test"
- "Testcontainers", "Postgres test", "Respawn"
- "WebApplicationFactory", "test the endpoint", "test the API"
- "test the handler" (if the handler touches DB)
- "test the repository"
- "test the auth flow"

**Do NOT trigger for** pure unit tests of Domain entities, validators, or value objects — those use plain xUnit without Testcontainers/WAF (they live in `*.UnitTests` projects).

## Workflow

1. **Identify which test type:**
   - **Integration test** (`Greenlens.Application.IntegrationTests`) — tests handlers, repositories, EF configurations against a real Postgres+PostGIS container. Goes through MediatR pipeline. Does NOT go through HTTP. Most common.
   - **Functional test** (`Greenlens.Api.FunctionalTests`) — tests the full HTTP pipeline (auth, controllers, model binding, error mapping). Used sparingly — for happy path of critical flows + auth.

2. **Confirm the BR ID(s)** the test covers. Test name format: `<Action>_<Condition>_<Expected>_BR_XXX_NNN` (xUnit `[Fact]`).

3. **Pick the template:**
   - `assets/integration-fixture.cs.template` — one-time setup: `PostgresContainerFixture`, `IntegrationTestBase` (Respawn-backed)
   - `assets/integration-test.cs.template` — a test class for one feature (e.g. `SubmitReportTests`)
   - `assets/functional-fixture.cs.template` — `GreenlensWebApplicationFactory<TProgram>` with overrides
   - `assets/functional-test.cs.template` — HTTP test class
   - `assets/test-data-builder.cs.template` — fluent builder for entities (avoids boilerplate)

4. **Generate the file** under `tests/Greenlens.Application.IntegrationTests/Features/<Module>/` or `tests/Greenlens.Api.FunctionalTests/Endpoints/<Resource>/`.

## Conventions

- xUnit + FluentAssertions + Testcontainers + Respawn + NSubstitute (mocks for non-DB boundaries: AI service, email sender, Cloudflare Turnstile).
- **Use the official `postgis/postgis:16-3.4` image**, not plain `postgres` — the project needs PostGIS.
- **One container per test collection**, NOT per test. Respawn resets the DB between tests inside the collection. Containers are expensive (~5s startup).
- **Do not mock EF** — the whole point of these tests is exercising real EF/Postgres behavior.
- **Mock external services** (AI, email, Turnstile, R2) at the boundary interface — NSubstitute or in-memory fakes.
- **Test name encodes the BR**: `Verify_BySameUser_ReturnsConflictOfInterest_BR_OFF_004`.
- **Arrange-Act-Assert** with blank lines between sections.
- **No shared mutable state across tests** beyond what the fixture sets up. Respawn resets between.
- **WAF tests use real authentication tokens** — generate via a test helper that signs JWTs with the same key as `appsettings.Testing.json`.
- **Functional tests run on `WebApplicationFactory<Program>`** — `Program` must be `public partial class Program { }` at the bottom of `Program.cs` for this to work.

## DB lifecycle

```
[CollectionFixture: PostgresContainerFixture]
   start container, run EF migrations    (once per test run)
   ↓
[For each test in collection]
   Respawn.ResetAsync(connectionString)  (clean slate, < 100ms)
   seed test-specific data via TestDataBuilder
   run handler/HTTP request
   assert
```

## Self-check

- [ ] File in correct project: `Greenlens.Application.IntegrationTests` or `Greenlens.Api.FunctionalTests`
- [ ] Test class decorated with `[Collection("Postgres")]` to share container
- [ ] `IClassFixture<TFixture>` if test class needs setup
- [ ] Test names follow `<Action>_<Condition>_<Expected>_BR_XXX_NNN`
- [ ] AAA structure with blank lines
- [ ] Mocks only at external boundaries
- [ ] Container image is `postgis/postgis:16-3.4`, not plain `postgres`
- [ ] Respawn called between tests (in `IAsyncLifetime.InitializeAsync`)
- [ ] No `Thread.Sleep` — use `Task.Delay` with a `WithTimeout` helper if necessary

## Templates

- `assets/integration-fixture.cs.template` — PostgresContainerFixture + IntegrationTestBase (one-time)
- `assets/integration-test.cs.template` — example test class for a handler
- `assets/functional-fixture.cs.template` — GreenlensWebApplicationFactory (one-time)
- `assets/functional-test.cs.template` — HTTP test class
- `assets/test-data-builder.cs.template` — fluent builder pattern

## Common pitfalls (project-specific)

| Pitfall | Why bad | Fix |
|---|---|---|
| Using `WebApplicationFactory<Startup>` (old style) | .NET 9 uses top-level Program | Use `WebApplicationFactory<Program>` and add `public partial class Program { }` at end of Program.cs |
| Mocking `IApplicationDbContext` | Project explicitly avoids this (OVERVIEW.md §7) | Use Testcontainers Postgres |
| Not resetting between tests | Test pollution | Respawn in `InitializeAsync` |
| Re-running migrations in every test | Slow | Migrate once in fixture's `IAsyncLifetime.InitializeAsync` |
| Hardcoding Cloudflare Turnstile site key in tests | Won't pass siteverify | Use Cloudflare's documented test sitekey/secret pair (always-pass) |
| Asserting `ProblemDetails.Title` text | Locale-dependent | Assert `Status` + error code in `extensions.code` |
| Using Hangfire's real scheduler in tests | Tests become time-flaky | Replace with `BackgroundJobClientMock` or invoke job class directly |
