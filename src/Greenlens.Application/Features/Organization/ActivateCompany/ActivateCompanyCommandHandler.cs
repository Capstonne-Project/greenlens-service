using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.ActivateCompany;

/// <summary>
/// DEO activates a company after CM has completed onboarding (set password via reset-password flow).
/// </summary>
/// <remarks>Implements: BR-CMP-003.</remarks>
public sealed class ActivateCompanyCommandHandler(
    IEnvironmentalServiceCompanyRepository companies,
    IUnitOfWork uow,
    ILogger<ActivateCompanyCommandHandler> logger)
    : IRequestHandler<ActivateCompanyCommand, Result>
{
    public async Task<Result> Handle(ActivateCompanyCommand request, CancellationToken ct)
    {
        var company = await companies.GetByIdAsync(request.CompanyId, ct)
            .ConfigureAwait(false);

        if (company is null)
            return Errors.Organization.CompanyNotFound;

        if (company.Status != CompanyStatus.PendingActivation)
            return Errors.Organization.CompanyNotPendingActivation;

        company.Activate();

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Company {CompanyId} '{Name}' activated by DEO",
            company.Id, company.Name);

        return Result.Success();
    }
}
