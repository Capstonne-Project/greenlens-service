using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Organization.GetCompanyById;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.GetMyCompany;

/// <summary>
/// Resolves CM's companyId from token, then queries full company detail
/// with service areas and staff count — same projection as GetCompanyById.
/// </summary>
/// <remarks>Implements: BR-CMP-001.</remarks>
public sealed class GetMyCompanyQueryHandler(
    ICompanyStaffRepository companyStaff,
    IEnvironmentalServiceCompanyRepository companies,
    ICurrentUser currentUser,
    ILogger<GetMyCompanyQueryHandler> logger) : IRequestHandler<GetMyCompanyQuery, Result<CompanyDetailResponse>>
{
    public async Task<Result<CompanyDetailResponse>> Handle(
        GetMyCompanyQuery request,
        CancellationToken ct)
    {
        logger.LogInformation("Getting my company for user {UserId}", currentUser.UserId);

        // 1. Resolve CM's companyId
        var staff = await companyStaff.GetByUserIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (staff is null || !staff.IsActive)
        {
            logger.LogWarning("Company manager not found or inactive for user ID {UserId}", currentUser.UserId);
            return Errors.Organization.NotCompanyManager;
        }

        // 2. Query full company detail (same projection as GetCompanyById)
        var company = await companies.QueryAsNoTracking()
            .Include(c => c.Department)
            .Include(c => c.ServiceAreas)
                .ThenInclude(sa => sa.Ward)
            .Include(c => c.Staff)
            .FirstOrDefaultAsync(c => c.Id == staff.CompanyId, ct)
            .ConfigureAwait(false);

        if (company is null)
        {
            logger.LogWarning("Company {CompanyId} not found", staff.CompanyId);
            return Errors.Organization.CompanyNotFound;
        }

        var serviceAreas = company.ServiceAreas
            .OrderBy(sa => sa.Ward?.Name)
            .Select(sa => new CompanyServiceAreaDto(
                sa.Id,
                sa.WardCode,
                sa.Ward?.Name ?? sa.WardCode,
                sa.Ward?.ProvinceCode ?? ""))
            .ToList();

        logger.LogInformation("My company found: {CompanyId}, {Name}", company.Id, company.Name);

        return new CompanyDetailResponse(
            company.Id,
            company.Name,
            company.ContractNumber,
            company.ContractType.ToString(),
            company.Status.ToString(),
            company.ContractStartDate,
            company.ContractEndDate,
            company.TaxCode,
            company.Address,
            company.Phone,
            company.Email,
            company.DepartmentId,
            company.Department?.Name,
            company.ActivatedAt,
            serviceAreas,
            company.Staff.Count,
            company.CreatedAt);
    }
}
