namespace Greenlens.Application.Features.Catalog.GetWardBoundary;

/// <param name="BoundaryUrl">HTTPS URL to GeoJSON polygon for map overlay; null if not seeded.</param>
public sealed record GetWardBoundaryResponse(string WardCode, string? BoundaryUrl);
