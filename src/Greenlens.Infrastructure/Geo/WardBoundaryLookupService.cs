using Greenlens.Application.Common.Interfaces;
using Greenlens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Infrastructure.Geo;

/// <summary>
/// PostGIS point-in-polygon lookup for ward boundaries (BR-ORG-004, BR-ORG-010, BR-ORG-016).
/// </summary>
internal sealed class WardBoundaryLookupService(ApplicationDbContext db) : IWardBoundaryLookupService
{
    public async Task<string?> FindWardCodeByPointAsync(
        decimal latitude, decimal longitude, CancellationToken ct = default)
    {
        // ST_MakePoint(lng, lat) — PostGIS uses (x=lng, y=lat) order.
        var result = await db.Database
            .SqlQueryRaw<string>(
                """
                SELECT code AS "Value"
                FROM wards
                WHERE boundary IS NOT NULL
                  AND ST_Contains(boundary, ST_SetSRID(ST_MakePoint({0}, {1}), 4326))
                LIMIT 1
                """,
                (double)longitude, (double)latitude)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return result.SingleOrDefault();
    }

    public async Task<string?> GetWardBoundaryGeoJsonAsync(string wardCode, CancellationToken ct = default)
    {
        var result = await db.Database
            .SqlQueryRaw<string>(
                """
                SELECT ST_AsGeoJSON(boundary) AS "Value"
                FROM wards
                WHERE code = {0} AND boundary IS NOT NULL
                """,
                wardCode)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return result.SingleOrDefault();
    }

    public async Task<string?> GetProvinceBoundaryGeoJsonAsync(string provinceCode, CancellationToken ct = default)
    {
        var result = await db.Database
            .SqlQueryRaw<string>(
                """
                SELECT ST_AsGeoJSON(boundary) AS "Value"
                FROM provinces
                WHERE code = {0} AND boundary IS NOT NULL
                """,
                provinceCode)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return result.SingleOrDefault();
    }
}
