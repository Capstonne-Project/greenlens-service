namespace Greenlens.Application.Features.Catalog.GetProvinces;

/// <summary>One province row for dropdowns (official 2-char code).</summary>
/// <param name="BoundaryUrl">HTTPS URL to GeoJSON polygon for map overlay; null if not seeded.</param>
public sealed record ProvinceListItemDto(string Code, string Name, string? BoundaryUrl);
