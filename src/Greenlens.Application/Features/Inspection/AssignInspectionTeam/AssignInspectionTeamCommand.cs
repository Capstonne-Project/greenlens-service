using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Inspection.AssignInspectionTeam;

/// <summary>
/// LEO assigns an Inspector Team to an existing InspectionReport (Draft).
/// BR-INS-001, BR-OFF-005.
/// </summary>
public sealed record AssignInspectionTeamCommand(
    Guid InspectionId,
    Guid TeamId) : IRequest<Result>;
