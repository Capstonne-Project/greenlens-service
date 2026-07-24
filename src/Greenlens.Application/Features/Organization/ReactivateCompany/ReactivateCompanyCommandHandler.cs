using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.ReactivateCompany;

/// <summary>
/// DEO/Admin reactivates a suspended company. No cascading needed — company simply becomes Active again.
/// </summary>
/// <remarks>Implements: BR-CMP-004 (Suspended → Active).</remarks>
public sealed class ReactivateCompanyCommandHandler(
    IEnvironmentalServiceCompanyRepository companies,
    IUnitOfWork uow,
    ILogger<ReactivateCompanyCommandHandler> logger) : IRequestHandler<ReactivateCompanyCommand, Result>
{
    public async Task<Result> Handle(ReactivateCompanyCommand request, CancellationToken ct)
    {
        logger.LogInformation("Reactivating company {CompanyId}", request.CompanyId);

        var company = await companies.GetByIdAsync(request.CompanyId, ct).ConfigureAwait(false);
        if (company is null)
        {
            logger.LogWarning("Company {CompanyId} not found", request.CompanyId);
            return Errors.Organization.CompanyNotFound;
        }

        if (company.Status != CompanyStatus.Suspended)
        {
            logger.LogWarning("Company {CompanyId} is not suspended", request.CompanyId);
            return Errors.Organization.CompanyNotSuspended;
        }

        // BR-CMP-004: Suspended → Active
        company.Reactivate();

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Company {CompanyId} ({CompanyName}) reactivated",
            company.Id, company.Name);

        return Result.Success();
    }
}
