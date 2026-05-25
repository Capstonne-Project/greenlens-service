using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.AssignTeam;

/// <summary>
/// LEO assigns one or more teams to a verified report. BR-OFF-011, BR-ORG-013.
/// All teams are equal — no primary/secondary distinction.
/// </summary>
public sealed record AssignTeamCommand(
    Guid ReportId,
    List<TeamAssignmentItem> Teams) : IRequest<Result>;

public sealed record TeamAssignmentItem(
    Guid TeamId,
    string? Note);
