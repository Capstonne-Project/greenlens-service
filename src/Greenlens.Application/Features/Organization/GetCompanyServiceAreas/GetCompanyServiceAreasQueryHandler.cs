using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Organization.GetCompanyServiceAreas;

/// <summary>
/// Returns the list of wards assigned to a company's service area.
/// </summary>
/// <remarks>Implements: BR-CMP-008.</remarks>
public sealed class GetCompanyServiceAreasQueryHandler(
    IEnvironmentalServiceCompanyRepository companies)
    : IRequestHandler<GetCompanyServiceAreasQuery, Result<GetCompanyServiceAreasResponse>>
{
    public async Task<Result<GetCompanyServiceAreasResponse>> Handle(
        GetCompanyServiceAreasQuery request,
        CancellationToken ct)
    {
        var company = await companies.QueryAsNoTracking()
            .Include(c => c.ServiceAreas)
                .ThenInclude(sa => sa.Ward!)
                    .ThenInclude(w => w.Province)
            .FirstOrDefaultAsync(c => c.Id == request.CompanyId, ct)
            .ConfigureAwait(false);

        if (company is null)
            return Errors.Organization.CompanyNotFound;

        var items = company.ServiceAreas
            .OrderBy(sa => sa.Ward?.Name)
            .Select(sa => new ServiceAreaItem(
                sa.Id,
                sa.WardCode,
                sa.Ward?.Name ?? sa.WardCode,
                sa.Ward?.ProvinceCode ?? "",
                sa.Ward?.Province?.Name ?? "",
                sa.CreatedAt))
            .ToList();

        return new GetCompanyServiceAreasResponse(company.Id, company.Name, items);
    }
}
