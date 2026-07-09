using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.RenewContract;

/// <summary>
/// BR-CMP-006: DEO/Admin gia hạn/tái ký hợp đồng Bidding.
/// Tạo kỳ hợp đồng mới, cập nhật metadata trên Company, auto-reactivate nếu Expired.
/// </summary>
public sealed record RenewContractCommand(
    Guid CompanyId,
    DateTime NewStartDate,
    DateTime NewEndDate,
    string NewContractNumber,
    string? Note = null) : IRequest<Result<RenewContractResponse>>;

public sealed record RenewContractResponse(
    Guid ContractPeriodId,
    string CompanyStatus);
