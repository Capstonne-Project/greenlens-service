using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Organization.CreateCompanyTeam;

/// <summary>
/// CompanyManager creates a CleanupTeam under their company.
/// InspectionTeam is NOT allowed — InspectionTeam is always ward-level (LEO-managed).
/// </summary>
/// <remarks>Implements: BR-CMP-004.</remarks>
public sealed record CreateCompanyTeamCommand(
    string Name,
    Guid LocalOfficeId) : IRequest<Result<CreateCompanyTeamResponse>>;

public sealed record CreateCompanyTeamResponse(
    Guid Id, string Name, Guid LocalOfficeId, Guid CompanyId, string TeamType);
