using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.GetDepartments;

public sealed record GetDepartmentsQuery(
    int Page = 1,
    int PageSize = 20,
    bool? IsActive = null) : IRequest<Result<GetDepartmentsResponse>>;

public sealed record GetDepartmentsResponse(
    IReadOnlyList<DepartmentItem> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record DepartmentItem(
    Guid Id,
    string Name,
    string ProvinceCode,
    string? ProvinceName,
    bool IsActive,
    int OfficeCount,
    DateTime CreatedAt);
