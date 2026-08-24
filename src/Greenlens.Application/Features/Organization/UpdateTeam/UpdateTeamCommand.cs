using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.UpdateTeam;

/// <remarks>Implements: BR-ORG-003, BR-CLN-005.</remarks>
public sealed record UpdateTeamCommand(
    Guid Id,
    string Name,
    List<Guid>? WasteTagIds) : IRequest<Result>;
