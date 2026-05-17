using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.ReassignTeam;

/// <summary>LEO reassigns report to a different team. BR-OFF-012.</summary>
public sealed record ReassignTeamCommand(
    Guid ReportId,
    Guid OldTeamId,
    Guid NewTeamId,
    string Reason) : IRequest<Result>;
