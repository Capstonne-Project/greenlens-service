using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.Seeders;

/// <summary>
/// Seeds one LocalOffice per ward (linked to its province's Department),
/// plus one LEO account per office and one DEO account per department.
/// Runs on startup in Development only. Idempotent — skips existing records.
/// </summary>
/// <remarks>
/// Email pattern:
///   LEO: leo.{wardCode}@greenlens.dev   (e.g. leo.00004@greenlens.dev)
///   DEO: deo.{provinceCode}@greenlens.dev (e.g. deo.79@greenlens.dev)
/// Password: Officer@123
/// </remarks>
internal static class LocalOfficeSeeder
{
    private const string DefaultPassword = "Officer@123";

    public static async Task SeedAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken ct = default)
    {
        // ── 1. Seed LocalOffices (1 per ward, linked to department via province) ──
        var departments = await db.Departments
            .AsNoTracking()
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (departments.Count == 0)
        {
            logger.LogWarning("No departments found — skipping LocalOffice seed");
            return;
        }

        var deptByProvince = departments.ToDictionary(d => d.ProvinceCode, d => d.Id);

        var existingWardCodes = await db.LocalOffices
            .Select(lo => lo.WardCode)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var existingSet = existingWardCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var wards = await db.Set<Domain.Entities.Location.Ward>()
            .AsNoTracking()
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var officesAdded = 0;

        foreach (var ward in wards.OrderBy(w => w.Code))
        {
            if (existingSet.Contains(ward.Code))
                continue;

            if (!deptByProvince.TryGetValue(ward.ProvinceCode, out var deptId))
                continue;

            var office = LocalOffice.Create(
                name: $"VP MTĐT {ward.Name}",
                departmentId: deptId,
                wardCode: ward.Code);

            db.LocalOffices.Add(office);
            officesAdded++;
        }

        if (officesAdded > 0)
        {
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            logger.LogInformation("Seeded {Count} local offices", officesAdded);
        }

        // ── 2. Seed DEO accounts (1 per department) ──
        await SeedDeoAccountsAsync(db, logger, ct).ConfigureAwait(false);

        // ── 3. Seed LEO accounts (1 per office) + assign to office ──
        await SeedLeoAccountsAsync(db, logger, ct).ConfigureAwait(false);
    }

    private static async Task SeedDeoAccountsAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken ct)
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(DefaultPassword, workFactor: 12);

        var departments = await db.Departments
            .Include(d => d.Province)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        // Get all existing DEO emails to skip
        var existingDeoEmails = await db.Users
            .Where(u => u.Role == UserRole.DEO)
            .Select(u => u.Email)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var existingSet = existingDeoEmails.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var added = 0;

        foreach (var dept in departments.OrderBy(d => d.ProvinceCode))
        {
            var email = $"deo.{dept.ProvinceCode}@greenlens.dev";

            if (existingSet.Contains(email))
                continue;

            var provinceName = dept.Province?.Name ?? dept.Name.Replace("Sở TNMT ", "");
            var user = User.CreateByAdmin(
                email,
                passwordHash,
                $"DEO {provinceName}",
                UserRole.DEO);

            user.AssignToDepartment(dept.Id);
            db.Users.Add(user);
            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            logger.LogInformation("Seeded {Count} DEO accounts (password: {Password})", added, DefaultPassword);
        }
    }

    private static async Task SeedLeoAccountsAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken ct)
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(DefaultPassword, workFactor: 12);

        var offices = await db.LocalOffices
            .ToListAsync(ct)
            .ConfigureAwait(false);

        // Get all existing LEO emails to skip
        var existingLeoEmails = await db.Users
            .Where(u => u.Role == UserRole.LEO)
            .Select(u => u.Email)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var existingSet = existingLeoEmails.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var added = 0;

        foreach (var office in offices.OrderBy(o => o.WardCode))
        {
            var email = $"leo.{office.WardCode}@greenlens.dev";

            if (existingSet.Contains(email))
                continue;

            var officeName = office.Name
                .Replace("VP MTĐT ", "")
                .Replace("VP MTDT ", "");
            var user = User.CreateByAdmin(
                email,
                passwordHash,
                $"LEO {officeName}",
                UserRole.LEO);

            user.AssignToLocalOffice(office.Id);
            db.Users.Add(user);

            // Also assign this LEO as the officer of the office
            office.AssignOfficer(user.Id);

            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            logger.LogInformation("Seeded {Count} LEO accounts (password: {Password})", added, DefaultPassword);
        }
    }
}
