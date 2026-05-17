using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Organization.GetLocalOfficeById;

public sealed record GetLocalOfficeByIdQuery(Guid Id) : IRequest<Result<LocalOfficeDetailResponse>>;

public sealed record LocalOfficeDetailResponse(
    Guid Id, string Name, Guid DepartmentId, string? DepartmentName,
    string WardCode, string? WardName, Guid? OfficerId, string? OfficerName,
    bool IsOnboarded, IReadOnlyList<TeamInOffice> Teams,
    DateTime CreatedAt, DateTime? UpdatedAt);

public sealed record TeamInOffice(
    Guid Id, string Name, TeamType TeamType, bool IsActive, int MemberCount);
