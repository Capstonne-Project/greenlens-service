using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Catalog.GetWardsByProvince;

/// <summary>
/// Loads wards for the selected province code after validation.
/// </summary>
/// <remarks>
/// Implements: BR-REP-003 (structured Vietnam administrative codes on reports).
/// </remarks>
public sealed class GetWardsByProvinceQueryHandler(
    IProvinceRepository provinces,
    IWardRepository wards)
    : IRequestHandler<GetWardsByProvinceQuery, Result<GetWardsByProvinceResponse>>
{
    public async Task<Result<GetWardsByProvinceResponse>> Handle(
        GetWardsByProvinceQuery request,
        CancellationToken cancellationToken)
    {
        var code = request.ProvinceCode.Trim();
        var provinceExists = await provinces.ExistsAsync(p => p.Code == code, cancellationToken)
            .ConfigureAwait(false);
        if (!provinceExists)
            return Errors.Catalog.ProvinceNotFound;

        var items = await wards.QueryAsNoTracking()
            .Where(w => w.ProvinceCode == code)
            .OrderBy(w => w.Name)
            .Select(w => new WardListItemDto(
                w.Code,
                w.Name,
                w.AdministrativeUnit!.Abbreviation,
                w.BoundaryUrl))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new GetWardsByProvinceResponse(items);
    }
}
