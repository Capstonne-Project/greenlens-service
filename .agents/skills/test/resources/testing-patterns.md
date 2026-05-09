# Testing Patterns — xUnit + FluentAssertions + NSubstitute

> **Source:** CLAUDE.md §7 — Testing Strategy

## Test Project Structure

```
tests/
├── Greenlens.Domain.UnitTests/
│   ├── Entities/
│   │   ├── ReportTests.cs          # State machine tests
│   │   └── UserTests.cs
│   ├── ValueObjects/
│   │   └── GeoLocationTests.cs
│   └── _Imports.cs                 # Global usings
│
├── Greenlens.Application.UnitTests/
│   ├── Features/
│   │   ├── Reports/
│   │   │   ├── SubmitReportCommandValidatorTests.cs
│   │   │   └── SubmitReportCommandHandlerTests.cs
│   │   └── Auth/
│   │       └── LoginCommandHandlerTests.cs
│   ├── Behaviors/
│   │   └── ValidationBehaviorTests.cs
│   └── _Imports.cs
│
├── Greenlens.Application.IntegrationTests/
│   ├── Common/
│   │   ├── IntegrationTestBase.cs   # Shared Testcontainer setup
│   │   └── TestDatabaseFixture.cs
│   ├── Features/
│   │   └── Reports/
│   │       └── ReportQueryIntegrationTests.cs
│   └── _Imports.cs
│
└── Greenlens.Api.FunctionalTests/
    ├── Common/
    │   ├── CustomWebApplicationFactory.cs
    │   └── TestTokens.cs
    ├── Endpoints/
    │   ├── ReportsEndpointTests.cs
    │   └── AuthEndpointTests.cs
    └── _Imports.cs
```

## Global Usings

```csharp
// _Imports.cs (in each test project)
global using Xunit;
global using FluentAssertions;
global using NSubstitute;
global using Greenlens.Domain.Common;
global using Greenlens.Domain.Entities;
global using Greenlens.Domain.Enums;
```

## Unit Test Patterns

### Domain Entity — State Machine

```csharp
public sealed class ReportTests
{
    private static Report CreateSubmittedReport() => Report.Create(
        Guid.NewGuid(),
        new GeoLocation(10.7626, 106.6602),
        PollutionType.Trash,
        "Test pollution report");

    [Fact]
    public void Verify_WhenSubmitted_TransitionsToVerified_BR_REP_020()
    {
        // Arrange
        var report = CreateSubmittedReport();
        var officerId = Guid.NewGuid();

        // Act
        report.Verify(officerId);

        // Assert
        report.Status.Should().Be(ReportStatus.Verified);
        report.VerifiedBy.Should().Be(officerId);
    }

    [Fact]
    public void Verify_WhenAlreadyVerified_ThrowsDomainException_BR_REP_021()
    {
        // Arrange
        var report = CreateSubmittedReport();
        report.Verify(Guid.NewGuid());

        // Act
        var act = () => report.Verify(Guid.NewGuid());

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Invalid state transition*");
    }

    [Fact]
    public void Verify_RaisesDomainEvent_BR_REP_020()
    {
        // Arrange
        var report = CreateSubmittedReport();

        // Act
        report.Verify(Guid.NewGuid());

        // Assert
        report.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ReportVerifiedEvent>();
    }

    [Theory]
    [InlineData(ReportStatus.InProgress)]
    [InlineData(ReportStatus.Resolved)]
    [InlineData(ReportStatus.Closed)]
    [InlineData(ReportStatus.Rejected)]
    public void Verify_FromInvalidState_Throws_BR_REP_021(ReportStatus invalidState)
    {
        // Arrange
        var report = CreateReportInState(invalidState);

        // Act & Assert
        var act = () => report.Verify(Guid.NewGuid());
        act.Should().Throw<DomainException>();
    }
}
```

### Value Object Tests

```csharp
public sealed class GeoLocationTests
{
    [Theory]
    [InlineData(10.7626, 106.6602, true)]   // Ho Chi Minh City
    [InlineData(21.0285, 105.8542, true)]   // Hanoi
    [InlineData(50.0, 120.0, false)]        // Outside Vietnam
    [InlineData(7.9, 106.0, false)]         // Below lat range
    public void IsWithinVietnam_ReturnsExpected_BR_REP_003(
        double lat, double lng, bool expected)
    {
        var location = new GeoLocation(lat, lng);
        location.IsWithinVietnam().Should().Be(expected);
    }
}
```

