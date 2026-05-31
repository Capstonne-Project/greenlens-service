using Greenlens.Application.Features.Catalog.GetWardsByProvince;

namespace Greenlens.Application.Features.Organization.GetMyWards;

/// <summary>Response containing the province info and its wards.</summary>
public sealed record GetMyWardsResponse(
    string ProvinceCode,
    string ProvinceName,
    IReadOnlyList<WardListItemDto> Wards);
