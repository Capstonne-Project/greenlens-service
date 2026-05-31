using Greenlens.Application.Common.Models;

namespace Greenlens.Application.Features.Organization.GetMyLocalOffices;

/// <summary>Response containing the department info and its local offices with pagination.</summary>
public sealed record GetMyLocalOfficesResponse(
    Guid DepartmentId,
    string DepartmentName,
    string ProvinceCode,
    IReadOnlyList<MyLocalOfficeItem> Offices,
    PaginationMeta Pagination);

/// <summary>One office row for the officer dashboard.</summary>
public sealed record MyLocalOfficeItem(
    Guid Id,
    string Name,
    string WardCode,
    string? WardName,
    Guid? OfficerId,
    string? OfficerName,
    bool IsOnboarded,
    int TeamCount,
    DateTime CreatedAt);
