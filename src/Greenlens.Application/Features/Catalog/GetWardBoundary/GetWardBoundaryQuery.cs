using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Catalog.GetWardBoundary;

/// <summary>Looks up a ward's boundary GeoJSON URL directly by ward code (no province code required).</summary>
public sealed record GetWardBoundaryQuery(string WardCode)
    : IRequest<Result<GetWardBoundaryResponse>>;
