using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.GetOfficeCompanies;

/// <summary>
/// Resolves LEO's local office by ICurrentUser, then returns active companies
/// whose service area matches the office's ward.
/// Used on LEO dashboard to see which companies operate in their ward.
/// </summary>
/// <remarks>Implements: BR-CMP-005 (active check), BR-CMP-008 (service area match).</remarks>
public sealed class GetOfficeCompaniesQueryHandler(
    ICurrentUser currentUser,
    ILocalOfficeRepository offices,
    IEnvironmentalServiceCompanyRepository companies,
    ILogger<GetOfficeCompaniesQueryHandler> logger)
    : IRequestHandler<GetOfficeCompaniesQuery, Result<GetOfficeCompaniesResponse>>
{
    public async Task<Result<GetOfficeCompaniesResponse>> Handle(
        GetOfficeCompaniesQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting office companies for user {UserId}", currentUser.UserId);

        // 1. Find LEO's assigned office
        var office = await offices.QueryAsNoTracking()
            .FirstOrDefaultAsync(o => o.OfficerId == currentUser.UserId, ct)
            .ConfigureAwait(false);

        if (office is null)
        {
            logger.LogWarning("Office not found for user ID {UserId}", currentUser.UserId);
            return Errors.Organization.OfficeNotFound;
        }

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

        logger.LogInformation("Office companies found: {Count}", items.Count);

        return new GetOfficeCompaniesResponse(items);
    }
}
