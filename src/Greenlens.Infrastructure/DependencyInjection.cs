using System.Text;
using FluentValidation;
using Greenlens.Application.Common.Behaviors;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Infrastructure.Email;
using Greenlens.Infrastructure.Identity;
using Greenlens.Infrastructure.Options;
using Greenlens.Infrastructure.Persistence;
using Greenlens.Infrastructure.Persistence.Repositories;
using Greenlens.Infrastructure.Persistence.Repositories.Location;

using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
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
            .UseSnakeCaseNamingConvention()
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning)));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IOtpRepository, OtpRepository>();
        services.AddScoped<IPollutionCategoryRepository, PollutionCategoryRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IReportMediaRepository, ReportMediaRepository>();
        services.AddScoped<IReportStatusHistoryRepository, ReportStatusHistoryRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ── Identity & Auth ──────────────────────────────
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();

        // ── Email ────────────────────────────────────────
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        // ── SMS (SpeedSMS) ───────────────────────────────
        services.AddHttpClient<ISmsSender, Services.SpeedSmsSender>();

        // ── File Storage (R2 Cloudflare) ────────────────
        services.AddSingleton<IFileStorageService, Storage.R2FileStorageService>();

        // ── MediatR ──────────────────────────────────────
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(
                typeof(Application.Common.Errors).Assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        });

        // ── FluentValidation ─────────────────────────────
        services.AddValidatorsFromAssembly(
            typeof(Application.Common.Errors).Assembly);

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
        services.AddOptions<Storage.R2Options>()
            .Bind(configuration.GetSection("R2"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SpeedSmsOptions>()
            .Bind(configuration.GetSection(SpeedSmsOptions.Section));

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

            options.Events = new JwtBearerEvents
            {
                // 401 Unauthorized — no token or invalid token
                OnChallenge = async context =>
                {
                    context.HandleResponse(); // suppress default behavior
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                    var json = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        code = "UNAUTHORIZED",
                        message = "Bạn chưa đăng nhập hoặc token không hợp lệ.",
                        status = 401,
                        data = (object?)null
                    });
                    await context.Response.WriteAsync(json);
                },
                // 403 Forbidden — authenticated but wrong role
                OnForbidden = async context =>
                {
                    context.Response.StatusCode = 403;
                    context.Response.ContentType = "application/json";
                    var json = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        code = "FORBIDDEN",
                        message = "Bạn không có quyền truy cập tài nguyên này.",
                        status = 403,
                        data = (object?)null
                    });
                    await context.Response.WriteAsync(json);
                }
            };
        });

        services.AddAuthorization();

        return services;
    }
}
