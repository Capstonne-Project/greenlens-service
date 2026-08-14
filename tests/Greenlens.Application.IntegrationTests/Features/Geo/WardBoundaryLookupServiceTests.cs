using FluentAssertions;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.IntegrationTests.Fixtures;
using Greenlens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Greenlens.Application.IntegrationTests.Features.Geo;

/// <remarks>Implements: BR-ORG-004, BR-ORG-010, BR-ORG-016 (point-in-polygon từ GPS).</remarks>
[Collection("Postgres")]
public sealed class WardBoundaryLookupServiceTests(PostgresContainerFixture fixture)
    : IntegrationTestBase(fixture)
{
    // Hình vuông đơn giản quanh (lng 105.80–105.81, lat 21.00–21.01) — không trùng vùng thật nào.
    private const string SquareWkt =
        "MULTIPOLYGON(((105.80 21.00, 105.81 21.00, 105.81 21.01, 105.80 21.01, 105.80 21.00)))";

    [Fact]
    public async Task FindWardCodeByPointAsync_PointInsidePolygon_ReturnsWardCode()
    {
        await SeedWardWithBoundaryAsync("00004", SquareWkt);

        var lookup = Services.GetRequiredService<IWardBoundaryLookupService>();

        var result = await lookup.FindWardCodeByPointAsync(latitude: 21.005m, longitude: 105.805m);

        result.Should().Be("00004");
    }

    [Fact]
    public async Task FindWardCodeByPointAsync_PointOutsidePolygon_ReturnsNull()
    {
        await SeedWardWithBoundaryAsync("00004", SquareWkt);

        var lookup = Services.GetRequiredService<IWardBoundaryLookupService>();

        // Điểm rõ ràng nằm ngoài hình vuông đã seed.
        var result = await lookup.FindWardCodeByPointAsync(latitude: 10.0m, longitude: 106.0m);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetWardBoundaryGeoJsonAsync_WardWithBoundary_ReturnsValidGeoJson()
    {
        await SeedWardWithBoundaryAsync("00004", SquareWkt);

        var lookup = Services.GetRequiredService<IWardBoundaryLookupService>();

        var geoJson = await lookup.GetWardBoundaryGeoJsonAsync("00004");

        geoJson.Should().NotBeNullOrEmpty();
        geoJson!.Should().Contain("MultiPolygon");
    }

    [Fact]
    public async Task GetWardBoundaryGeoJsonAsync_WardWithoutBoundary_ReturnsNull()
    {
        await SeedWardWithoutBoundaryAsync("00008");

        var lookup = Services.GetRequiredService<IWardBoundaryLookupService>();

        var geoJson = await lookup.GetWardBoundaryGeoJsonAsync("00008");

        geoJson.Should().BeNull();
    }

    private async Task SeedWardWithBoundaryAsync(string wardCode, string wkt)
    {
        await SeedWardWithoutBoundaryAsync(wardCode);

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE wards SET boundary = ST_GeomFromText({wkt}, 4326) WHERE code = {wardCode}");
    }

    private async Task SeedWardWithoutBoundaryAsync(string wardCode)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        const string provinceCode = "01";

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO administrative_regions (id, name, name_en, code_name, code_name_en)
            VALUES (1, 'Test Region', 'Test Region', 'test_region', 'test_region')
            ON CONFLICT (id) DO NOTHING
            """);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO administrative_units (id, full_name, full_name_en, short_name, short_name_en, code_name, code_name_en)
            VALUES (1, 'Tỉnh', 'Province', 'Tỉnh', 'Province', 'tinh', 'province'),
                   (3, 'Phường', 'Ward', 'Phường', 'Ward', 'phuong', 'ward')
            ON CONFLICT (id) DO NOTHING
            """);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO provinces (code, name, administrative_region_id, administrative_unit_id)
            VALUES ({provinceCode}, 'Test Province', 1, 1)
            ON CONFLICT (code) DO NOTHING
            """);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO wards (code, name, province_code, administrative_unit_id)
            VALUES ({wardCode}, 'Test Ward', {provinceCode}, 3)
            ON CONFLICT (code) DO NOTHING
            """);
    }
}
