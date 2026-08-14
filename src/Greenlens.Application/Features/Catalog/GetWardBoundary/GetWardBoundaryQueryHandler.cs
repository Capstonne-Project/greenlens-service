using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Catalog.GetWardBoundary;

/// <summary>
/// Looks up a ward's boundary GeoJSON by ward code, for the LEO map to draw the office's ward
/// polygon without knowing the province code up front.
/// </summary>
/// <remarks>Implements: BR-ORG-004 (LocalOffice gắn WardCode → polygon GeoJSON).</remarks>
public sealed class GetWardBoundaryQueryHandler(
    IWardRepository wards,
    IWardBoundaryLookupService boundaryLookup,
    ILogger<GetWardBoundaryQueryHandler> logger)
    : IRequestHandler<GetWardBoundaryQuery, Result<GetWardBoundaryResponse>>
{
    public async Task<Result<GetWardBoundaryResponse>> Handle(
        GetWardBoundaryQuery request,
        CancellationToken cancellationToken)
    {
        var code = request.WardCode.Trim();

        var ward = await wards.GetByCodeAsync(code, cancellationToken).ConfigureAwait(false);
        if (ward is null)
        {
            logger.LogWarning("Ward not found for boundary lookup: {WardCode}", code);
            return Errors.Catalog.WardNotFound;
        }

        var geoJson = await boundaryLookup.GetWardBoundaryGeoJsonAsync(code, cancellationToken)
            .ConfigureAwait(false);

        return new GetWardBoundaryResponse(ward.Code, ward.Name, geoJson, BoundaryUrl: null);
    }
}
