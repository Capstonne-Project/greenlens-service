using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.RenewContract;

/// <summary>
/// BR-CMP-006: DEO/Admin gia hạn/tái ký hợp đồng Bidding.
/// Tạo kỳ hợp đồng mới, cập nhật metadata trên Company, auto-reactivate nếu Expired.
/// </summary>
/// <remarks>Implements: BR-ADM-010.</remarks>
public sealed record RenewContractCommand(
    Guid CompanyId,
    DateTime NewStartDate,
    DateTime NewEndDate,
    string NewContractNumber,
    string? Note = null) : IRequest<Result<RenewContractResponse>>, IAuditable
{
    string IAuditable.AuditEntityType => "Company";
    string? IAuditable.AuditEntityId => CompanyId.ToString();
}

public sealed record RenewContractResponse(
    Guid ContractPeriodId,
    string CompanyStatus);
