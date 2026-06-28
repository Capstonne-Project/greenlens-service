using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Organization.GetOfficeCompanies;

/// <summary>
/// Returns active companies whose service area includes the office's ward.
/// Used by LEO dashboard to see which companies operate in their ward.
/// </summary>
/// <remarks>Implements: BR-CMP-005 (active check), BR-CMP-008 (service area match).</remarks>
public sealed class GetOfficeCompaniesQueryHandler(
    ILocalOfficeRepository offices,
    IEnvironmentalServiceCompanyRepository companies)
    : IRequestHandler<GetOfficeCompaniesQuery, Result<GetOfficeCompaniesResponse>>
{
    public async Task<Result<GetOfficeCompaniesResponse>> Handle(
        GetOfficeCompaniesQuery request, CancellationToken ct)
    {
        // 1. Load office to get WardCode
        var office = await offices.GetByIdAsync(request.OfficeId, ct).ConfigureAwait(false);
        if (office is null)
            return Errors.Organization.OfficeNotFound;

        // 2. Find active companies serving this ward
        var items = await companies.QueryAsNoTracking()
            .Include(c => c.ServiceAreas)
            .Include(c => c.Staff)
            .Where(c => c.Status == CompanyStatus.Active)
            .Where(c => c.ServiceAreas.Any(sa => sa.WardCode == office.WardCode))
            .OrderBy(c => c.Name)
            .Select(c => new OfficeCompanyItem(
                c.Id,
                c.Name,
                c.ContractNumber,
                c.ContractType.ToString(),
                c.Status.ToString(),
                c.Phone,
                c.Email,
                c.ServiceAreas.Count,
                c.Staff.Count))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new GetOfficeCompaniesResponse(items);
    }
}
