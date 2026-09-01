using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Organization.GetOfficeCompanies;

/// <summary>
/// LEO retrieves companies whose service area covers the ward of their assigned local office.
/// Scoped automatically from the authenticated LEO account.
/// </summary>
/// <remarks>Implements: BR-CMP-008 (service area), BR-CMP-005 (operational status).</remarks>
public sealed record GetOfficeCompaniesQuery(
    int Page = 1,
    int PageSize = 20,
    CompanyStatus? Status = null,
    ContractType? ContractType = null,
    string? Search = null,
    string? SortBy = null,
    bool SortDesc = false) : IRequest<Result<GetOfficeCompaniesResponse>>;

public sealed record GetOfficeCompaniesResponse(
    Guid LocalOfficeId,
    string LocalOfficeName,
    string WardCode,
    string WardName,
    IReadOnlyList<OfficeCompanyItem> Items,
    PaginationMeta Pagination);

public sealed record OfficeCompanyItem(
    Guid Id,
    string Name,
    string ContractNumber,
    string ContractType,
    string Status,
    DateTime ContractStartDate,
    DateTime? ContractEndDate,
    string? TaxCode,
    string? Phone,
    string? Email,
    int ServiceAreaCount,
    int StaffCount,
    int ActiveReportCount,
    DateTime CreatedAt);
