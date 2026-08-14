namespace Greenlens.Infrastructure.Persistence.Seeders.Location;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// One-time import: đọc GeoJSON boundary (tỉnh/ward) từ gis.vn theo streaming feature-by-feature,
/// UPDATE cột <c>boundary</c> (geometry) của <c>provinces</c>/<c>wards</c> qua ST_GeomFromGeoJSON.
/// </summary>
/// <remarks>
/// KHÔNG chạy tự động lúc startup — chạy tay 1 lần qua <c>tools/Greenlens.DbSeed</c>
/// (xem <see cref="BoundaryGeometryImporterRunner"/>). File ward có thể tới vài trăm MB,
/// nên đọc bằng <see cref="Utf8JsonReader"/> qua FileStream, parse mỗi feature riêng lẻ
/// (<see cref="JsonDocument.ParseValue"/>) thay vì <c>JsonDocument.Parse</c> toàn file.
/// </remarks>
internal static class BoundaryGeometryImporter
{
    private const int BatchSize = 200;

    public static Task ImportProvincesAsync(
        ApplicationDbContext db, string geoJsonPath, ILogger logger, CancellationToken ct = default)
        => ImportAsync(db, geoJsonPath, "provinces", "ma_tinh", logger, ct);

    public static Task ImportWardsAsync(
        ApplicationDbContext db, string geoJsonPath, ILogger logger, CancellationToken ct = default)
        => ImportAsync(db, geoJsonPath, "wards", "ma_xa", logger, ct);

    private static async Task ImportAsync(
        ApplicationDbContext db,
        string geoJsonPath,
        string tableName,
        string codePropertyName,
        ILogger logger,
        CancellationToken ct)
    {
        var matched = 0;
        var unmatched = 0;
        var batch = new List<(string Code, string GeometryJson)>(BatchSize);

        // Utf8JsonReader là ref struct nên không thể "yield return" xuyên qua nó (CS4007) —
        // ReadFeatures dùng callback đồng bộ để gom feature vào batch. DbContext không
        // thread-safe cho các lệnh song song, nên mỗi batch đầy được flush (await) tuần tự
        // ngay trong callback trước khi tiếp tục parse phần còn lại của file.
        void OnFeature(string code, string geometryJson)
        {
            batch.Add((code, geometryJson));
            if (batch.Count < BatchSize)
                return;

            var (m, u) = FlushBatchAsync(db, tableName, batch, logger, ct)
                .GetAwaiter().GetResult();
            matched += m;
            unmatched += u;
            batch.Clear();
        }

        ReadFeatures(geoJsonPath, codePropertyName, OnFeature, ct);

        if (batch.Count > 0)
        {
            var (m, u) = await FlushBatchAsync(db, tableName, batch, logger, ct).ConfigureAwait(false);
            matched += m;
            unmatched += u;
        }

        logger.LogInformation(
            "Boundary import for {Table} done: matched={Matched}, unmatched={Unmatched}",
            tableName, matched, unmatched);
    }

    private static async Task<(int Matched, int Unmatched)> FlushBatchAsync(
        ApplicationDbContext db,
        string tableName,
        List<(string Code, string GeometryJson)> batch,
        ILogger logger,
        CancellationToken ct)
    {
        var matched = 0;
        var unmatched = 0;

        // tableName chỉ nhận "provinces"/"wards" (hardcode nội bộ, không phải input), an toàn để
        // ghép trực tiếp; geometryJson/code luôn đi qua tham số hoá để tránh injection.
        var sql = "UPDATE " + tableName +
            " SET boundary = ST_SetSRID(ST_Multi(ST_GeomFromGeoJSON({0})), 4326) WHERE code = {1}";

        foreach (var (code, geometryJson) in batch)
        {
            var rows = await db.Database.ExecuteSqlRawAsync(sql, [geometryJson, code], ct)
                .ConfigureAwait(false);

            if (rows > 0)
                matched++;
            else
            {
                unmatched++;
                logger.LogWarning("No {Table} row found for code {Code} — skipped", tableName, code);
            }
        }

        return (matched, unmatched);
    }

