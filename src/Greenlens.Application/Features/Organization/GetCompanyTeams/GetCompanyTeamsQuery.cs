using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Organization.GetCompanyTeams;

/// <summary>
/// CompanyManager retrieves teams belonging to their company.
/// </summary>
/// <remarks>Implements: BR-CMP-004.</remarks>
public sealed record GetCompanyTeamsQuery(
    int Page = 1,
    int PageSize = 20,
    bool? IsActive = null) : IRequest<Result<GetCompanyTeamsResponse>>;

public sealed record GetCompanyTeamsResponse(
    IReadOnlyList<CompanyTeamItem> Items, PaginationMeta Pagination);

public sealed record CompanyTeamItem(
    Guid Id, string Name, TeamType TeamType, Guid LocalOfficeId,
    string? OfficeName, bool IsActive, int MemberCount, DateTime CreatedAt);
