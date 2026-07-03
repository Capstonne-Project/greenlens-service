using System.Text;
using FirebaseAdmin;
using FluentValidation;
using Google.Apis.Auth.OAuth2;
using Greenlens.Application.Common.Behaviors;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Infrastructure.Ai;
using Greenlens.Infrastructure.Email;
using Greenlens.Infrastructure.Identity;
using Greenlens.Infrastructure.Persistence;
using Greenlens.Infrastructure.Persistence.Repositories;
using Greenlens.Infrastructure.Persistence.Repositories.Location;
using Greenlens.Infrastructure.BackgroundJobs;
using Greenlens.Infrastructure.DomainEvents;
using Greenlens.Infrastructure.Notifications;
using Hangfire;
using Hangfire.PostgreSql;

using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
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
        services.AddScoped<IWasteTagRepository, WasteTagRepository>();
        services.AddScoped<IReportWasteTagRepository, ReportWasteTagRepository>();

        // ── Organization module (v1.1) ──
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<ILocalOfficeRepository, LocalOfficeRepository>();
        services.AddScoped<IEnvironmentalTeamRepository, EnvironmentalTeamRepository>();
        services.AddScoped<ITeamMemberRepository, TeamMemberRepository>();
        services.AddScoped<IReportAssignmentRepository, ReportAssignmentRepository>();

        // ── Company module (v1.3) ──
        services.AddScoped<IEnvironmentalServiceCompanyRepository, EnvironmentalServiceCompanyRepository>();
        services.AddScoped<ICompanyStaffRepository, CompanyStaffRepository>();
        services.AddScoped<ICompanyServiceAreaRepository, CompanyServiceAreaRepository>();

        // ── Inspection module (v3.0) ──
        services.AddScoped<IInspectionReportRepository, InspectionReportRepository>();

        // ── Gamification module (v1.2) ──
        services.AddScoped<IUserPointsRepository, UserPointsRepository>();
        services.AddScoped<IBadgeRepository, BadgeRepository>();
        services.AddScoped<IUserBadgeRepository, UserBadgeRepository>();

        // ── Notification module (BR-NTF-001..004) ──
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationPreferenceRepository, NotificationPreferenceRepository>();

        // ── Password History (BR-AUTH-020) ──
        services.AddScoped<IPasswordHistoryRepository, PasswordHistoryRepository>();

        // ── Staff Invitation (BR-ORG-021) ──
        services.AddScoped<IStaffInvitationRepository, StaffInvitationRepository>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ITransactionManager, TransactionManager>();
        services.AddScoped<IDomainEventCollector, DomainEventCollector>();
        services.AddScoped<IChangeTrackerCleaner, ChangeTrackerCleaner>();

        // ── Identity & Auth ──────────────────────────────
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();

        // ── Email ────────────────────────────────────────────
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        // ── Notifications (BR-NTF-001..004) ───────────────
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IPushNotificationSender, FcmPushNotificationSender>();

        // ── Firebase Phone Auth ──────────────────────────
        services.AddScoped<IFirebasePhoneAuthService, FirebasePhoneAuthService>();

        // ── File Storage (R2 Cloudflare) ────────────────
        services.AddSingleton<IFileStorageService, Storage.R2FileStorageService>();

        // ── Video Transcoding (FFmpeg) — BR-REP-002 ──
        services.AddScoped<IVideoTranscoder, Video.FFmpegVideoTranscoder>();

        // ── AI Classification ─────────────────────────
        services.AddOptions<AiOptions>()
            .Bind(configuration.GetSection("Ai"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient("AiService", (sp, client) =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AiOptions>>().Value;
            client.BaseAddress = new Uri(opts.BaseUrl);
        });

        services.AddScoped<IAiClassificationService, AiClassificationService>();
        services.AddSingleton<ITempImageStore, TempImageStore>();

        // ── MediatR ──────────────────────────────────────
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(
                typeof(Application.Common.Errors).Assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
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

        // ── Firebase Admin SDK ──────────────────────────
        var firebaseKeyPath = configuration["Firebase:ServiceAccountKeyPath"];
        if (!string.IsNullOrEmpty(firebaseKeyPath) && File.Exists(firebaseKeyPath) && FirebaseApp.DefaultInstance is null)
        {
            using var stream = File.OpenRead(firebaseKeyPath);
#pragma warning disable CS0618 // GoogleCredential.FromStream is deprecated but Firebase Admin SDK requires it
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromStream(stream),
            });
#pragma warning restore CS0618
        }
        else if (FirebaseApp.DefaultInstance is null)
        {
            // Fallback: use GOOGLE_APPLICATION_CREDENTIALS env var
            FirebaseApp.Create();
        }

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

        // ── Hangfire Background Jobs ───────────────
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(opts =>
                opts.UseNpgsqlConnection(
                    configuration.GetConnectionString("DefaultConnection"))));

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = 2;
            options.Queues = ["default", "gamification"];
        });

        return services;
    }

    /// <summary>Register recurring Hangfire jobs. Call from Program.cs after app.Build().</summary>
    public static void UseRecurringJobs(this IApplicationBuilder _)
    {
        // BR-GAM-005: Leaderboard snapshot daily at 00:05 UTC
        RecurringJob.AddOrUpdate<LeaderboardSnapshotJob>(
            "leaderboard-snapshot",
            job => job.ExecuteAsync(),
            "5 0 * * *"); // 00:05 UTC daily

        // BR-REP-016: Auto-close reports Resolved > 7 days
        RecurringJob.AddOrUpdate<AutoCloseResolvedReportJob>(
            "auto-close-resolved-reports",
            job => job.ExecuteAsync(),
            "0 * * * *"); // every hour

        // BR-OFF-002: Flag SLA verification breach (Submitted > 24h)
        RecurringJob.AddOrUpdate<SlaBreachVerificationJob>(
            "sla-breach-verification",
            job => job.ExecuteAsync(),
            "*/15 * * * *"); // every 15 minutes

        // BR-OFF-020: Flag SLA resolution breach (InProgress > severity deadline)
        RecurringJob.AddOrUpdate<SlaBreachResolutionJob>(
            "sla-breach-resolution",
            job => job.ExecuteAsync(),
            "*/30 * * * *"); // every 30 minutes

        // BR-AUTH-021: Permanently delete accounts soft-deleted > 90 days
        RecurringJob.AddOrUpdate<AccountHardDeleteJob>(
            "account-hard-delete",
            job => job.ExecuteAsync(),
            "0 2 * * *"); // daily at 02:00 UTC
    }
}
