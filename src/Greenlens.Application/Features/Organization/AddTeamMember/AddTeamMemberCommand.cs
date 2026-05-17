using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.AddTeamMember;

/// <summary>
/// Admin adds a user to an Environmental Team.
/// </summary>
/// <remarks>Implements: BR-ORG-003.</remarks>
public sealed record AddTeamMemberCommand(
    Guid TeamId,
    Guid UserId,
    bool IsLeader = false) : IRequest<Result<AddTeamMemberResponse>>;

public sealed record AddTeamMemberResponse(Guid Id, Guid TeamId, Guid UserId, bool IsLeader);
