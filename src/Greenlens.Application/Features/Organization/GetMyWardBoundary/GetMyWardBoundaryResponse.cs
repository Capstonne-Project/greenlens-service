namespace Greenlens.Application.Features.Organization.GetMyWardBoundary;

/// <param name="BoundaryUrl">HTTPS URL to GeoJSON polygon for map overlay; null if not seeded.</param>
public sealed record GetMyWardBoundaryResponse(string WardCode, string? WardName, string? BoundaryUrl);
