using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.Seeders.Administrator;

internal static class SystemSettingsSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, ILogger logger, CancellationToken ct = default)
    {
        var existing = await db.Set<SystemSetting>()
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var byKey = existing.ToDictionary(s => (s.Module, s.Key));
        var catalogByKey = SystemSettingDefinitions.All
            .ToDictionary(d => (d.Module, d.Key));

        var retired = existing
            .Where(s => SystemSettingDefinitions.RetiredKeys.Contains((s.Module, s.Key)))
            .ToList();

        string? migratedImageSizeMb = null;
        if (byKey.TryGetValue((SystemSettingModule.Reports, "max_image_size_bytes"), out var legacyBytes)
            && int.TryParse(legacyBytes.Value, out var bytes)
            && bytes > 0)
        {
            migratedImageSizeMb = Math.Clamp((int)Math.Round(bytes / (1024.0 * 1024.0)), 1, 50).ToString();
        }

        if (retired.Count > 0)
        {
            db.Set<SystemSetting>().RemoveRange(retired);
            foreach (var row in retired)
                byKey.Remove((row.Module, row.Key));

            logger.LogInformation("Removed {Count} retired system setting(s).", retired.Count);
        }

        var inserted = 0;
        foreach (var def in SystemSettingDefinitions.All)
        {
            if (byKey.ContainsKey((def.Module, def.Key)))
                continue;

            var defaultValue = def.DefaultValue;
            if (def.Key == "max_image_size_mb" && migratedImageSizeMb is not null)
                defaultValue = migratedImageSizeMb;

            var setting = SystemSetting.Create(
                def.Module,
                def.Key,
                def.ValueType,
                defaultValue,
                def.Title,
                def.Description,
                def.Unit,
                def.MinValue,
                def.MaxValue);

            db.Set<SystemSetting>().Add(setting);
            byKey[(def.Module, def.Key)] = setting;
            inserted++;
        }

        var metadataUpdated = 0;
        var boundsUpdated = 0;
        foreach (var def in SystemSettingDefinitions.All)
        {
            if (!byKey.TryGetValue((def.Module, def.Key), out var setting))
                continue;

            var beforeTitle = setting.Title;
            var beforeDescription = setting.Description;
            var beforeUnit = setting.Unit;
            setting.UpdateMetadata(def.Title, def.Description, def.Unit);

            if (!string.Equals(beforeTitle, setting.Title, StringComparison.Ordinal)
                || !string.Equals(beforeDescription, setting.Description, StringComparison.Ordinal)
                || !string.Equals(beforeUnit, setting.Unit, StringComparison.Ordinal))
            {
                metadataUpdated++;
            }

            var beforeMin = setting.MinValue;
            var beforeMax = setting.MaxValue;
            setting.UpdateBounds(def.MinValue, def.MaxValue);

            if (beforeMin != setting.MinValue || beforeMax != setting.MaxValue)
                boundsUpdated++;
        }

        if (inserted == 0 && retired.Count == 0 && metadataUpdated == 0 && boundsUpdated == 0)
        {
            logger.LogDebug("System settings up to date.");
            return;
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "System settings sync: inserted={Inserted}, removed={Removed}, metadataUpdated={MetadataUpdated}, boundsUpdated={BoundsUpdated}.",
            inserted,
            retired.Count,
            metadataUpdated,
            boundsUpdated);
    }
}
