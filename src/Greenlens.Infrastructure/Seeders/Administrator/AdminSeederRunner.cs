using Greenlens.Infrastructure.Persistence;
using Greenlens.Infrastructure.Persistence.Seeders.Location;
using Microsoft.EntityFrameworkCore;
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
        await AdminSeeder.SeedAsync(db, logger).ConfigureAwait(false);

        // Administrative catalog (~regions/units/provinces/wards): DbContext is scoped — use same scope.
        await scope.ServiceProvider.SeedLocationAsync().ConfigureAwait(false);
    }
}
