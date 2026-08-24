using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Organization.GetTeamById;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Organization.GetCompanyTeamById;

/// <summary>CompanyManager retrieves one team belonging to their company.</summary>
/// <remarks>Implements: BR-CMP-004, BR-CLN-005.</remarks>
public sealed record GetCompanyTeamByIdQuery(Guid TeamId)
    : IRequest<Result<CompanyTeamDetailResponse>>;

public sealed record CompanyTeamDetailResponse(
    Guid Id,
    string Name,
    TeamType TeamType,
    Guid CompanyId,
    bool IsActive,
    int MemberCount,
    IReadOnlyList<MemberInTeam> Members,
    IReadOnlyList<WasteTagSummaryDto> WasteTags,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
