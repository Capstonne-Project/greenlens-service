using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.RenewContract;

/// <summary>
/// BR-CMP-006: Gia hạn/tái ký hợp đồng Bidding.
/// </summary>
public sealed class RenewContractCommandHandler(
    IEnvironmentalServiceCompanyRepository companies,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<RenewContractCommandHandler> logger)
    : IRequestHandler<RenewContractCommand, Result<RenewContractResponse>>
{
    public async Task<Result<RenewContractResponse>> Handle(
        RenewContractCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Renewing contract for company {CompanyId}", request.CompanyId);

        var company = await companies.GetByIdAsync(request.CompanyId, cancellationToken)
            .ConfigureAwait(false);

        if (company is null)
        {
            logger.LogWarning("Company {CompanyId} not found", request.CompanyId);
            return Errors.Organization.CompanyNotFound;
        }

        if (company.ContractType != ContractType.Bidding)
        {
            logger.LogWarning("Company {CompanyId} is not a bidding company", request.CompanyId);
            return Errors.Organization.SubsidiaryCannotRenew;
        }

        var newContractNumber = request.NewContractNumber.Trim();
        var contractExists = await companies.ContractNumberExistsAsync(
            newContractNumber,
            excludeCompanyId: company.Id,
            ct: cancellationToken).ConfigureAwait(false);

        if (contractExists)
        {
            logger.LogWarning("Contract number {ContractNumber} already used", newContractNumber);
            return Errors.Organization.ContractNumberAlreadyUsed;
        }

        var period = company.RenewContract(
            request.NewStartDate,
            request.NewEndDate,
            newContractNumber,
            currentUser.UserId,
            request.Note);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning("Error renewing contract for company {CompanyId}", request.CompanyId);
            var mapped = PostgresUniqueViolationMapper.TryMap(ex);
            if (mapped is not null)
                return mapped;
            throw;
        }

        logger.LogInformation(
            "BR-CMP-006: Contract renewed for company {CompanyId}. New period {PeriodId}. Status: {Status}",
            company.Id, period.Id, company.Status);

        return new RenewContractResponse(period.Id, company.Status.ToString());
    }
}
