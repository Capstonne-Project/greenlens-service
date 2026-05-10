using System.Text;
using FluentValidation;
using Greenlens.Application.Common.Behaviors;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Infrastructure.Email;
using Greenlens.Infrastructure.Identity;
using Greenlens.Infrastructure.Persistence;
using Greenlens.Infrastructure.Persistence.Repositories;
using Greenlens.Infrastructure.Persistence.Repositories.Location;
using Greenlens.Infrastructure.Persistence.Seeders.Location;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

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
                o => o.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IOtpRepository, OtpRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ── Identity & Auth ──────────────────────────────
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();

        // ── Email ────────────────────────────────────────
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        // ── MediatR ──────────────────────────────────────
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(
                typeof(Greenlens.Application.Common.Errors).Assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        });

        // ── FluentValidation ─────────────────────────────
        services.AddValidatorsFromAssembly(
            typeof(Greenlens.Application.Common.Errors).Assembly);

        // ── Options ──────────────────────────────────────
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection("Jwt"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SmtpOptions>()
            .Bind(configuration.GetSection("Smtp"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // ── Map Migrations ────────────────────────────────
        services.AddScoped<IAdministrativeRegionRepository, AdministrativeRegionRepository>();
        services.AddScoped<IAdministrativeUnitRepository, AdministrativeUnitRepository>();
        services.AddScoped<IProvinceRepository, ProvinceRepository>();
        services.AddScoped<IWardRepository, WardRepository>();

        // ── JWT Authentication ───────────────────────────
        var jwtSection = configuration.GetSection("Jwt");
        var secret = jwtSection["Secret"]!;


        

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSection["Issuer"],
                ValidAudience = jwtSection["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorization();

        return services;
    }

    /// <summary>
    /// Apply pending EF Core migrations and seed initial data. Use in Development only.
    /// </summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>()
            .CreateLogger("DatabaseSeeder");

        await db.Database.MigrateAsync().ConfigureAwait(false);
        await DatabaseSeeder.SeedAsync(db, logger).ConfigureAwait(false);

        // Administrative catalog (~regions/units/provinces/wards): DbContext is scoped — use same scope.
        await scope.ServiceProvider.SeedLocationAsync().ConfigureAwait(false);
    }
}
