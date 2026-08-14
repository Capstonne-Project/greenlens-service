using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Catalog.GetProvinceBoundary;

/// <summary>Looks up a province's boundary GeoJSON directly by province code.</summary>
public sealed record GetProvinceBoundaryQuery(string ProvinceCode)
    : IRequest<Result<GetProvinceBoundaryResponse>>;
