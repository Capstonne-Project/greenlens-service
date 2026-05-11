namespace Greenlens.Application.Features.Catalog.GetProvinces;

public sealed record GetProvincesResponse(IReadOnlyList<ProvinceListItemDto> Items);
