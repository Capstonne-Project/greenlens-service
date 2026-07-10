using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.GetContractHistory;

/// <summary>
/// BR-CMP-006: Truy vấn lịch sử các kỳ hợp đồng của công ty.
/// DEO/Admin: any company. CM: own company only (BR-CMP-021).
/// </summary>
public sealed record GetContractHistoryQuery(
    Guid CompanyId) : IRequest<Result<ContractHistoryResponse>>;

public sealed record ContractHistoryResponse(
    Guid CompanyId,
    string CompanyName,
    List<ContractPeriodDto> Periods);

public sealed record ContractPeriodDto(
    Guid Id,
    string ContractNumber,
    string ContractType,
    DateTime StartDate,
    DateTime? EndDate,
    Guid RenewedByUserId,
    string? RenewedByName,
    string? Note,
    DateTime CreatedAt);
