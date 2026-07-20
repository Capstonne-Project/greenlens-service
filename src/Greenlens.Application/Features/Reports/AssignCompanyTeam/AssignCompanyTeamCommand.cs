using Greenlens.Application.Features.Reports.AssignTeam;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.AssignCompanyTeam;

/// <summary>
/// CompanyManager assigns company team(s) to a report that was dispatched to their company.
/// Transitions report Verified → InProgress.
/// </summary>
public sealed record AssignCompanyTeamCommand(
    Guid ReportId,
    List<TeamAssignmentItem> Teams) : IRequest<Result>;
