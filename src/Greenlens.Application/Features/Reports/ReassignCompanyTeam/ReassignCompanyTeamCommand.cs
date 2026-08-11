using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.ReassignCompanyTeam;

/// <summary>
/// CompanyManager reassigns a report to another company team after decline or before accept.
/// Report stays InProgress; old assignment remains Declined with reason visible in detail.
/// </summary>
public sealed record ReassignCompanyTeamCommand(
    Guid ReportId,
    Guid OldTeamId,
    Guid NewTeamId,
    string Reason) : IRequest<Result>;
