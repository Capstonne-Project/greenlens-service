using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Catalog.GetWardsByProvince;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.GetMyWards;

/// <summary>
/// Returns wards under the province managed by the current officer's department.
/// DEO → User.DepartmentId → Department.ProvinceCode → Wards.
/// LEO → User.LocalOfficeId → LocalOffice.DepartmentId → Department.ProvinceCode → Wards.
/// </summary>
/// <remarks>Implements: BR-ORG-001 (department ↔ province), BR-ORG-002 (office ↔ ward).</remarks>
public sealed class GetMyWardsQueryHandler(
    IUserRepository users,
    IProvinceRepository provinces,
    IWardRepository wards,
    ICurrentUser currentUser,
    ILogger<GetMyWardsQueryHandler> logger)
    : IRequestHandler<GetMyWardsQuery, Result<GetMyWardsResponse>>
{
    public async Task<Result<GetMyWardsResponse>> Handle(
        GetMyWardsQuery request,
        CancellationToken ct)
    {
        // 1. Resolve the officer's province code via Department chain
        var provinceCode = await users.QueryAsNoTracking()
            .Where(u => u.Id == currentUser.UserId)
            .Select(u =>
                u.DepartmentId != null
                    ? u.Department!.ProvinceCode                  // DEO
                    : u.LocalOffice!.Department!.ProvinceCode)    // LEO
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (string.IsNullOrEmpty(provinceCode))
            return Errors.Organization.DepartmentNotFound;

        // 2. Get province name
        var province = await provinces.GetByCodeAsync(provinceCode, ct).ConfigureAwait(false);
        var provinceName = province?.Name ?? provinceCode;

        // 3. Get all wards in the province
        var wardItems = await wards.QueryAsNoTracking()
            .Where(w => w.ProvinceCode == provinceCode)
            .OrderBy(w => w.Name)
            .Select(w => new WardListItemDto(
                w.Code,
                w.Name,
                w.AdministrativeUnit!.Abbreviation,
                w.BoundaryUrl))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        logger.LogInformation(
            "Officer {UserId} fetched {Count} wards for province {ProvinceCode}",
            currentUser.UserId, wardItems.Count, provinceCode);

        return new GetMyWardsResponse(provinceCode, provinceName, wardItems);
    }
}
