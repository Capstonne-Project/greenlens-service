using Greenlens.Domain.Entities;
using Greenlens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.Seeders;

/// <summary>
/// Seeds the three official pollution categories (BR-REP-005 v1.2).
/// Deactivates removed categories (SMOKE) if they already exist.
/// </summary>
/// <remarks>Implements: BR-REP-005.</remarks>
internal static class PollutionCategorySeeder
{
    private static readonly (string Code, string NameVi, string NameEn)[] DefaultCategories =
    [
        ("TRASH", "Ô nhiễm rác thải", "Trash"),
        ("WASTEWATER", "Ô nhiễm nước", "Water"),
        ("CHEMICAL", "Ô nhiễm hóa chất", "Chemical"),
    ];

    /// <summary>Category codes removed in BR v1.2 — will be deactivated if present.</summary>
    private static readonly string[] RemovedCodes = ["SMOKE"];

    public static async Task SeedAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken ct = default)
    {
        var existingCodes = await db.PollutionCategories
            .Select(c => c.Code)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var existingSet = existingCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var added = 0;

        foreach (var (code, nameVi, nameEn) in DefaultCategories)
        {
            if (existingSet.Contains(code))
                continue;

            db.PollutionCategories.Add(PollutionCategory.Create(code, nameVi, nameEn));
            added++;
        }

        // BR-REP-005 v1.2: Deactivate removed categories
        var toDeactivate = await db.PollutionCategories
            .Where(c => RemovedCodes.Contains(c.Code) && c.IsActive)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var cat in toDeactivate)
        {
            cat.Deactivate();
            logger.LogInformation("Deactivated removed pollution category: {Code}", cat.Code);
        }

        if (added == 0 && toDeactivate.Count == 0)
            return;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        if (added > 0)
            logger.LogInformation("Seeded {Count} pollution categories", added);
    }
}
