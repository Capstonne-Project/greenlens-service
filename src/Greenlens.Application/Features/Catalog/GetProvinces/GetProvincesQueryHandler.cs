using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Catalog.GetProvinces;

/// <summary>
/// Returns the national province catalog for dependent ward selection.
/// </summary>
/// <remarks>
/// Supports address capture aligned with <see cref="Greenlens.Domain.Entities.Report"/>'s
/// <c>ProvinceCode</c> / <c>WardCode</c> (BR-REP-003 location metadata).
/// Includes <c>BoundaryUrl</c> for client map overlays when seeded.
/// </remarks>
public sealed class GetProvincesQueryHandler(IProvinceRepository provinces)
    : IRequestHandler<GetProvincesQuery, Result<GetProvincesResponse>>
{
    public async Task<Result<GetProvincesResponse>> Handle(
        GetProvincesQuery request,
        CancellationToken cancellationToken)
    {
        var items = await provinces.QueryAsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new ProvinceListItemDto(p.Code, p.Name, p.BoundaryUrl))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new GetProvincesResponse(items);
    }
}
