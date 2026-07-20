using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.RenewContract;

/// <summary>
/// BR-CMP-006: Gia hạn/tái ký hợp đồng Bidding.
/// - Chỉ áp dụng cho Bidding (Subsidiary vô thời hạn).
/// - Tạo ContractPeriod mới để lưu lịch sử.
/// - Cập nhật ContractEndDate, ContractNumber trên Company.
/// - Auto-reactivate nếu Company đang Expired.
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
        var company = await companies.GetByIdAsync(request.CompanyId, cancellationToken)
            .ConfigureAwait(false);

        if (company is null)
            return Errors.Organization.CompanyNotFound;

        if (company.ContractType != ContractType.Bidding)
            return Errors.Organization.SubsidiaryCannotRenew;

        // Domain method handles all invariants + creates ContractPeriod
        var period = company.RenewContract(
            request.NewStartDate,
            request.NewEndDate,
            request.NewContractNumber,
            currentUser.UserId,
            request.Note);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "BR-CMP-006: Contract renewed for company {CompanyId}. " +
            "New period {PeriodId}: {Start} → {End}. Status: {Status}",
            company.Id, period.Id,
            request.NewStartDate, request.NewEndDate,
            company.Status);

        return new RenewContractResponse(period.Id, company.Status.ToString());
    }
}
