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
    IReadOnlyList<OfficeInDepartment> Offices,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record OfficeInDepartment(
    Guid Id,
    string Name,
    string WardCode,
    string? WardName,
    Guid? OfficerId,
    string? OfficerName,
    bool IsOnboarded,
    int TeamCount);
