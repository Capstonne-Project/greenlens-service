using Greenlens.Application.Common.Interfaces;
using Greenlens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Infrastructure.Geo;

/// <summary>
/// PostGIS ST_DWithin query for citizens with report history near a point (BR-NTF-002).
/// </summary>
internal sealed class NearbyCitizenQuery(ApplicationDbContext db) : INearbyCitizenQuery
{
    public async Task<IReadOnlyList<Guid>> FindCitizenIdsWithinRadiusAsync(
        decimal latitude,
        decimal longitude,
        Guid? excludeUserId,
        double radiusMeters,
        int maxRecipients,
        CancellationToken ct = default)
    {
        if (maxRecipients <= 0)
            return [];

        // Citizens who previously reported within radius — best available proxy for "near me".
        var sql = excludeUserId.HasValue
            ? """
              SELECT DISTINCT u.id AS "Value"
              FROM users u
              INNER JOIN reports r ON r.reporter_id = u.id
              WHERE u.role = 'Citizen'
                AND u.is_banned = false
                AND u.deleted_at IS NULL
                AND r.deleted_at IS NULL
                AND r.reporter_id IS NOT NULL
                AND u.id <> {4}
                AND ST_DWithin(
                    ST_MakePoint(r.longitude, r.latitude)::geography,
                    ST_MakePoint({0}, {1})::geography,
                    {2})
              LIMIT {3}
              """
            : """
              SELECT DISTINCT u.id AS "Value"
              FROM users u
              INNER JOIN reports r ON r.reporter_id = u.id
              WHERE u.role = 'Citizen'
                AND u.is_banned = false
                AND u.deleted_at IS NULL
                AND r.deleted_at IS NULL
                AND r.reporter_id IS NOT NULL
                AND ST_DWithin(
                    ST_MakePoint(r.longitude, r.latitude)::geography,
                    ST_MakePoint({0}, {1})::geography,
                    {2})
              LIMIT {3}
              """;

        var query = excludeUserId.HasValue
            ? db.Database.SqlQueryRaw<Guid>(
                sql,
                (double)longitude,
                (double)latitude,
                radiusMeters,
                maxRecipients,
                excludeUserId.Value)
            : db.Database.SqlQueryRaw<Guid>(
                sql,
                (double)longitude,
                (double)latitude,
                radiusMeters,
                maxRecipients);

        return await query.ToListAsync(ct).ConfigureAwait(false);
    }
}
