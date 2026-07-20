using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.UpdateCompanyTeam;

/// <summary>
/// CompanyManager renames a team belonging to their company.
/// </summary>
/// <remarks>Implements: BR-CMP-004.</remarks>
public sealed record UpdateCompanyTeamCommand(
    Guid TeamId,
    string Name) : IRequest<Result>;
