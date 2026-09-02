using Greenlens.Application.Common.Interfaces;
using Greenlens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

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

    public async Task<string?> FindProvinceCodeByPointAsync(
        decimal latitude, decimal longitude, CancellationToken ct = default)
    {
        var result = await db.Database
            .SqlQueryRaw<string>(
                """
                SELECT code AS "Value"
                FROM provinces
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

    public async Task<IReadOnlyDictionary<string, string>> GetAllProvinceBoundaryGeoJsonAsync(
        CancellationToken ct = default)
    {
        return await QueryCodeToGeoJsonAsync(
                """
                SELECT code, ST_AsGeoJSON(boundary)
                FROM provinces
                WHERE boundary IS NOT NULL
                """,
                [],
                ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyDictionary<string, string>> GetWardBoundaryGeoJsonByProvinceAsync(
        string provinceCode, CancellationToken ct = default)
    {
        return await QueryCodeToGeoJsonAsync(
                """
                SELECT code, ST_AsGeoJSON(boundary)
                FROM wards
                WHERE province_code = @provinceCode AND boundary IS NOT NULL
                """,
                [("provinceCode", provinceCode)],
                ct)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Ad-hoc ADO.NET (thay vì <c>SqlQueryRaw&lt;T&gt;</c>) — EF Core 9 chỉ hỗ trợ
    /// <c>SqlQueryRaw&lt;T&gt;</c> cho kiểu scalar hoặc entity đã khai báo qua
    /// <c>ToSqlQuery</c>/<c>HasNoKey</c>; dùng trực tiếp cho 1 record ad-hoc 2 cột sẽ ném
    /// <see cref="InvalidOperationException"/> lúc runtime. Đọc thẳng qua <see cref="NpgsqlDataReader"/>
    /// tránh phụ thuộc giới hạn đó, vẫn dùng chung connection/transaction của <see cref="ApplicationDbContext"/>.
    /// Named `@param` (KHÔNG dùng Npgsql positional `$1`) — đặt `ParameterName = "$1"` khiến Npgsql
    /// coi đó là named parameter tên "$1", không khớp placeholder `$1` trong SQL text → server báo
    /// "bind message supplies 0 parameters, but prepared statement requires 1" dù code build sạch.
    /// </summary>
    private async Task<IReadOnlyDictionary<string, string>> QueryCodeToGeoJsonAsync(
        string sql, (string Name, object Value)[] parameters, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        var wasClosed = connection.State != System.Data.ConnectionState.Open;
        if (wasClosed) await connection.OpenAsync(ct).ConfigureAwait(false);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            if (db.Database.CurrentTransaction is { } tx)
                command.Transaction = tx.GetDbTransaction();

            foreach (var (name, value) in parameters)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = name;
                parameter.Value = value;
                command.Parameters.Add(parameter);
            }

            var result = new Dictionary<string, string>();
            await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                var code = reader.GetString(0);
                var geoJson = reader.GetString(1);
                result[code] = geoJson;
            }
            return result;
        }
        finally
        {
            if (wasClosed) await connection.CloseAsync().ConfigureAwait(false);
        }
    }
}
