using Greenlens.Domain.Entities;
using Greenlens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.Seeders;

/// <summary>
/// Seeds the four default pollution categories for report forms and AI mapping.
/// </summary>
/// <remarks>Implements: BR-REP-005.</remarks>
internal static class PollutionCategorySeeder
{
    private static readonly (string Code, string NameVi, string NameEn)[] DefaultCategories =
    [
        ("TRASH", "Ô nhiễm rác thải", "Trash"),
        ("WASTEWATER", "Ô nhiễm nước", "Water"),
        ("SMOKE", "Ô nhiễm không khí", "Smoke"),
        ("CHEMICAL", "Ô nhiễm hóa chất", "Chemical"),
    ];

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

        if (added == 0)
            return;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Seeded {Count} pollution categories", added);
    }
}
