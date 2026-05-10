namespace Greenlens.Infrastructure.Persistence.Seeders.Location;

using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using Greenlens.Domain.Entities.Location;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Seeder cho dữ liệu hành chính Việt Nam (sau cải cách 2025).
/// Đọc dữ liệu từ embedded SQL resource <c>seed_data.sql</c>, parse bằng regex và bulk insert.
/// </summary>
/// <remarks>
/// <para>
/// Đây là <see cref="Infrastructure"/>-internal seeder chạy 1 lần khi DB chưa có dữ liệu vùng/tỉnh/phường.
/// Đặc thù: catalog data lớn (~3300 wards) nên KHÔNG dùng EF <c>HasData()</c> (sẽ làm migration phình to).
/// </para>
/// <para>
/// Idempotent: mỗi step kiểm tra <c>AnyAsync()</c> trước khi seed, an toàn để chạy mọi lần startup.
/// </para>
/// <para>Source dữ liệu: <see href="https://github.com/ThangLeQuoc/vietnamese-provinces-database"/></para>
/// </remarks>
internal static partial class LocationSeeder
{
    /// <summary>Must match EmbeddedResource logical name (SDK: AssemblyName + path with dots).</summary>
    private const string SeedSqlResourceName =
        "Greenlens.Infrastructure.Seeders.Location.seed_data.sql";

    /// <summary>Batch size cho bulk insert wards (tránh OOM + giảm số transaction).</summary>
    private const int BatchSize = 500;

    public static async Task SeedAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var sql = await ReadEmbeddedSqlAsync();
        if (string.IsNullOrEmpty(sql))
        {
            logger.LogCritical("Embedded resource {Resource} missing or empty", SeedSqlResourceName);
            throw new InvalidOperationException(
                $"Location seed requires embedded resource {SeedSqlResourceName}. " +
                "Đảm bảo seed_data.sql được đánh dấu là EmbeddedResource trong .csproj.");
        }

