using Greenlens.Domain.Entities;
using Greenlens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.Seeders.Administrator;

internal static class SystemSettingsSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, ILogger logger, CancellationToken ct = default)
    {
        var existingKeys = await db.Set<SystemSetting>()
            .AsNoTracking()
            .Select(s => new { s.Module, s.Key })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var existingSet = existingKeys
            .Select(k => (k.Module, k.Key))
            .ToHashSet();

        var toInsert = SystemSettingDefinitions.All
            .Where(d => !existingSet.Contains((d.Module, d.Key)))
            .Select(d => SystemSetting.Create(
                d.Module,
                d.Key,
                d.ValueType,
                d.DefaultValue,
                d.Description,
                d.MinValue,
                d.MaxValue))
            .ToList();

        if (toInsert.Count == 0)
        {
            logger.LogDebug("System settings up to date — no new keys to seed.");
            return;
        }

        db.Set<SystemSetting>().AddRange(toInsert);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Seeded {Count} system setting(s).", toInsert.Count);
    }
}
