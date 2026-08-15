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
}
