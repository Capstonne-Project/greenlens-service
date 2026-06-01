using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.GetDepartmentById;

public sealed record GetDepartmentByIdQuery(Guid Id) : IRequest<Result<DepartmentDetailResponse>>;

public sealed record DepartmentDetailResponse(
    Guid Id,
    string Name,
    string ProvinceCode,
    string? ProvinceName,
    bool IsActive,
    DeoOfficerInfo? Deo,
    IReadOnlyList<OfficeInDepartment> Offices,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

/// <summary>Thông tin DEO đang phụ trách department.</summary>
public sealed record DeoOfficerInfo(
    Guid Id,
    string FullName,
    string Email,
    string? PhoneNumber,
    string? AvatarUrl);

public sealed record OfficeInDepartment(
    Guid Id,
    string Name,
    string WardCode,
    string? WardName,
    Guid? OfficerId,
    string? OfficerName,
    bool IsOnboarded,
    int TeamCount);
