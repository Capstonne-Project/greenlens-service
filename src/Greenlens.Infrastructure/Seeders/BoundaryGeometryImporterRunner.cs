using Greenlens.Infrastructure.Persistence;
using Greenlens.Infrastructure.Persistence.Seeders.Location;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.Seeders;

/// <summary>Runs the one-time boundary geometry import without starting the full API host.</summary>
public static class BoundaryGeometryImporterRunner
{
    public static async Task RunAsync(
        string provinceGeoJsonPath,
        string wardGeoJsonPath,
        string? connectionString = null,
        CancellationToken ct = default)
    {
        var apiConfigDir = ResolveApiConfigDirectory();
        var userSecretsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Microsoft", "UserSecrets", "90242cfd-3c31-4402-b7d6-8a73ff78c1dd", "secrets.json");

        var config = new ConfigurationBuilder()
            .SetBasePath(apiConfigDir)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddJsonFile(userSecretsPath, optional: true)
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
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("BoundaryGeometryImporter");

        await db.Database.MigrateAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Importing province boundaries from {Path}", provinceGeoJsonPath);
        await BoundaryGeometryImporter.ImportProvincesAsync(db, provinceGeoJsonPath, logger, ct)
            .ConfigureAwait(false);

        logger.LogInformation("Importing ward boundaries from {Path}", wardGeoJsonPath);
        await BoundaryGeometryImporter.ImportWardsAsync(db, wardGeoJsonPath, logger, ct)
            .ConfigureAwait(false);
    }

    private static string ResolveApiConfigDirectory()
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
