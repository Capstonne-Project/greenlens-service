namespace Greenlens.Application.Features.CitizenMap.GetCitizenMapWards;

public sealed record GetCitizenMapWardsResponse(
    string ProvinceCode,
    string? ProvinceName,
    IReadOnlyList<CitizenMapWardDto> Items);
