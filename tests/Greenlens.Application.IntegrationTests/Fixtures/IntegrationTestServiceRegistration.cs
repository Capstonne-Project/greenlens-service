using Greenlens.Application.Common.Behaviors;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Infrastructure.Configuration;
using Greenlens.Infrastructure.DomainEvents;
using Greenlens.Infrastructure.Geo;
using Greenlens.Infrastructure.Notifications;
using Greenlens.Infrastructure.Identity;
using Greenlens.Infrastructure.Persistence;
using Greenlens.Infrastructure.Persistence.Repositories;
using Greenlens.Infrastructure.Persistence.Repositories.Location;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.IntegrationTests.Fixtures;

internal static class IntegrationTestServiceRegistration
{
    public static IServiceCollection AddIntegrationTestServices(
        this IServiceCollection services,
        string connectionString,
        TestCurrentUser currentUser)
    {
        services.AddSingleton(currentUser);
        services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<TestCurrentUser>());
        services.AddScoped<IDateTimeProvider, DateTimeProvider>();

        services.AddDbContext<ApplicationDbContext>((_, options) =>
            options.UseNpgsql(connectionString, o =>
                    o.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IApplicationDbContext>(sp =>
            sp.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPollutionCategoryRepository, PollutionCategoryRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IWasteTagRepository, WasteTagRepository>();
        services.AddScoped<IReportWasteTagRepository, ReportWasteTagRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IEnvironmentalTeamRepository, EnvironmentalTeamRepository>();
        services.AddScoped<IReportAssignmentRepository, ReportAssignmentRepository>();
        services.AddScoped<IEnvironmentalServiceCompanyRepository, EnvironmentalServiceCompanyRepository>();
        services.AddScoped<ICompanyStaffRepository, CompanyStaffRepository>();
        services.AddScoped<IInspectionReportRepository, InspectionReportRepository>();
        services.AddScoped<IViolatingEntityRepository, ViolatingEntityRepository>();
        services.AddScoped<IStaffInvitationRepository, StaffInvitationRepository>();
        services.AddScoped<ITeamMemberRepository, TeamMemberRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IOtpRepository, OtpRepository>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<ILocalOfficeRepository, LocalOfficeRepository>();
        services.AddScoped<IWardBoundaryLookupService, WardBoundaryLookupService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ITransactionManager, TransactionManager>();
        services.AddScoped<IDomainEventCollector, DomainEventCollector>();
        services.AddScoped<INotificationDispatchCollector, NotificationDispatchCollector>();
        services.AddScoped<ISystemSettingsCacheInvalidationCollector, SystemSettingsCacheInvalidationCollector>();
        services.AddSingleton<ISystemSettingsCacheInvalidator, NoOpSystemSettingsCacheInvalidator>();
        services.AddSingleton<INotificationDispatchScheduler, NoOpNotificationDispatchScheduler>();
        services.AddScoped<IChangeTrackerCleaner, ChangeTrackerCleaner>();
        services.AddSingleton<IAuditLogger, NoOpAuditLogger>();
        services.AddSingleton<INotificationService, NoOpNotificationService>();
        services.AddSingleton<IAuthEmailScheduler, NoOpAuthEmailScheduler>();

        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(Application.Common.Errors).Assembly);
            cfg.NotificationPublisherType = typeof(IsolatingNotificationPublisher);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        });

        return services;
    }
}
