namespace Greenlens.Application.Features.Map.GetPublicMapReports;

/// <summary>
/// Public map payload: either per-report pins (detail) or grouped cells (aggregate).
/// </summary>
public sealed record PublicMapReportsResponse(
    string Mode,
    IReadOnlyList<PublicMapReportPinDto>? Items,
    IReadOnlyList<PublicMapAggregateCellDto>? Cells,
    PublicMapReportsMetaDto Meta);
