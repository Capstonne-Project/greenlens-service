namespace Greenlens.Application.Features.Map.GetPublicMapReports;

/// <summary>Metadata for GET map reports response.</summary>
public sealed record PublicMapReportsMetaDto(
    int Returned,
    int? Limit,
    int? GridLevel,
    decimal? CellSizeDegrees);
