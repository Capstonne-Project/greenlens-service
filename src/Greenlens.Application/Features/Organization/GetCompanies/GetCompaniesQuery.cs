using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;

namespace Greenlens.Application.Features.Organization.GetCompanies;

/// <summary>
/// DEO/Admin retrieves a paginated list of companies, optionally filtered.
/// </summary>
/// <remarks>Implements: BR-CMP-001 (company listing).</remarks>
public sealed record GetCompaniesQuery(
    int Page = 1,
    int PageSize = 20,
    CompanyStatus? Status = null,
    string? Search = null,
    string? SortBy = null,
    bool SortDesc = false) : IRequest<Result<GetCompaniesResponse>>;

public sealed record GetCompaniesResponse(
    IReadOnlyList<CompanyListItem> Items,
    PaginationMeta Pagination);

public sealed record CompanyListItem(
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
    DateTime CreatedAt);
