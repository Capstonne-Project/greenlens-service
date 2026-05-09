# Dependency Injection Patterns — GreenLens

> **Source:** OVERVIEW.md §3, §4.4, §4.8, §4.12 (v1.2)

## Core Principles

1. **Register in Infrastructure** → `DependencyInjection.cs` is the single registration point.
2. **Resolve in Api** → `Program.cs` calls `services.AddInfrastructure(configuration)`.
3. **Depend on abstractions** → Application defines interfaces, Infrastructure implements.

## Registration File Pattern

```csharp
// Infrastructure/DependencyInjection.cs
namespace Greenlens.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Persistence (Strict Repo §4.12) ──────────────
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                o =>
                {
                    o.UseNetTopologySuite();
                    o.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                })
            .UseSnakeCaseNamingConvention());

        // NO IApplicationDbContext — DbContext is internal to Infrastructure
        // Register each repository individually
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IReportMediaRepository, ReportMediaRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<IBadgeRepository, BadgeRepository>();
        services.AddScoped<ICleanupTaskRepository, CleanupTaskRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ❌ NEVER register open generic:
        // services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

        // ── Identity & Auth ──────────────────────────────
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddSingleton<IJwtService, JwtService>();

        // ── Storage (Cloudflare R2) ──────────────────────
        services.AddSingleton<IFileStorage, R2FileStorage>();

        // ── Caching ──────────────────────────────────────
        services.AddStackExchangeRedisCache(options =>
            options.Configuration = configuration.GetConnectionString("Redis"));
        services.AddSingleton<ICacheService, RedisCacheService>();

        // ── External Services ────────────────────────────
        services.AddHttpClient<IAiClassificationService, AiClassificationService>();
        services.AddScoped<IEmailSender, EmailSender>();
        services.AddScoped<IPushNotifier, PushNotifier>();

        // ── Background Jobs ──────────────────────────────
        services.AddHangfire(config =>
            config.UsePostgreSqlStorage(
                configuration.GetConnectionString("DefaultConnection")));
        services.AddHangfireServer();

        // ── MediatR ──────────────────────────────────────
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(
                typeof(Greenlens.Application.AssemblyMarker).Assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
        });

        // ── FluentValidation ─────────────────────────────
        services.AddValidatorsFromAssembly(
            typeof(Greenlens.Application.AssemblyMarker).Assembly);

        // ── Options ──────────────────────────────────────
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection("Jwt"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<R2Options>()
            .Bind(configuration.GetSection("Cloudflare:R2"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<TurnstileOptions>()
            .Bind(configuration.GetSection("Cloudflare:Turnstile"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}
```

## Lifetime Guidelines

| Lifetime | When to use | GreenLens examples |
|----------|------------|-------------------|
| **Scoped** | Per-request state, DbContext, user context | `IXxxRepository`, `IUnitOfWork`, `ICurrentUser`, `IEmailSender` |
| **Singleton** | Stateless, thread-safe, expensive to create | `IJwtService`, `IFileStorage`, `ICacheService` |
| **Transient** | Lightweight, no shared state | Validators, simple factories |

## DO / DON'T

```csharp
// ✅ DO — Define interface in Application
// Application/Common/Interfaces/ICurrentUser.cs
public interface ICurrentUser
{
    Guid UserId { get; }
    string Role { get; }
    bool IsAuthenticated { get; }
}

// ✅ DO — Implement in Infrastructure
// Infrastructure/Identity/CurrentUser.cs
public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid UserId => Guid.Parse(
        accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());
    public string Role =>
        accessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    public bool IsAuthenticated =>
        accessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}

// ❌ DON'T — Import IHttpContextAccessor in Application layer
// Application/Features/Reports/SubmitReport/SubmitReportCommandHandler.cs
using Microsoft.AspNetCore.Http; // ❌ VIOLATION — never in Application
```

## Options Pattern (Validated)

```csharp
// ✅ Always validate options at startup
public sealed class JwtOptions
{
    [Required] public string Secret { get; init; } = default!;
    [Required] public string Issuer { get; init; } = default!;
    [Required] public string Audience { get; init; } = default!;
    [Range(1, 168)] public int AccessTokenExpirationHours { get; init; } = 24;
    [Range(1, 90)] public int RefreshTokenExpirationDays { get; init; } = 30;
}

// Registration
services.AddOptions<JwtOptions>()
    .Bind(configuration.GetSection("Jwt"))
    .ValidateDataAnnotations()
    .ValidateOnStart();  // Fail fast at startup

// Injection — use IOptions<T> for singleton, IOptionsSnapshot<T> for scoped
public sealed class JwtService(IOptions<JwtOptions> options) : IJwtService
{
    private readonly JwtOptions _jwt = options.Value;
}
```

## Controller DI Pattern

```csharp
// ✅ Controllers depend ONLY on ISender (MediatR)
[ApiController]
[Route("v1/[controller]")]
public sealed class ReportsController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> SubmitAsync(
        [FromBody] SubmitReportCommand cmd,
        CancellationToken ct)
        => (await sender.Send(cmd, ct)).ToHttp();
}

// ❌ DON'T inject repositories or services directly into controllers
public sealed class ReportsController(IReportRepository repo) : ControllerBase // ❌
```

## Common Pitfall: Captive Dependency

```csharp
// ❌ Singleton capturing Scoped — MEMORY LEAK
services.AddSingleton<ISomeService, SomeService>(); // Singleton
// SomeService depends on IReportRepository (Scoped) → CAPTIVE!

// ✅ Fix: Use IServiceScopeFactory
public sealed class SomeService(IServiceScopeFactory scopeFactory)
{
    public async Task DoWorkAsync()
    {
        using var scope = scopeFactory.CreateScope();
        var reports = scope.ServiceProvider.GetRequiredService<IReportRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        // use reports + uow...
    }
}
```
