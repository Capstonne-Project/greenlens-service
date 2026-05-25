using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Organization.CreateTeam;

/// <summary>
/// Admin creates an Environmental Team (Cleanup or Inspection) under a LocalOffice.
/// </summary>
/// <remarks>Implements: BR-ORG-003, BR-ADM-011.</remarks>
public sealed record CreateTeamCommand(
    string Name,
    Guid LocalOfficeId,
    TeamType TeamType) : IRequest<Result<CreateTeamResponse>>;

public sealed record CreateTeamResponse(Guid Id, string Name, Guid LocalOfficeId, string TeamType);
