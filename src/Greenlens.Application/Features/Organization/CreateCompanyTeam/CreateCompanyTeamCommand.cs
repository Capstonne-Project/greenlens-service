using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.CreateCompanyTeam;

/// <summary>
/// CompanyManager creates a CleanupTeam under their company.
/// No LocalOfficeId — company teams go where the task is.
/// InspectionTeam is NOT allowed — InspectionTeam is always ward-level (LEO-managed).
/// </summary>
/// <remarks>Implements: BR-CMP-004.</remarks>
public sealed record CreateCompanyTeamCommand(
    string Name) : IRequest<Result<CreateCompanyTeamResponse>>;

public sealed record CreateCompanyTeamResponse(
    Guid Id, string Name, Guid CompanyId, string TeamType);