    /// <summary>
    /// Đọc FeatureCollection streaming: track tới "features" StartArray, sau đó parse
    /// TỪNG feature riêng lẻ (an toàn, mỗi feature chỉ vài KB–vài trăm KB, không load cả
    /// file vào memory) và gọi <paramref name="onFeature"/> ngay lập tức. Dùng callback
    /// đồng bộ thay vì <c>yield return</c> vì <see cref="Utf8JsonReader"/> là ref struct —
    /// không thể preserve qua iterator state machine (CS4007).
    /// </summary>
    /// <remarks>
    /// Buffer tự quản lý (không dùng <c>PipeReader</c>): đọc thêm dữ liệu vào cuối buffer,
    /// dùng <see cref="Utf8JsonReader.TrySkip"/> để xác nhận 1 feature object nằm TRỌN trong
    /// buffer hiện tại trước khi parse — nếu chưa đủ, nạp thêm rồi thử lại (KHÔNG gọi
    /// <c>JsonDocument.ParseValue</c> khi object có thể bị cắt giữa, vì hàm đó không tự xử lý
    /// multi-segment và sẽ throw <c>JsonReaderException</c>).
    /// </remarks>
    private static void ReadFeatures(
        string path,
        string codePropertyName,
        Action<string, string> onFeature,
        CancellationToken ct)
    {
        const int InitialBufferSize = 64 * 1024;

        using var stream = File.OpenRead(path);
        var buffer = new byte[InitialBufferSize];
        var bufferLength = 0;
        var inFeatures = false;
        var done = false;
        var streamAtEnd = false;
        var state = new JsonReaderState();

        while (!done)
        {
            ct.ThrowIfCancellationRequested();

            // Nạp thêm dữ liệu vào cuối buffer hiện tại (giữ lại phần chưa xử lý từ vòng trước).
            if (!streamAtEnd && bufferLength < buffer.Length)
            {
                var bytesRead = stream.Read(buffer, bufferLength, buffer.Length - bufferLength);
                bufferLength += bytesRead;
                if (bytesRead == 0)
                    streamAtEnd = true;
            }

            var isFinalBlock = streamAtEnd;
            // Truyền lại state (depth, "đang ở giữa array/object nào") từ vòng trước — nếu reset
            // về default mỗi vòng, reader tưởng đây luôn là khởi đầu 1 document mới và báo lỗi cấu
            // trúc ("',' is invalid...") ngay khi gặp dấu phẩy giữa 2 phần tử của "features" array.
            var jsonReader = new Utf8JsonReader(buffer.AsSpan(0, bufferLength), isFinalBlock, state);
            var progressed = false;

            while (true)
            {
                if (!inFeatures)
                {
                    if (!jsonReader.Read())
                        break;

                    if (jsonReader.TokenType == JsonTokenType.PropertyName
                        && jsonReader.ValueTextEquals("features"))
                    {
                        if (!jsonReader.Read() || jsonReader.TokenType != JsonTokenType.StartArray)
                        {
                            done = true;
                            break;
                        }
                        inFeatures = true;
                    }

                    progressed = true;
                    continue;
                }

                var beforeFeaturePos = (int)jsonReader.BytesConsumed;
                var probeReader = jsonReader;

                if (!probeReader.Read())
                    break; // hết dữ liệu trong buffer — cần đọc thêm.

                if (probeReader.TokenType == JsonTokenType.EndArray)
                {
                    jsonReader = probeReader;
                    done = true;
                    progressed = true;
                    break;
                }

                if (probeReader.TokenType != JsonTokenType.StartObject)
                {
                    jsonReader = probeReader;
                    progressed = true;
                    continue;
                }

                // TokenStartIndex = vị trí byte thật của dấu '{' (bỏ qua dấu ',' / khoảng trắng
                // trước đó mà BytesConsumed vẫn tính vào) — featureSpan phải bắt đầu từ đây.
                var featureStartPos = (int)probeReader.TokenStartIndex;

                // TrySkip xác nhận toàn bộ feature object nằm trọn trong buffer — nếu không,
                // dừng vòng trong, giữ nguyên jsonReader ở vị trí TRƯỚC feature này, và nạp
                // thêm dữ liệu ở vòng ngoài.
                if (!probeReader.TrySkip())
                {
                    if (isFinalBlock)
                        throw new InvalidDataException(
                            $"Unexpected end of GeoJSON while reading a feature at byte {beforeFeaturePos} in {path}.");
                    break;
                }

                var featureEndPos = (int)probeReader.BytesConsumed;
                var featureSpan = buffer.AsSpan(featureStartPos, featureEndPos - featureStartPos);
                var featureReader = new Utf8JsonReader(featureSpan, isFinalBlock: true, default);
                using var featureDoc = JsonDocument.ParseValue(ref featureReader);
                var root = featureDoc.RootElement;

                if (root.TryGetProperty("properties", out var properties)
                    && properties.TryGetProperty(codePropertyName, out var codeElement)
                    && codeElement.GetString() is { Length: > 0 } code
                    && root.TryGetProperty("geometry", out var geometry))
                {
                    onFeature(code, geometry.GetRawText());
                }

                jsonReader = probeReader;
                progressed = true;
            }

            if (done)
                break;

            state = jsonReader.CurrentState;
            var consumed = (int)jsonReader.BytesConsumed;
            var remaining = bufferLength - consumed;

            if (!progressed && remaining == buffer.Length && !isFinalBlock)
            {
                // 1 feature/token đơn lẻ lớn hơn cả buffer — tăng gấp đôi để chứa được.
                Array.Resize(ref buffer, buffer.Length * 2);
            }
            else if (consumed > 0)
            {
                Buffer.BlockCopy(buffer, consumed, buffer, 0, remaining);
            }

            bufferLength = remaining;

            if (isFinalBlock && remaining == 0)
                break;
        }
    }
}
