using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.ToggleCompanyTeamStatus;

/// <summary>
/// CompanyManager toggles active status of a team belonging to their company.
/// </summary>
/// <remarks>Implements: BR-CMP-004.</remarks>
public sealed record ToggleCompanyTeamStatusCommand(Guid TeamId, bool IsActive) : IRequest<Result>;
