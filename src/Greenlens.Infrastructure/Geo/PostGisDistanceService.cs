using Microsoft.EntityFrameworkCore;
using Greenlens.Infrastructure.Persistence;

namespace Greenlens.Infrastructure.Geo;

/// <summary>
/// PostGIS-based distance calculation using ST_Distance with geography type.
/// BR-CLN-002, BR-INS-004: check-in ≤ 200m.
/// </summary>
internal sealed class PostGisDistanceService(ApplicationDbContext db)
    : Greenlens.Application.Common.Interfaces.IGeoDistanceService
{
    /// <inheritdoc />
    public async Task<double> GetDistanceInMetersAsync(
        decimal lat1, decimal lng1,
        decimal lat2, decimal lng2,
        CancellationToken ct = default)
    {
        // ST_MakePoint(lng, lat) — note: PostGIS uses (x=lng, y=lat) order.
        // ::geography casts to WGS84 sphere, so ST_Distance returns meters.
        var distance = await db.Database
            .SqlQueryRaw<double>(
                """
                SELECT ST_Distance(
                    ST_MakePoint({0}, {1})::geography,
                    ST_MakePoint({2}, {3})::geography
                ) AS "Value"
                """,
                (double)lng1, (double)lat1,
                (double)lng2, (double)lat2)
            .SingleAsync(ct)
            .ConfigureAwait(false);

        return distance;
    }
}
