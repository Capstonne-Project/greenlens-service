namespace Greenlens.Application.Features.CitizenMap.GetCitizenMapWardReports;

public sealed record GetCitizenMapWardReportsResponse(
    string WardCode,
    string? WardName,
    IReadOnlyList<CitizenMapWardReportPinDto> Items);
