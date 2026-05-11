namespace Greenlens.Infrastructure.Persistence.Seeders.Location;

/// <summary>
/// Mapping từ province code sang AdministrativeRegionId.
/// Dataset gốc <c>seed_data.sql</c> không lưu region_id ở row provinces,
/// nên seeder cần map ngoài (giữ nguyên từ project location-service trước đây).
/// </summary>
internal static class ProvinceRegionMap
{
    public static IReadOnlyDictionary<string, int> Mapping { get; } =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            // Đồng bằng sông Hồng (3)
            ["01"] = 3, ["24"] = 3, ["25"] = 3, ["31"] = 3, ["33"] = 3, ["37"] = 3,

            // Đông Bắc Bộ (1)
            ["04"] = 1, ["08"] = 1, ["19"] = 1, ["20"] = 1, ["22"] = 1,

            // Tây Bắc Bộ (2)
            ["11"] = 2, ["12"] = 2, ["14"] = 2, ["15"] = 2,

            // Bắc Trung Bộ (4)
            ["38"] = 4, ["40"] = 4, ["42"] = 4, ["44"] = 4, ["46"] = 4,

            // Duyên hải Nam Trung Bộ (5)
            ["48"] = 5, ["51"] = 5, ["56"] = 5,

            // Tây Nguyên (6)
            ["52"] = 6, ["66"] = 6, ["68"] = 6,

            // Đông Nam Bộ (7)
            ["75"] = 7, ["79"] = 7, ["80"] = 7,

            // Đồng bằng sông Cửu Long (8)
            ["82"] = 8, ["86"] = 8, ["91"] = 8, ["92"] = 8, ["96"] = 8,
        };
}
