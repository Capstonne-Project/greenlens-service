using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.DeleteCompanyTeam;

/// <summary>
/// CompanyManager deactivates (soft-deletes) a team belonging to their company.
/// </summary>
/// <remarks>Implements: BR-CMP-004.</remarks>
public sealed record DeleteCompanyTeamCommand(Guid TeamId) : IRequest<Result>;
