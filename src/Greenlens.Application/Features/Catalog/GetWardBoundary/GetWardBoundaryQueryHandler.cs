using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Catalog.GetWardBoundary;

/// <summary>
/// Looks up a ward's boundary URL by ward code, for the LEO map to draw the office's ward polygon
/// without knowing the province code up front.
/// </summary>
public sealed class GetWardBoundaryQueryHandler(
    IWardRepository wards,
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

        return new GetWardBoundaryResponse(ward.Code, ward.BoundaryUrl);
    }
}
