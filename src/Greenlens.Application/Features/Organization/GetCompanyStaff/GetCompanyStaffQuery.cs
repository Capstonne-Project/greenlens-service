using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.GetCompanyStaff;

/// <summary>
/// CM lists all staff in their company with pagination.
/// </summary>
/// <remarks>Implements: BR-CMP-004.</remarks>
public sealed record GetCompanyStaffQuery(
    int Page = 1,
    int PageSize = 20,
    bool? IsActive = null) : IRequest<Result<GetCompanyStaffResponse>>;

public sealed record GetCompanyStaffResponse(
    IReadOnlyList<CompanyStaffItem> Items,
    PaginationMeta Pagination);

public sealed record CompanyStaffItem(
    Guid UserId,
    string Email,
    string FullName,
    string? Position,
    bool IsActive,
    string? TeamName,
    Guid? TeamId,
    DateTime CreatedAt);
