using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.TransferTeamMember;

/// <summary>
/// LEO transfers a member from their current team to another team
/// within the same LocalOffice. Atomic: remove + add in 1 transaction.
/// </summary>
/// <remarks>Implements: BR-ORG-006 (team transfer).</remarks>
public sealed record TransferTeamMemberCommand(
    Guid CurrentTeamId,
    Guid UserId,
    Guid NewTeamId,
    bool IsLeader = false) : IRequest<Result<TransferTeamMemberResponse>>;

public sealed record TransferTeamMemberResponse(
    Guid UserId,
    Guid OldTeamId,
    Guid NewTeamId,
    Guid NewTeamMemberId,
    bool IsLeader);
