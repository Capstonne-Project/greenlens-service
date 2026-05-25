using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Organization.GetTeams;

public sealed record GetTeamsQuery(
    int Page = 1, int PageSize = 20,
    Guid? LocalOfficeId = null,
    TeamType? TeamType = null,
    bool? IsActive = null) : IRequest<Result<GetTeamsResponse>>;

public sealed record GetTeamsResponse(
    IReadOnlyList<TeamItem> Items, int TotalCount, int Page, int PageSize);

public sealed record TeamItem(
    Guid Id, string Name, TeamType TeamType, Guid LocalOfficeId,
    string? OfficeName, bool IsActive, int MemberCount, DateTime CreatedAt);
