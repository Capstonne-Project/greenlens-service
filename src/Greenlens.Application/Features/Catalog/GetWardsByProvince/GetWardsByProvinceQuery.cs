using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Catalog.GetWardsByProvince;

/// <summary>List wards/communes for a single province (dependent dropdown).</summary>
public sealed record GetWardsByProvinceQuery(string ProvinceCode)
    : IRequest<Result<GetWardsByProvinceResponse>>;
