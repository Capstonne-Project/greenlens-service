using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Organization.CreateCompany;

/// <summary>
/// DEO creates a new Environmental Service Company with contract info + manager account.
/// </summary>
/// <remarks>Implements: BR-CMP-001, BR-CMP-002.</remarks>
public sealed record CreateCompanyCommand(
    string Name,
    Guid DepartmentId,
    string ContractNumber,
    DateTime ContractStartDate,
    DateTime? ContractEndDate,
    ContractType ContractType,
    string ManagerEmail,
    string ManagerFullName,
    string? TaxCode = null,
    string? Address = null,
    string? Phone = null,
    string? Email = null) : IRequest<Result<CreateCompanyResponse>>;

public sealed record CreateCompanyResponse(
    Guid CompanyId,
    string CompanyName,
    string ContractNumber,
    string ContractType,
    string Status,
    Guid ManagerUserId,
    string ManagerEmail,
    string TempPassword);