        await SeedAdministrativeRegionsAsync(db, sql, logger, cancellationToken);
        await SeedAdministrativeUnitsAsync(db, sql, logger, cancellationToken);
        await SeedProvincesAsync(db, sql, logger, cancellationToken);
        await SeedWardsAsync(db, sql, logger, cancellationToken);
        await SeedProvinceBoundaryUrlsAsync(db, logger, cancellationToken);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Administrative regions (8 rows)
    // ─────────────────────────────────────────────────────────────────────────
    private static async Task SeedAdministrativeRegionsAsync(
        ApplicationDbContext db, string sql, ILogger logger, CancellationToken ct)
    {
        if (await db.Set<AdministrativeRegion>().AnyAsync(ct))
        {
            logger.LogInformation("Administrative regions already seeded, skipping");
            return;
        }

        var regions = ParseAdministrativeRegionsFromSql(sql);
        if (regions.Count == 0)
        {
            logger.LogCritical("Parsed 0 administrative_regions from seed_data.sql");
            throw new InvalidOperationException("Location seed: no administrative_regions parsed.");
        }

        db.Set<AdministrativeRegion>().AddRange(regions);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} administrative regions", regions.Count);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Administrative units (5 rows)
    // ─────────────────────────────────────────────────────────────────────────
    private static async Task SeedAdministrativeUnitsAsync(
        ApplicationDbContext db, string sql, ILogger logger, CancellationToken ct)
    {
        if (await db.Set<AdministrativeUnit>().AnyAsync(ct))
        {
            logger.LogInformation("Administrative units already seeded, skipping");
            return;
        }

        var units = ParseAdministrativeUnitsFromSql(sql);
        if (units.Count == 0)
        {
            logger.LogCritical("Parsed 0 administrative_units from seed_data.sql");
            throw new InvalidOperationException("Location seed: no administrative_units parsed.");
        }

        db.Set<AdministrativeUnit>().AddRange(units);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} administrative units", units.Count);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Provinces (34 rows)
    // ─────────────────────────────────────────────────────────────────────────
    private static async Task SeedProvincesAsync(
        ApplicationDbContext db, string sql, ILogger logger, CancellationToken ct)
    {
        if (await db.Set<Province>().AnyAsync(ct))
        {
            logger.LogInformation("Provinces already seeded, skipping");
            return;
        }

        var provinces = ParseProvincesFromSql(sql);
        if (provinces.Count == 0)
        {
            logger.LogCritical("Parsed 0 provinces from seed_data.sql");
            throw new InvalidOperationException("Location seed: no provinces parsed.");
        }

        db.Set<Province>().AddRange(provinces);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} provinces", provinces.Count);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Wards (~3300 rows) - bulk insert with batches
    // ─────────────────────────────────────────────────────────────────────────
    private static async Task SeedWardsAsync(
        ApplicationDbContext db, string sql, ILogger logger, CancellationToken ct)
    {
        if (await db.Set<Ward>().AnyAsync(ct))
        {
            logger.LogInformation("Wards already seeded, skipping");
            return;
        }

        var wards = ParseWardsFromSql(sql);
        if (wards.Count == 0)
        {
            logger.LogCritical("Parsed 0 wards from seed_data.sql; check INSERT format");
            throw new InvalidOperationException("Location seed: no ward rows parsed.");
        }

        logger.LogInformation("Parsed {Count} wards from SQL resource, bulk inserting in batches of {BatchSize}",
            wards.Count, BatchSize);

        for (var i = 0; i < wards.Count; i += BatchSize)
        {
            var batch = wards.Skip(i).Take(BatchSize).ToList();
            db.Set<Ward>().AddRange(batch);
            await db.SaveChangesAsync(ct);

            // Detach để tránh tracking memory bloat khi bulk insert nhiều batch
            foreach (var entry in db.ChangeTracker.Entries<Ward>().ToList())
                entry.State = EntityState.Detached;
        }

        logger.LogInformation("Seeded {Count} wards", wards.Count);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Province boundary URLs - bulk UPDATE via VALUES table (giữ logic project cũ)
    // ─────────────────────────────────────────────────────────────────────────
    private static async Task SeedProvinceBoundaryUrlsAsync(
        ApplicationDbContext db, ILogger logger, CancellationToken ct)
    {
        var existingCodes = await db.Set<Province>()
            .Where(p => p.BoundaryUrl == null)
            .Select(p => p.Code)
            .ToListAsync(ct);

        if (existingCodes.Count == 0)
        {
            logger.LogInformation("All provinces already have BoundaryUrl, skipping");
            return;
        }

        // Bulk UPDATE qua VALUES table thay vì 34 round-trip riêng lẻ.
        // URLs là compile-time constants, KHÔNG có SQL injection risk.
        var valueRows = string.Join(",",
            ProvinceBoundaryUrls.Mapping.Select(kv => $"('{kv.Key}','{kv.Value}')"));

        // PostgreSQL syntax — table name "provinces" (snake_case do EFCore.NamingConventions).
#pragma warning disable EF1002 // values are compile-time constants, no injection risk
        await db.Database.ExecuteSqlRawAsync(
            $"""
            UPDATE provinces SET boundary_url = v.url
            FROM (VALUES {valueRows}) AS v(code, url)
            WHERE provinces.code = v.code AND provinces.boundary_url IS NULL
            """,
            ct);
#pragma warning restore EF1002

        logger.LogInformation("Seeded {Count} province boundary URLs", ProvinceBoundaryUrls.Mapping.Count);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Resource loading
    // ─────────────────────────────────────────────────────────────────────────
    private static async Task<string?> ReadEmbeddedSqlAsync()
    {
        var assembly = typeof(LocationSeeder).Assembly;
        await using var stream = assembly.GetManifestResourceStream(SeedSqlResourceName);
        if (stream is null) return null;

        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SQL section markers (giữ nguyên format từ seed_data.sql gốc)
    // ─────────────────────────────────────────────────────────────────────────
    private const string RegionsSectionStart  = "-- DATA for administrative_regions --";
    private const string RegionsSectionEnd    = "-- DATA for administrative_units --";
    private const string UnitsSectionStart    = "-- DATA for administrative_units --";
    private const string UnitsSectionEnd      = "-- DATA for provinces --";
    private const string ProvincesSectionStart = "-- DATA for provinces --";
    private const string ProvincesSectionEnd   = "-- DATA for wards --";
    private const string WardsSectionStart     = "-- DATA for wards --";

    private static string ExtractSqlSection(string sql, string startMarker, string? endMarker)
    {
        var start = sql.IndexOf(startMarker, StringComparison.Ordinal);
        if (start < 0) return string.Empty;
        start += startMarker.Length;

        if (string.IsNullOrEmpty(endMarker)) return sql[start..];

        var end = sql.IndexOf(endMarker, start, StringComparison.Ordinal);
        return sql[start..(end < 0 ? sql.Length : end)];
    }

    private static string UnescapeSqlString(string s) => s.Replace("''", "'", StringComparison.Ordinal);

    // ─────────────────────────────────────────────────────────────────────────
    // Parsers
    // ─────────────────────────────────────────────────────────────────────────
    private static List<AdministrativeRegion> ParseAdministrativeRegionsFromSql(string fullSql)
    {
        var section = ExtractSqlSection(fullSql, RegionsSectionStart, RegionsSectionEnd);
        var list = new List<AdministrativeRegion>();

        foreach (Match match in AdministrativeRegionInsertRegex().Matches(section))
        {
            list.Add(AdministrativeRegion.Seed(
                id: int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
                name: UnescapeSqlString(match.Groups[2].Value)));
        }

        return list.OrderBy(r => r.Id).ToList();
    }

    private static List<AdministrativeUnit> ParseAdministrativeUnitsFromSql(string fullSql)
    {
        var section = ExtractSqlSection(fullSql, UnitsSectionStart, UnitsSectionEnd);
        var list = new List<AdministrativeUnit>();

        foreach (Match match in AdministrativeUnitInsertRegex().Matches(section))
        {
            list.Add(AdministrativeUnit.Seed(
                id: int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
                name: UnescapeSqlString(match.Groups[2].Value),
                abbreviation: UnescapeSqlString(match.Groups[3].Value)));
        }

        return list.OrderBy(u => u.Id).ToList();
    }

    private static List<Province> ParseProvincesFromSql(string fullSql)
    {
        var section = ExtractSqlSection(fullSql, ProvincesSectionStart, ProvincesSectionEnd);
        if (string.IsNullOrEmpty(section)) return [];

        var map = ProvinceRegionMap.Mapping;
        var list = new List<Province>();

        foreach (Match match in ProvinceRowRegex().Matches(section))
        {
            var code = match.Groups[1].Value;
            var name = UnescapeSqlString(match.Groups[2].Value);
            var administrativeUnitId = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);

            if (!map.TryGetValue(code, out var regionId))
            {
                throw new InvalidOperationException(
                    $"Location seed: province code '{code}' has no AdministrativeRegionId mapping; " +
                    $"update {nameof(ProvinceRegionMap)}.");
            }

            list.Add(Province.Seed(code, name, regionId, administrativeUnitId));
        }

        return list.OrderBy(p => p.Code, StringComparer.Ordinal).ToList();
    }

    private static List<Ward> ParseWardsFromSql(string fullSql)
    {
        var section = ExtractSqlSection(fullSql, WardsSectionStart, endMarker: null);
        var wards = new List<Ward>();

        foreach (Match match in WardRowRegex().Matches(section))
        {
            wards.Add(Ward.Seed(
                code: match.Groups[1].Value,
                name: UnescapeSqlString(match.Groups[2].Value),
                provinceCode: match.Groups[3].Value,
                administrativeUnitId: int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture)));
        }

        return wards;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Regex patterns (source-generated — .NET 9)
    // ─────────────────────────────────────────────────────────────────────────
    [GeneratedRegex(
        @"INSERT INTO administrative_regions\(id,name,name_en,code_name,code_name_en\) VALUES\((\d+),'((?:[^']|'')+)'")]
    private static partial Regex AdministrativeRegionInsertRegex();

    [GeneratedRegex(
        @"INSERT INTO administrative_units\(id,full_name,full_name_en,short_name,short_name_en,code_name,code_name_en\) VALUES\((\d+),'((?:[^']|'')+)','(?:[^']|'')+','((?:[^']|'')+)'")]
    private static partial Regex AdministrativeUnitInsertRegex();

    [GeneratedRegex(@"\('(\d{2})','((?:[^']|'')+)','(?:[^']|'')+','(?:[^']|'')+','(?:[^']|'')+','(?:[^']|'')+',(\d+)\)")]
    private static partial Regex ProvinceRowRegex();

    [GeneratedRegex(@"\('(\d+)','((?:[^']|'')+)','(?:[^']|'')+','(?:[^']|'')+','(?:[^']|'')+','(?:[^']|'')+','(\d+)',(\d+)\)")]
    private static partial Regex WardRowRegex();
}
