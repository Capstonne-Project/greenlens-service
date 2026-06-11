using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.CreateCompany;

/// <summary>
/// DEO creates a new Environmental Service Company under their department.
/// </summary>
/// <remarks>Implements: BR-CMP-001.</remarks>
public sealed class CreateCompanyCommandHandler(
    IEnvironmentalServiceCompanyRepository companies,
    IDepartmentRepository departments,
    IUnitOfWork uow,
    ILogger<CreateCompanyCommandHandler> logger)
    : IRequestHandler<CreateCompanyCommand, Result<CreateCompanyResponse>>
{
    public async Task<Result<CreateCompanyResponse>> Handle(
        CreateCompanyCommand request,
        CancellationToken ct)
    {
        // ── 1. Verify department exists ──
        var department = await departments.GetByIdAsync(request.DepartmentId, ct)
            .ConfigureAwait(false);

        if (department is null)
            return Errors.Organization.DepartmentNotFound;

        // ── 2. Check contract number uniqueness ──
        var contractExists = await companies.ExistsAsync(
            c => c.ContractNumber == request.ContractNumber, ct)
            .ConfigureAwait(false);

        if (contractExists)
            return Errors.Organization.CompanyContractNumberExists;

        // ── 3. Create entity ──
        var company = EnvironmentalServiceCompany.Create(
            request.Name,
            request.DepartmentId,
            request.ContractNumber,
            request.ContractStartDate,
            request.ContractEndDate,
            request.ContractType,
            request.TaxCode,
            request.Address,
            request.Phone,
            request.Email);

        companies.Add(company);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Company {CompanyId} '{Name}' created under department {DeptId} (Contract: {ContractNumber}, Type: {Type})",
            company.Id, company.Name, company.DepartmentId, company.ContractNumber, company.ContractType);

        return new CreateCompanyResponse(
            company.Id,
            company.Name,
            company.ContractNumber,
            company.ContractType.ToString(),
            company.Status.ToString());
    }
}
