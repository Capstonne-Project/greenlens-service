using Greenlens.Infrastructure.Persistence;
using Greenlens.Infrastructure.Seeders;
using Greenlens.Infrastructure.Seeders.Administrator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.Seeders;

/// <summary>Runs gamification catalog seed without starting the full API host.</summary>
public static class GamificationCatalogSeederRunner
{
    public static async Task RunAsync(string? connectionString = null, CancellationToken ct = default)
    {
        RepoDotEnvLoader.LoadIfPresent();

        var apiConfigDir = ResolveApiConfigDirectoryPublic();
        var config = new ConfigurationBuilder()
            .SetBasePath(apiConfigDir)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        connectionString ??= config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, o =>
                    o.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
                .UseSnakeCaseNamingConvention());

        await using var provider = services.BuildServiceProvider();
        var db = provider.GetRequiredService<ApplicationDbContext>();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("GamificationCatalogSeeder");

        await db.Database.MigrateAsync(ct).ConfigureAwait(false);

        var r2PublicUrl = config["R2:PublicUrl"];
        await GamificationSeeder.SeedAsync(db, logger, ct, r2PublicUrl).ConfigureAwait(false);
        await NotificationTemplateSeeder.SeedAsync(db, logger).ConfigureAwait(false);

        var badgeCount = await db.Badges.CountAsync(ct).ConfigureAwait(false);
        var templateCount = await db.Set<Domain.Entities.NotificationTemplate>()
            .CountAsync(t => t.Type == Domain.Enums.NotificationType.BadgeEarned
                             || t.Type == Domain.Enums.NotificationType.LevelUp, ct)
            .ConfigureAwait(false);

        logger.LogInformation(
            "Gamification catalog ready: {BadgeCount} badges, {TemplateCount} gamification notification template(s).",
            badgeCount, templateCount);
    }

    private static string ResolveApiConfigDirectory() =>
        ResolveApiConfigDirectoryPublic();

    internal static string ResolveApiConfigDirectoryPublic()
    {
        var candidates = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "src", "Greenlens.Api"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "Greenlens.Api")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Greenlens.Api")),
        };

        foreach (var path in candidates)
        {
            if (Directory.Exists(path))
                return path;
        }

        throw new InvalidOperationException(
            "Cannot find Greenlens.Api config folder. Run from repo root or tools/Greenlens.DbSeed.");
    }
}
