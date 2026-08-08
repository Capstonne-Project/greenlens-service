using System.Text.Json;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.TerminateCompany;

/// <summary>
/// DEO/Admin terminates a company contract early. Cascading: auto-decline active tasks,
/// revert reports to Verified, notify LEO.
/// </summary>
/// <remarks>Implements: BR-CMP-004 (state transition), BR-CMP-013 (cascading deactivation).</remarks>
public sealed class TerminateCompanyCommandHandler(
    IEnvironmentalServiceCompanyRepository companies,
    ICompanyCascadeService cascadeService,
    IUnitOfWork uow,
    IAuditLogger auditLogger,
    ILogger<TerminateCompanyCommandHandler> logger) : IRequestHandler<TerminateCompanyCommand, Result>
{
    public async Task<Result> Handle(TerminateCompanyCommand request, CancellationToken ct)
    {
        logger.LogInformation("Terminating company {CompanyId}", request.CompanyId);

        var company = await companies.GetByIdAsync(request.CompanyId, ct).ConfigureAwait(false);
        if (company is null)
        {
            logger.LogWarning("Company {CompanyId} not found", request.CompanyId);
            return Errors.Organization.CompanyNotFound;
        }

        // BR-CMP-004: Cannot terminate from PendingActivation or already Terminated
        if (company.Status is CompanyStatus.Terminated or CompanyStatus.PendingActivation)
        {
            logger.LogWarning("Company {CompanyId} cannot be terminated", request.CompanyId);
            return Errors.Organization.CompanyCannotTerminate;
        }

        var oldSnapshot = JsonSerializer.Serialize(new { status = company.Status.ToString() });

        // BR-CMP-004: → Terminated
        company.Terminate();

        // BR-CMP-013: cascade — decline assignments, revert reports, notify LEO
        await cascadeService.CascadeDeactivationAsync(
            request.CompanyId,
            $"Company terminated: {request.Reason}",
            ct).ConfigureAwait(false);

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        await auditLogger.LogAsync(
            "TerminateCompany",
            "Company",
            company.Id.ToString(),
            oldValues: oldSnapshot,
            newValues: JsonSerializer.Serialize(new
            {
                status = company.Status.ToString(),
                reasonLength = request.Reason.Length
            }),
            ct).ConfigureAwait(false);

        logger.LogWarning("Company {CompanyId} ({CompanyName}) terminated. Reason: {Reason}",
            company.Id, company.Name, request.Reason);

        return Result.Success();
    }
}
