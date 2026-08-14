namespace Greenlens.Application.Features.Catalog.GetWardBoundary;

/// <param name="GeoJson">GeoJSON geometry (MultiPolygon) string từ PostGIS ST_AsGeoJSON; null nếu chưa import boundary.</param>
/// <param name="BoundaryUrl">
/// DEPRECATED — CDN cũ đã chết, luôn null. Giữ tạm để FE có thời gian migrate sang <see cref="GeoJson"/>.
/// </param>
public sealed record GetWardBoundaryResponse(string WardCode, string? Name, string? GeoJson, string? BoundaryUrl);
