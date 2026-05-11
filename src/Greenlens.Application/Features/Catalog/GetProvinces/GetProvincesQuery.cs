using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Catalog.GetProvinces;

/// <summary>List all provinces / centrally governed cities for address dropdowns.</summary>
public sealed record GetProvincesQuery : IRequest<Result<GetProvincesResponse>>;
