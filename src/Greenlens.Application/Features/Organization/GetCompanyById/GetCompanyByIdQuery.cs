using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.GetCompanyById;

/// <summary>
/// DEO/Admin retrieves company detail including service areas and staff count.
/// </summary>
/// <remarks>Implements: BR-CMP-001.</remarks>
public sealed record GetCompanyByIdQuery(Guid Id) : IRequest<Result<CompanyDetailResponse>>;

public sealed record CompanyDetailResponse(
    Guid Id,
    string Name,
    string ContractNumber,
    string ContractType,
    string Status,
    DateTime ContractStartDate,
    DateTime? ContractEndDate,
    string? TaxCode,
    string? Address,
    string? Phone,
    string? Email,
    Guid DepartmentId,
    string? DepartmentName,
    DateTime? ActivatedAt,
    IReadOnlyList<CompanyServiceAreaDto> ServiceAreas,
    int StaffCount,
    DateTime CreatedAt);

public sealed record CompanyServiceAreaDto(
    Guid Id,
    string WardCode,
    string WardName,
    string ProvinceCode);
