---
name: greenlens-test
description: >
  Guides test writing, execution, and failure triage for the GreenLens backend.
  Follows the testing pyramid (70% unit, 25% integration, 5% E2E) with xUnit,
  FluentAssertions, NSubstitute, and Testcontainers.
  Triggers: "test", "verify", "validate", "write tests", "fix test", "triage failure".
---

# GreenLens — Test Step

> **Goal:** Write and execute tests following the testing pyramid. Produce test results and triage failures.

## Testing Pyramid

| Layer | Ratio | Stack | Project |
|-------|-------|-------|---------|
| Unit | ~70% | xUnit + FluentAssertions + NSubstitute | `Greenlens.Domain.UnitTests`, `Greenlens.Application.UnitTests` |
| Integration | ~25% | + Testcontainers Postgres + Respawn | `Greenlens.Application.IntegrationTests` |
| E2E | ~5% | + WebApplicationFactory | `Greenlens.Api.FunctionalTests` |

## Test Naming Convention

Pattern: `{Method}_{Scenario}_{ExpectedResult}_{BR_ID}`

```csharp
[Fact]
public async Task SubmitReport_NoPhoto_ReturnsValidationError_BR_REP_001() { ... }
```

**Every BR ID from the Plan step MUST have at least 1 test.**

## Test Rules

- **AAA** (Arrange-Act-Assert), no shared state via static fields.
- **Mock only boundaries** (S3, AI, email, FCM) — NEVER mock DbContext.
- **Testcontainers** for DB tests with `postgis/postgis:18-3.4` image.
- **Respawn** for schema reset between test classes (NOT full DB drop).
- Happy path + ≥ 1 error case per endpoint.

## Unit Test Example (Domain)

```csharp
[Fact]
public void Verify_WhenSubmitted_ChangesStatusToVerified_BR_REP_020()
{
    // Arrange
    var report = CreateSubmittedReport();
    // Act
    report.Verify(Guid.NewGuid());
    // Assert
    report.Status.Should().Be(ReportStatus.Verified);
    report.DomainEvents.Should().ContainSingle()
        .Which.Should().BeOfType<ReportVerifiedEvent>();
}
```

## Integration Test Example

```csharp
public sealed class ReportQueryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgis/postgis:18-3.4").Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        // setup DbContext + migrate
    }
    public async Task DisposeAsync() => await _postgres.DisposeAsync();
}
```

## Running Tests

```bash
dotnet test                                          # all
dotnet test tests/Greenlens.Domain.UnitTests/        # specific project
dotnet test --filter "FullyQualifiedName~BR_REP_001" # by BR ID
```

## BR Coverage Report Template

```markdown
| BR ID | Test Name | Type | Status |
|-------|-----------|------|--------|
| BR-REP-001 | SubmitReport_NoPhoto_...BR_REP_001 | Unit | ✅ |
```

## Failure Triage Template

```markdown
### Failed Test
- **Name:** `TestName`
- **Error:** `error message`
### Root Cause
- **Category:** Code Bug / Test Bug / Environment / Flaky
- **Related BR:** BR-XXX-NNN
### Fix
- **Action:** description
- **Verified:** ✅/❌
```

## Quality Checklist

- [ ] AAA pattern used consistently
- [ ] Test names include BR IDs
- [ ] Mock only boundaries, never DbContext
- [ ] Integration tests use Testcontainers
- [ ] Respawn for schema reset
- [ ] `dotnet test` passes with 0 failures
- [ ] Every BR has ≥ 1 test
- [ ] Happy path + ≥ 1 error case per endpoint

## Resources

| Resource | Description |
|----------|-------------|
| [testing-patterns.md](resources/testing-patterns.md) | Full test project structure, domain/validator/handler/integration/E2E test templates, response envelope assertions |

## Sources & References

| Source | Description |
|--------|-------------|
| `OVERVIEW.md §7` | Testing strategy, pyramid ratios, rules |
| `00_API_CONVENTIONS.md §12` | Definition of Done checklist |
| [xUnit](https://xunit.net/) | Test framework |
| [FluentAssertions](https://fluentassertions.com/) | Assertion library |
| [NSubstitute](https://nsubstitute.github.io/) | Mocking framework |
| [Testcontainers](https://dotnet.testcontainers.org/) | Docker-based integration tests |
| [Respawn](https://github.com/jbogard/Respawn) | Database reset between tests |

