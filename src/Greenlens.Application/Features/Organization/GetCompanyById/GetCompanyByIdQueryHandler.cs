using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.GetCompanyById;

/// <summary>
/// Returns full company detail with service areas and staff count.
/// </summary>
public sealed class GetCompanyByIdQueryHandler(
    IEnvironmentalServiceCompanyRepository companies,
    ILogger<GetCompanyByIdQueryHandler> logger)
    : IRequestHandler<GetCompanyByIdQuery, Result<CompanyDetailResponse>>
{
    public async Task<Result<CompanyDetailResponse>> Handle(
        GetCompanyByIdQuery request,
        CancellationToken ct)
    {
        logger.LogInformation("Getting company by ID {Id}", request.Id);

        var company = await companies.QueryAsNoTracking()
            .Include(c => c.Department)
            .Include(c => c.ServiceAreas)
                .ThenInclude(sa => sa.Ward)
            .Include(c => c.Staff)
            .FirstOrDefaultAsync(c => c.Id == request.Id, ct)
            .ConfigureAwait(false);

        if (company is null)
        {
            logger.LogWarning("Company {Id} not found", request.Id);
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

        logger.LogInformation("Company {Id} found", request.Id);
        logger.LogInformation("Service areas: {ServiceAreas}", serviceAreas);
        logger.LogInformation("Staff count: {StaffCount}", company.Staff.Count);
        logger.LogInformation("Created at: {CreatedAt}", company.CreatedAt);

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
