using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.GetMyWardBoundary;

/// <summary>
/// Resolves the ward boundary for the LEO's own office from the JWT, for the officer map
/// to mask everything outside the ward polygon without needing provinceCode.
/// </summary>
/// <remarks>Implements: BR-ORG-004 (LocalOffice gắn WardCode → polygon GeoJSON).</remarks>
public sealed class GetMyWardBoundaryQueryHandler(
    IUserRepository users,
    IWardBoundaryLookupService boundaryLookup,
    ICurrentUser currentUser,
    ILogger<GetMyWardBoundaryQueryHandler> logger)
    : IRequestHandler<GetMyWardBoundaryQuery, Result<GetMyWardBoundaryResponse>>
{
    public async Task<Result<GetMyWardBoundaryResponse>> Handle(
        GetMyWardBoundaryQuery request,
        CancellationToken ct)
    {
        var officeInfo = await users.QueryAsNoTracking()
            .Where(u => u.Id == currentUser.UserId)
            .Select(u => new
            {
                HasOffice = u.LocalOfficeId.HasValue,
                WardCode = u.LocalOffice != null ? u.LocalOffice.WardCode : null,
                WardName = u.LocalOffice != null && u.LocalOffice.Ward != null
                    ? u.LocalOffice.Ward.Name : null
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (officeInfo is null || !officeInfo.HasOffice || officeInfo.WardCode is null)
        {
            logger.LogWarning("Office not found for user {UserId}", currentUser.UserId);
            return Errors.Organization.OfficeNotFound;
        }

        var geoJson = await boundaryLookup.GetWardBoundaryGeoJsonAsync(officeInfo.WardCode, ct)
            .ConfigureAwait(false);

        return new GetMyWardBoundaryResponse(officeInfo.WardCode, officeInfo.WardName, geoJson, BoundaryUrl: null);
    }
}
