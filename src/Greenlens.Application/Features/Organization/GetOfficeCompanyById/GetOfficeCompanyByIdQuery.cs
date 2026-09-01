using Greenlens.Application.Features.Organization.GetCompanyById;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.GetOfficeCompanyById;

/// <summary>
/// LEO retrieves detail for a company serving their ward.
/// Scoped from the authenticated LEO account.
/// </summary>
/// <remarks>Implements: BR-CMP-001, BR-CMP-008, BR-ORG-003.</remarks>
public sealed record GetOfficeCompanyByIdQuery(Guid Id) : IRequest<Result<OfficeCompanyDetailResponse>>;

public sealed record OfficeCompanyDetailResponse(
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
    Guid LocalOfficeId,
    string LocalOfficeName,
    string WardCode,
    string WardName,
    CompanyServiceAreaDto WardServiceArea,
    IReadOnlyList<CompanyServiceAreaDto> AllServiceAreas,
    int StaffCount,
    int TeamCount,
    int ActiveReportCount,
    int CompletedReportCount,
    DateTime CreatedAt);
