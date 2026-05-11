namespace Greenlens.Application.Features.Catalog.GetWardsByProvince;

/// <summary>One ward row for dropdowns; UnitAbbreviation is e.g. Phường / Xã.</summary>
/// <param name="BoundaryUrl">HTTPS URL to GeoJSON polygon for map overlay; null if not seeded.</param>
public sealed record WardListItemDto(
    string Code,
    string Name,
    string UnitAbbreviation,
    string? BoundaryUrl);