### FluentValidation Tests

```csharp
public sealed class SubmitReportCommandValidatorTests
{
    private readonly SubmitReportCommandValidator _sut = new();

    [Fact]
    public async Task Validate_ValidCommand_Passes()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_NoPhotos_Fails_BR_REP_001()
    {
        var cmd = CreateValidCommand() with { PhotoIds = [] };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PhotoIds");
    }

    [Fact]
    public async Task Validate_GpsOutOfVietnam_Fails_BR_REP_003()
    {
        var cmd = CreateValidCommand() with { Lat = 50.0, Lng = 120.0 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Lat");
    }

    [Fact]
    public async Task Validate_TooManyPhotos_Fails_BR_REP_002()
    {
        var cmd = CreateValidCommand() with
        {
            PhotoIds = Enumerable.Range(0, 6).Select(_ => Guid.NewGuid()).ToList()
        };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }
}
```

### Handler Tests with NSubstitute

```csharp
public sealed class SubmitReportCommandHandlerTests
{
    private readonly IApplicationDbContext _db = Substitute.For<IApplicationDbContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ICacheService _cache = Substitute.For<ICacheService>();
    private readonly SubmitReportCommandHandler _sut;

    public SubmitReportCommandHandlerTests()
    {
        _currentUser.UserId.Returns(Guid.NewGuid());
        _currentUser.Role.Returns("Citizen");

        var mockDbSet = Substitute.For<DbSet<Report>>();
        _db.Reports.Returns(mockDbSet);

        _sut = new SubmitReportCommandHandler(_db, _currentUser, _cache);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessWithId_BR_REP_013()
    {
        // Arrange
        var command = CreateValidCommand();
        _cache.GetAsync<bool>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((bool?)null);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        _db.Reports.Received(1).Add(Arg.Any<Report>());
        await _db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
```

## Integration Test Pattern

```csharp
public sealed class ReportQueryIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgis/postgis:18-3.4")
        .Build();

    private ApplicationDbContext _db = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgres.GetConnectionString(),
                o => o.UseNetTopologySuite())
            .UseSnakeCaseNamingConvention()
            .Options;
        _db = new ApplicationDbContext(options);
        await _db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task FindNearby_ReturnsReportsWithinRadius_BR_MAP_004()
    {
        // Arrange — seed data at known coordinates
        var report = Report.Create(Guid.NewGuid(),
            new GeoLocation(10.7626, 106.6602), PollutionType.Trash, "Near");
        _db.Reports.Add(report);
        await _db.SaveChangesAsync();

        // Act
        var results = await _db.Reports
            .AsNoTracking()
            .Where(r => r.GeoPoint.IsWithinDistance(
                new Point(106.6602, 10.7626) { SRID = 4326 }, 1000))
            .ToListAsync();

        // Assert
        results.Should().ContainSingle();
    }
}
```

## Functional Test Pattern

```csharp
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgis/postgis:18-3.4")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace DbContext with Testcontainers
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString(),
                    o => o.UseNetTopologySuite())
                .UseSnakeCaseNamingConvention());

            // Replace external services with fakes
            services.RemoveAll<IFileStorage>();
            services.AddSingleton<IFileStorage, FakeFileStorage>();

            services.RemoveAll<IAiClassificationService>();
            services.AddSingleton<IAiClassificationService, FakeAiService>();
        });
    }

    public async Task InitializeAsync() => await _postgres.StartAsync();
    public async Task DisposeAsync() => await _postgres.DisposeAsync();
}
```

## Response Envelope Assertions

```csharp
// Helper extension for testing API responses
public static class HttpResponseAssertions
{
    public static async Task<ApiResponse<T>> ShouldBeSuccess<T>(
        this HttpResponseMessage response, int expectedStatus = 200)
    {
        response.StatusCode.Should().Be((HttpStatusCode)expectedStatus);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
        body.Should().NotBeNull();
        body!.Code.Should().Be("SUCCESS");
        body.Status.Should().Be(expectedStatus);
        return body;
    }

    public static async Task ShouldBeError(
        this HttpResponseMessage response,
        string expectedCode, int expectedStatus)
    {
        response.StatusCode.Should().Be((HttpStatusCode)expectedStatus);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<object>>();
        body!.Code.Should().Be(expectedCode);
        body.Status.Should().Be(expectedStatus);
        body.Data.Should().BeNull();
    }
}
```
