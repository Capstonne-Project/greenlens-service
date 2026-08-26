using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Organization.GetTeamById;

public sealed record GetTeamByIdQuery(Guid Id) : IRequest<Result<TeamDetailResponse>>;

public sealed record TeamDetailResponse(
    Guid Id, string Name, TeamType TeamType, Guid? LocalOfficeId,
    string? OfficeName, bool IsActive,
    IReadOnlyList<MemberInTeam> Members,
    IReadOnlyList<WasteTagSummaryDto> WasteTags,
    DateTime CreatedAt, DateTime? UpdatedAt);

public sealed record MemberInTeam(
    Guid UserId, string? FullName, string? Email, string? PhoneNumber,
    string? AvatarUrl, bool IsLeader, DateTime JoinedAt);
