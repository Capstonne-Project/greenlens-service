namespace Greenlens.Application.Features.Catalog.GetWardsByProvince;

public sealed record GetWardsByProvinceResponse(IReadOnlyList<WardListItemDto> Items);
