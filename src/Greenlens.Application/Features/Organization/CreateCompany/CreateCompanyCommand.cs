using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Organization.CreateCompany;

/// <summary>
/// DEO creates a new Environmental Service Company with optional manager account and optional ward assignments.
/// </summary>
/// <remarks>
/// Implements: BR-CMP-001, BR-CMP-002.
/// ManagerEmail and ManagerFullName are optional — company can be created without a CM account.
/// When both are provided, credentials are emailed to the manager (BR-CMP-002, BR-NTF-002).
/// Use POST /v1/companies/{id}/manager to create the CM account later.
/// WardCodes (optional) assigns the ward service areas immediately at creation time.
/// </remarks>
public sealed record CreateCompanyCommand(
    string Name,
    Guid DepartmentId,
    string ContractNumber,
    DateTime ContractStartDate,
    DateTime? ContractEndDate,
    ContractType ContractType,
    string? TaxCode = null,
    string? Address = null,
    string? Phone = null,
    string? Email = null,
    string? ManagerEmail = null,
    string? ManagerFullName = null,
    List<string>? WardCodes = null) : IRequest<Result<CreateCompanyResponse>>;

public sealed record CreateCompanyResponse(
    Guid CompanyId,
    string CompanyName,
    string ContractNumber,
    string ContractType,
    string Status,
    Guid? ManagerUserId,
    string? ManagerEmail,
    string? TempPassword);
