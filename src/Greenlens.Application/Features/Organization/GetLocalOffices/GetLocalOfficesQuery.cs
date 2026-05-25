using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.GetLocalOffices;

public sealed record GetLocalOfficesQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? DepartmentId = null,
    bool? IsOnboarded = null) : IRequest<Result<GetLocalOfficesResponse>>;

public sealed record GetLocalOfficesResponse(
    IReadOnlyList<LocalOfficeItem> Items, int TotalCount, int Page, int PageSize);

public sealed record LocalOfficeItem(
    Guid Id, string Name, Guid DepartmentId, string? DepartmentName,
    string WardCode, string? WardName, Guid? OfficerId, string? OfficerName,
    bool IsOnboarded, int TeamCount, DateTime CreatedAt);
