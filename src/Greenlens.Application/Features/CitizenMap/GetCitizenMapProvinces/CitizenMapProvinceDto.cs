namespace Greenlens.Application.Features.CitizenMap.GetCitizenMapProvinces;

/// <param name="GeoJson">GeoJSON geometry (Polygon/MultiPolygon) string from PostGIS; null if boundary not imported yet.</param>
public sealed record CitizenMapProvinceDto(string Code, string Name, string? GeoJson);
