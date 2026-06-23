using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.AddCompanyTeamMember;

/// <summary>
/// CompanyManager adds a CompanyStaff member to a company team.
/// </summary>
/// <remarks>Implements: BR-CMP-004.</remarks>
public sealed record AddCompanyTeamMemberCommand(
    Guid TeamId,
    Guid UserId,
    bool IsLeader = false) : IRequest<Result<AddCompanyTeamMemberResponse>>;

public sealed record AddCompanyTeamMemberResponse(
    Guid MemberId,
    Guid TeamId,
    Guid UserId,
    bool IsLeader);
