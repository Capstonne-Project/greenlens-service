using System.Text;
using FirebaseAdmin;
using FluentValidation;
using Google.Apis.Auth.OAuth2;
using Greenlens.Application.Common.Behaviors;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Infrastructure.Ai;
using Greenlens.Infrastructure.Audit;
using Greenlens.Infrastructure.Email;
using Greenlens.Infrastructure.Identity;
using Greenlens.Infrastructure.Persistence;
using Greenlens.Infrastructure.Persistence.Repositories;
using Greenlens.Infrastructure.Persistence.Repositories.Location;
using Greenlens.Infrastructure.BackgroundJobs;
using Greenlens.Infrastructure.Configuration;
using Greenlens.Infrastructure.DomainEvents;
using Greenlens.Infrastructure.Moderation;
using Greenlens.Application.Features.Notifications;
using Greenlens.Infrastructure.Notifications;
using Greenlens.Infrastructure.Options;
using Greenlens.Infrastructure.Services;
using Hangfire;
using Hangfire.PostgreSql;
using StackExchange.Redis;

using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
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
                Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning,
                Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

        services.AddScoped<IApplicationDbContext>(
            sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IOtpRepository, OtpRepository>();
        services.AddScoped<IPollutionCategoryRepository, PollutionCategoryRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IReportMediaRepository, ReportMediaRepository>();
        services.AddScoped<IReportStatusHistoryRepository, ReportStatusHistoryRepository>();
        services.AddScoped<IWasteTagRepository, WasteTagRepository>();
        services.AddScoped<IReportWasteTagRepository, ReportWasteTagRepository>();
        services.AddScoped<ITeamWasteTagRepository, TeamWasteTagRepository>();
        services.AddScoped<IReportDraftRepository, ReportDraftRepository>();
        services.AddScoped<IReportSatisfactionRepository, ReportSatisfactionRepository>();

        // ── Organization module (v1.1) ──
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<ILocalOfficeRepository, LocalOfficeRepository>();
        services.AddScoped<IEnvironmentalTeamRepository, EnvironmentalTeamRepository>();
        services.AddScoped<ITeamMemberRepository, TeamMemberRepository>();
        services.AddScoped<IReportAssignmentRepository, ReportAssignmentRepository>();
        services.AddScoped<IAssignmentProgressUpdateRepository, AssignmentProgressUpdateRepository>();
        services.AddScoped<ICommunityCleanupEventRepository, CommunityCleanupEventRepository>();
        services.AddScoped<ICommunityCleanupParticipantRepository, CommunityCleanupParticipantRepository>();

        // ── Company module (v1.3) ──
        services.AddScoped<IEnvironmentalServiceCompanyRepository, EnvironmentalServiceCompanyRepository>();
        services.AddScoped<ICompanyStaffRepository, CompanyStaffRepository>();
        services.AddScoped<ICompanyServiceAreaRepository, CompanyServiceAreaRepository>();

        // ── Inspection module (v3.0) ──
        services.AddScoped<IInspectionReportRepository, InspectionReportRepository>();
        services.AddScoped<IInspectionEvidenceRepository, InspectionEvidenceRepository>();
        services.AddScoped<IViolatingEntityRepository, ViolatingEntityRepository>();

        // ── Gamification module (v1.2) ──
        services.AddScoped<IUserPointsRepository, UserPointsRepository>();
        services.AddScoped<IBadgeRepository, BadgeRepository>();
        services.AddScoped<IUserBadgeRepository, UserBadgeRepository>();

        // ── Administration module (BR-ADM-*) ──
        services.AddScoped<IPenaltyFrameworkRepository, PenaltyFrameworkRepository>();
        services.AddScoped<IGamificationConfigRepository, GamificationConfigRepository>();
        services.AddScoped<IBlockedWordRepository, BlockedWordRepository>();
        services.AddScoped<INotificationTemplateRepository, NotificationTemplateRepository>();

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
        services.AddScoped<INotificationDispatchCollector, NotificationDispatchCollector>();
        services.AddScoped<ISystemSettingsCacheInvalidationCollector, SystemSettingsCacheInvalidationCollector>();
        services.AddScoped<IChangeTrackerCleaner, ChangeTrackerCleaner>();

        // ── Identity & Auth ──────────────────────────────
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();

        // ── Email ────────────────────────────────────────────
        services.AddOptions<SmtpOptions>()
            .Bind(configuration.GetSection("Smtp"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var smtpEnabled = configuration.GetValue("Smtp:Enabled", true);
        if (smtpEnabled)
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        else
            services.AddScoped<IEmailSender, NoOpEmailSender>();

        // ── Notifications (BR-NTF-001..004) ───────────────
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<INotificationDispatchScheduler, NotificationDispatchScheduler>();
        services.AddScoped<IAuthEmailScheduler, AuthEmailScheduler>();
        services.AddScoped<IPushNotificationSender, FcmPushNotificationSender>();
        services.AddScoped<IOfficerRecipientQuery, OfficerRecipientQuery>();
        services.AddScoped<ITeamMemberRecipientQuery, TeamMemberRecipientQuery>();
        services.AddScoped<ICleanupTaskAssignedNotifier, CleanupTaskAssignedNotifier>();
        services.AddScoped<ICompanyTeamAssignedLeoNotifier, CompanyTeamAssignedLeoNotifier>();
        services.AddScoped<ICleanupAssignmentActivityNotifier, CleanupAssignmentActivityNotifier>();
        services.AddScoped<IReportClosedByCitizenNotifier, ReportClosedByCitizenNotifier>();
        services.AddScoped<IInspectionTaskAssignedNotifier, InspectionTaskAssignedNotifier>();
        services.AddScoped<IInspectionAssignmentActivityNotifier, InspectionAssignmentActivityNotifier>();
        services.AddScoped<IInspectionTaskDeclinedNotifier, InspectionTaskDeclinedNotifier>();
        services.AddScoped<IInspectionClosedNoViolationNotifier, InspectionClosedNoViolationNotifier>();
        services.AddScoped<ICompanyManagerRecipientQuery, CompanyManagerRecipientQuery>();

        // ── Firebase Phone Auth ──────────────────────────
        services.AddScoped<IFirebasePhoneAuthService, FirebasePhoneAuthService>();

        // ── File Storage (R2 Cloudflare) ────────────────
        services.AddScoped<IFileStorageService, Storage.R2FileStorageService>();

        // ── Company Cascade (BR-CMP-013) ────────────────
        services.AddScoped<ICompanyCascadeService, CompanyCascadeService>();

        // ── Video Transcoding (FFmpeg) — BR-REP-002 ──
        services.AddScoped<IVideoTranscoder, Video.FFmpegVideoTranscoder>();

        // ── Geo / PostGIS (BR-CLN-002, BR-INS-004) ──
        services.AddScoped<IGeoDistanceService, Geo.PostGisDistanceService>();
        services.AddScoped<INearbyCitizenQuery, Geo.NearbyCitizenQuery>();

        // ── Geo / PostGIS (BR-ORG-004, BR-ORG-010, BR-ORG-016) ──
        services.AddScoped<IWardBoundaryLookupService, Geo.WardBoundaryLookupService>();

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

        services.AddHttpClient("ImageFetch", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddScoped<IAiClassificationService, AiClassificationService>();
        services.AddScoped<IAiImageCompareService, AiImageCompareService>();

        // ── Gemini (free tier) — auto-draft report description from AI classify result ──
        services.AddOptions<GeminiOptions>()
            .Bind(configuration.GetSection("Gemini"));

        services.AddHttpClient("Gemini", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(25);
        });

        services.AddScoped<IReportDescriptionGenerator, GeminiReportDescriptionGenerator>();
        services.AddSingleton<ITempImageStore, TempImageStore>();
        services.AddSingleton<IImageExifAnalyzer, Imaging.MetadataExtractorImageExifAnalyzer>();
        services.AddScoped<IImageBytesFetcher, Imaging.HttpImageBytesFetcher>();

        // ── Redis + report submit rate limit (BR-REP-010, P0-3) ──
        services.AddOptions<RedisInfrastructureOptions>()
            .Bind(configuration.GetSection(RedisInfrastructureOptions.SectionName))
            .ValidateOnStart();

        var redisOptions = configuration
            .GetSection(RedisInfrastructureOptions.SectionName)
            .Get<RedisInfrastructureOptions>() ?? new RedisInfrastructureOptions();

        var redisConnection = configuration.GetConnectionString("Redis");
        if (redisOptions.Required && string.IsNullOrWhiteSpace(redisConnection))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Redis is required when Redis:Required is true (staging/production). " +
                "Set the connection string via environment variable or secrets manager.");
        }

        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisConnection));
            services.AddSingleton<IReportSubmissionRateLimiter, RateLimiting.RedisReportSubmissionRateLimiter>();
            services.AddSingleton<IIdempotencyStore, Idempotency.RedisIdempotencyStore>();
        }
        else
        {
            services.AddSingleton<IReportSubmissionRateLimiter, RateLimiting.InMemoryReportSubmissionRateLimiter>();
            services.AddSingleton<IIdempotencyStore, Idempotency.InMemoryIdempotencyStore>();
        }

        services.AddScoped<IIdempotencyContext, Idempotency.IdempotencyContext>();

        // ── Comment moderation (BR-CMT-003 phase 1, BR-REP-004) ──
        services.AddSingleton<BlockedWordCache>();
        services.AddSingleton<IBlockedWordCache>(sp => sp.GetRequiredService<BlockedWordCache>());
        services.AddHostedService(sp => sp.GetRequiredService<BlockedWordCache>());

        // ── System settings cache (BR-ADM-010) ──
        // GetService<IConnectionMultiplexer>: null on dev không Redis; inject rõ ràng trên production.
        services.AddSingleton<SystemSettingsProvider>(sp => new SystemSettingsProvider(
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<ILogger<SystemSettingsProvider>>(),
            sp.GetService<IConnectionMultiplexer>()));
        services.AddSingleton<ISystemSettingsProvider>(sp => sp.GetRequiredService<SystemSettingsProvider>());
        services.AddSingleton<ISystemSettingsCache>(sp => sp.GetRequiredService<SystemSettingsProvider>());
        services.AddSingleton<ISystemSettingsCacheInvalidator>(sp => sp.GetRequiredService<SystemSettingsProvider>());
        services.AddHostedService(sp => sp.GetRequiredService<SystemSettingsProvider>());

        services.AddSingleton<IProfanityFilter, ProfanityFilter>();

        // ── Duplicate detection Tier 2 scheduler (BR-REP-030, BR-AI-002) ──
        services.AddScoped<IDuplicateCompareScheduler, DuplicateCompareScheduler>();

        services.AddScoped<Application.Features.Organization.Common.TeamWasteTagService>();

        // ── Audit (BR-ADM-010) ─────────────────────────────
        services.AddScoped<IAuditLogger, AuditLogger>();

        // ── MediatR ──────────────────────────────────────
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(
                typeof(Application.Common.Errors).Assembly);
            cfg.NotificationPublisherType = typeof(IsolatingNotificationPublisher);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(AuditLogBehavior<,>));
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

        // BR-OFF-013: Workload limits
        services.Configure<Application.Common.Options.WorkloadLimitsOptions>(
            configuration.GetSection(Application.Common.Options.WorkloadLimitsOptions.SectionName));

        services.AddOptions<Application.Common.Options.PublicWebOptions>()
            .Bind(configuration.GetSection(Application.Common.Options.PublicWebOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<Application.Common.Options.MetaPageOptions>()
            .Bind(configuration.GetSection(Application.Common.Options.MetaPageOptions.SectionName))
            .Validate(
                options => !options.AutoPostEnabled || options.IsConfigured,
                "Meta PageId and PageAccessToken are required when AutoPostEnabled is true")
            .ValidateOnStart();

        services.AddHttpClient("MetaGraph", client =>
        {
            client.BaseAddress = new Uri("https://graph.facebook.com/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddScoped<IFacebookPagePublisher, Meta.MetaGraphPagePublisher>();

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
        // Đường dẫn tương đối trong config resolve theo current directory, khác nhau tuỳ cách
        // khởi chạy (dotnet run / IIS / container). Thử thêm base directory của assembly để
        // key nằm cạnh binary vẫn tìm thấy.
        var firebaseKeyPath = ResolveFirebaseKeyPath(configuration["Firebase:ServiceAccountKeyPath"]);
        if (firebaseKeyPath is not null && FirebaseApp.DefaultInstance is null)
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
            // Fallback: GOOGLE_APPLICATION_CREDENTIALS env var. Ném lỗi nếu biến này cũng
            // không có — nuốt lỗi ở đây sẽ khiến Google login fail khó hiểu lúc runtime.
            try
            {
                FirebaseApp.Create();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Firebase Admin SDK could not be initialized. Set Firebase:ServiceAccountKeyPath " +
                    "to a readable service account JSON file, or set GOOGLE_APPLICATION_CREDENTIALS. " +
                    "Google login and phone auth require this.", ex);
            }
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
                // SignalR: support token passing in query string
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                },
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

        // BR-REP-016: Auto-close reports Resolved > 2 days
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

        // BR-REP-008 + BR-REP-009: Flag overdue reports, notify unassigned
        RecurringJob.AddOrUpdate<OverdueReportNotificationJob>(
            "overdue-report-notification",
            job => job.ExecuteAsync(),
            "0 * * * *"); // every hour

        // BR-REP-019: Delete stale drafts (> 7 days idle)
        RecurringJob.AddOrUpdate<DraftCleanupJob>(
            "draft-cleanup",
            job => job.ExecuteAsync(),
            "0 3 * * *"); // daily at 03:00 UTC

        // BR-OFF-010: Recalculate priority scores
        RecurringJob.AddOrUpdate<PriorityScoreRefreshJob>(
            "priority-score-refresh",
            job => job.ExecuteAsync(),
            "*/30 * * * *"); // every 30 minutes

        // BR-DAT-002: Data retention — delete expired media files (>2y), audit_logs (>12m), report status history (>12m)
        RecurringJob.AddOrUpdate<DataRetentionJob>(
            "data-retention",
            job => job.ExecuteAsync(),
            "0 4 * * 0"); // weekly Sunday 04:00 UTC

        // BR-CMP-007: Auto-expire Bidding companies + send 30/7/1-day warnings
        RecurringJob.AddOrUpdate<CompanyContractExpiryJob>(
            "company-contract-expiry",
            job => job.ExecuteAsync(),
            "0 2 * * *"); // daily at 02:00 UTC

        // BR-INS-030: Flag inspection reports exceeding SLA deadline
        RecurringJob.AddOrUpdate<SlaBreachInspectionJob>(
            "sla-breach-inspection",
            job => job.ExecuteAsync(),
            "*/30 * * * *"); // every 30 minutes

        // BR-INS-021: Mark penalty payment overdue and notify LEO/DEO
        RecurringJob.AddOrUpdate<PenaltyPaymentOverdueJob>(
            "penalty-payment-overdue",
            job => job.ExecuteAsync(),
            "0 * * * *"); // every hour

        // BR-CLN-004: Flag stale cleanup progress (>24h / >48h)
        RecurringJob.AddOrUpdate<CleanupProgressSlaJob>(
            "cleanup-progress-sla",
            job => job.ExecuteAsync(),
            "0 * * * *"); // every hour

        // Draft BR-CMU-*: Remind Community Cleanup participants ~15 min before StartsAt to check in
        RecurringJob.AddOrUpdate<CommunityCleanupCheckInReminderJob>(
            "community-cleanup-checkin-reminder",
            job => job.ExecuteAsync(),
            "*/5 * * * *"); // every 5 minutes

        // BR-GAM-004: Safety-net recheck of badge eligibility for every gamification user.
        // Backfills badges (e.g. streak_7d/30d) whose progress axis can cross the threshold
        // without a point-awarding event firing to trigger CheckBadgesCommand.
        RecurringJob.AddOrUpdate<BadgeRecheckJob>(
            "badge-recheck",
            job => job.ExecuteAsync(),
            "0 1 * * *"); // daily at 01:00 UTC

        // Classification is an opt-in pre-submit UX feature. Remove the legacy
        // recurring registration from persistent Hangfire storage after rollout.
        RecurringJob.RemoveIfExists("ai-retry");
    }

    /// <summary>
    /// Resolves the Firebase service account key, trying the path as configured and then
    /// relative to the app's base directory. Returns null when no readable file is found.
    /// </summary>
    private static string? ResolveFirebaseKeyPath(string? configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
            return null;

        if (File.Exists(configuredPath))
            return configuredPath;

        if (Path.IsPathRooted(configuredPath))
            return null;

        var nextToBinary = Path.Combine(AppContext.BaseDirectory, configuredPath);
        return File.Exists(nextToBinary) ? nextToBinary : null;
    }
}
