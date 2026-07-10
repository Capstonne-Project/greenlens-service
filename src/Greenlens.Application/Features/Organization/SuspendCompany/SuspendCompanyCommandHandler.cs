using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.SuspendCompany;

/// <summary>
/// DEO/Admin suspends a company. Cascading: auto-decline active tasks,
/// revert reports to Verified, notify LEO.
/// </summary>
/// <remarks>Implements: BR-CMP-004 (state transition), BR-CMP-013 (cascading deactivation).</remarks>
public sealed class SuspendCompanyCommandHandler(
    IEnvironmentalServiceCompanyRepository companies,
    ICompanyCascadeService cascadeService,
    IUnitOfWork uow,
    ILogger<SuspendCompanyCommandHandler> logger) : IRequestHandler<SuspendCompanyCommand, Result>
{
    public async Task<Result> Handle(SuspendCompanyCommand request, CancellationToken ct)
    {
        var company = await companies.GetByIdAsync(request.CompanyId, ct).ConfigureAwait(false);
        if (company is null)
            return Errors.Organization.CompanyNotFound;

        if (company.Status != CompanyStatus.Active)
            return Errors.Organization.CompanyNotActive;

        // BR-CMP-004: Active → Suspended
        company.Suspend();

        // BR-CMP-013: cascade — decline assignments, revert reports, notify LEO
        await cascadeService.CascadeDeactivationAsync(
            request.CompanyId,
            $"Company suspended: {request.Reason}",
            ct).ConfigureAwait(false);

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogWarning("Company {CompanyId} ({CompanyName}) suspended. Reason: {Reason}",
            company.Id, company.Name, request.Reason);

        return Result.Success();
    }
}
