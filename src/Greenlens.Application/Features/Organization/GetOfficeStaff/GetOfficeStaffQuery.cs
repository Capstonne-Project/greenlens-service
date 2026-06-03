using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Organization.GetOfficeStaff;

/// <summary>
/// LEO views staff (Cleaner/Inspector) in their LocalOffice.
/// Supports search, filter by role, filter by team assignment status.
/// </summary>
public sealed record GetOfficeStaffQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    UserRole? RoleFilter = null,
    bool? HasTeam = null) : IRequest<Result<GetOfficeStaffResponse>>;

public sealed record GetOfficeStaffResponse(
    IReadOnlyList<OfficeStaffItem> Items,
    PaginationMeta Pagination);

public sealed record OfficeStaffItem(
    Guid UserId,
    string FullName,
    string Email,
    string? PhoneNumber,
    string? AvatarUrl,
    UserRole Role,
    Guid? TeamId,
    string? TeamName,
    bool IsLeader,
    DateTime CreatedAt);
