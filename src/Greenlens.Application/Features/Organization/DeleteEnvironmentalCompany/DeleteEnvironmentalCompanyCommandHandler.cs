using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.DeleteEnvironmentalCompany;

/// <summary>DEO archives (soft-deletes) a company that is no longer operational.</summary>
/// <remarks>Implements: BR-CMP-004 (archive only when Terminated or staff-less PendingActivation).</remarks>
public sealed class DeleteEnvironmentalCompanyCommandHandler(
    IEnvironmentalServiceCompanyRepository companies,
    ICompanyStaffRepository companyStaff,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    ILogger<DeleteEnvironmentalCompanyCommandHandler> logger) : IRequestHandler<DeleteEnvironmentalCompanyCommand, Result>
{
    public async Task<Result> Handle(DeleteEnvironmentalCompanyCommand request, CancellationToken ct)
    {
        logger.LogInformation("Deleting environmental company {CompanyId}", request.Id);

        var company = await companies.GetByIdIncludingDeletedAsync(request.Id, ct).ConfigureAwait(false);
        if (company is null)
        {
            logger.LogWarning("Environmental company {CompanyId} not found", request.Id);
            return Errors.Organization.CompanyNotFound;
        }

        if (company.IsDeleted)
        {
            logger.LogWarning("Environmental company {CompanyId} already deleted", request.Id);
            return Errors.Organization.CompanyAlreadyDeleted;
        }

        var hasStaff = await companyStaff
            .ExistsAsync(s => s.CompanyId == company.Id, ct)
            .ConfigureAwait(false);

        if (hasStaff)
        {
            logger.LogWarning("Environmental company {CompanyId} has staff", request.Id);
            return Errors.Organization.CompanyMustTerminateFirst;
        }

        try
        {
            var priorStatus = company.Status;
            company.Archive(currentUser.UserId.ToString(), hasStaff);
            await uow.SaveChangesAsync(ct).ConfigureAwait(false);

            logger.LogInformation(
                "EnvironmentalServiceCompany {CompanyId} archived by {UserId} (prior status: {Status})",
                request.Id, currentUser.UserId, priorStatus);

            return Result.Success();
        }
        catch (DomainException)
        {
            logger.LogWarning("Failed to archive environmental company {CompanyId}", request.Id);
            return Errors.Organization.CompanyMustTerminateFirst;
        }
    }
}
