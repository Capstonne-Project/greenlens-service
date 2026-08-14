using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Catalog.GetProvinceBoundary;

/// <summary>
/// Looks up a province's boundary GeoJSON by province code, for the client map to draw the
/// province polygon.
/// </summary>
/// <remarks>Implements: BR-ORG-004 (LocalOffice gắn WardCode → polygon GeoJSON) — cùng cơ chế, cấp tỉnh.</remarks>
public sealed class GetProvinceBoundaryQueryHandler(
    IProvinceRepository provinces,
    IWardBoundaryLookupService boundaryLookup,
    ILogger<GetProvinceBoundaryQueryHandler> logger)
    : IRequestHandler<GetProvinceBoundaryQuery, Result<GetProvinceBoundaryResponse>>
{
    public async Task<Result<GetProvinceBoundaryResponse>> Handle(
        GetProvinceBoundaryQuery request,
        CancellationToken cancellationToken)
    {
        var code = request.ProvinceCode.Trim();

        var province = await provinces.GetByCodeAsync(code, cancellationToken).ConfigureAwait(false);
        if (province is null)
        {
            logger.LogWarning("Province not found for boundary lookup: {ProvinceCode}", code);
            return Errors.Catalog.ProvinceNotFound;
        }

        var geoJson = await boundaryLookup.GetProvinceBoundaryGeoJsonAsync(code, cancellationToken)
            .ConfigureAwait(false);

        return new GetProvinceBoundaryResponse(province.Code, province.Name, geoJson, BoundaryUrl: null);
    }
}
