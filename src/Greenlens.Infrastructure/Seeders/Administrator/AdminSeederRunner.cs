using Greenlens.Infrastructure.Persistence;
using Greenlens.Infrastructure.Persistence.Seeders.Location;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.Seeders.Administrator;

/// <summary>
/// Apply pending EF Core migrations and seed initial data. Use in Development only.
/// </summary>
public static class AdminSeederRunner
{
    public static async Task MigrateDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("AdminSeeder");

        await db.Database.MigrateAsync().ConfigureAwait(false);

        // Administrative catalog (~regions/units/provinces/wards) MUST run before
        // departments/offices which have FK to provinces.province_code.
        await scope.ServiceProvider.SeedLocationAsync().ConfigureAwait(false);

        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var r2PublicUrl = config["R2:PublicUrl"];
        await AdminSeeder.SeedAsync(db, logger, r2PublicUrl).ConfigureAwait(false);
    }
}
