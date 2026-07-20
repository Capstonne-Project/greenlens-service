using Greenlens.Domain.Entities;
using Greenlens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.Seeders;

/// <summary>
/// Seeds one Department per province in the database.
/// Each province gets a "Sở Tài nguyên và Môi trường [TenTinh]" department
/// so that citizen reports are auto-routed to the correct province queue.
/// </summary>
/// <remarks>Implements: BR-ORG-001 — every province has exactly one Department.</remarks>
internal static class DepartmentSeeder
{
    /// <summary>
    /// (ProvinceCode, ProvinceName) — all 34 provinces currently in the database.
    /// Naming convention: "Sở TNMT [TenTinh]" for consistency.
    /// </summary>
    private static readonly (string Code, string Name)[] ProvinceDepartments =
    [
        ("01", "Sở TNMT Hà Nội"),
        ("04", "Sở TNMT Cao Bằng"),
        ("08", "Sở TNMT Tuyên Quang"),
        ("11", "Sở TNMT Điện Biên"),
        ("12", "Sở TNMT Lai Châu"),
        ("14", "Sở TNMT Sơn La"),
        ("15", "Sở TNMT Lào Cai"),
        ("19", "Sở TNMT Thái Nguyên"),
        ("20", "Sở TNMT Lạng Sơn"),
        ("22", "Sở TNMT Quảng Ninh"),
        ("24", "Sở TNMT Bắc Ninh"),
        ("25", "Sở TNMT Phú Thọ"),
        ("31", "Sở TNMT Hải Phòng"),
        ("33", "Sở TNMT Hưng Yên"),
        ("37", "Sở TNMT Ninh Bình"),
        ("38", "Sở TNMT Thanh Hóa"),
        ("40", "Sở TNMT Nghệ An"),
        ("42", "Sở TNMT Hà Tĩnh"),
        ("44", "Sở TNMT Quảng Trị"),
        ("46", "Sở TNMT Huế"),
        ("48", "Sở TNMT Đà Nẵng"),
        ("51", "Sở TNMT Quảng Ngãi"),
        ("52", "Sở TNMT Gia Lai"),
        ("56", "Sở TNMT Khánh Hòa"),
        ("66", "Sở TNMT Đắk Lắk"),
        ("68", "Sở TNMT Lâm Đồng"),
        ("75", "Sở TNMT Đồng Nai"),
        ("79", "Sở TNMT Hồ Chí Minh"),
        ("80", "Sở TNMT Tây Ninh"),
        ("82", "Sở TNMT Đồng Tháp"),
        ("86", "Sở TNMT Vĩnh Long"),
        ("91", "Sở TNMT An Giang"),
        ("92", "Sở TNMT Cần Thơ"),
        ("96", "Sở TNMT Cà Mau"),
    ];

    public static async Task SeedAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken ct = default)
    {
        var existingCodes = await db.Departments
            .Select(d => d.ProvinceCode)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var existingSet = existingCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var added = 0;

        foreach (var (code, name) in ProvinceDepartments)
        {
            if (existingSet.Contains(code))
                continue;

            db.Departments.Add(Department.Create(name, code));
            added++;
        }

        if (added == 0)
            return;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Seeded {Count} departments for provinces", added);
    }
}
