namespace Greenlens.Application.Common.Interfaces;

/// <summary>
/// PostGIS-backed lookup cho ranh giới hành chính ward (point-in-polygon, GeoJSON export).
/// </summary>
/// <remarks>
/// Implements: BR-ORG-004 (LocalOffice gắn WardCode → polygon GeoJSON), BR-ORG-010/016
/// (point-in-polygon để xác định WardCode cho định tuyến report theo GPS).
/// </remarks>
public interface IWardBoundaryLookupService
{
    /// <summary>Tìm ward mà polygon ranh giới chứa điểm GPS này, hoặc null nếu không có ward nào khớp.</summary>
    Task<string?> FindWardCodeByPointAsync(decimal latitude, decimal longitude, CancellationToken ct = default);

    /// <summary>
    /// Tìm tỉnh chứa điểm GPS khi ward lookup thất bại — dùng cho BR-ORG-011 Department queue fallback.
    /// </summary>
    Task<string?> FindProvinceCodeByPointAsync(decimal latitude, decimal longitude, CancellationToken ct = default);

    /// <summary>Trả về ranh giới ward dạng GeoJSON geometry (qua ST_AsGeoJSON), hoặc null nếu chưa có boundary.</summary>
    Task<string?> GetWardBoundaryGeoJsonAsync(string wardCode, CancellationToken ct = default);

    /// <summary>Trả về ranh giới tỉnh dạng GeoJSON geometry (qua ST_AsGeoJSON), hoặc null nếu chưa có boundary.</summary>
    Task<string?> GetProvinceBoundaryGeoJsonAsync(string provinceCode, CancellationToken ct = default);

    /// <summary>
    /// Trả về ranh giới GeoJSON của TẤT CẢ tỉnh trong 1 query (code → geoJson), dùng cho citizen
    /// map bước 1 (toàn quốc) — tránh N+1 khi tô ranh giới 34 tỉnh cùng lúc.
    /// </summary>
    Task<IReadOnlyDictionary<string, string>> GetAllProvinceBoundaryGeoJsonAsync(CancellationToken ct = default);

    /// <summary>
    /// Trả về ranh giới GeoJSON của toàn bộ ward thuộc 1 tỉnh trong 1 query (code → geoJson), dùng
    /// cho citizen map bước 2 (drill-down phường/xã) — tránh N+1 khi tô ranh giới hàng chục ward.
    /// </summary>
    Task<IReadOnlyDictionary<string, string>> GetWardBoundaryGeoJsonByProvinceAsync(
        string provinceCode, CancellationToken ct = default);
}
