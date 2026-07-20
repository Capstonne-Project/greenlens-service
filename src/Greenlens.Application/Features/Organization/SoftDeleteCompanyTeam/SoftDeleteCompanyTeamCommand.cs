using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.SoftDeleteCompanyTeam;

/// <summary>
/// Soft-delete an EnvironmentalTeam (Company Team).
/// Only Admin can perform this.
/// </summary>
public sealed record SoftDeleteCompanyTeamCommand(Guid TeamId) : IRequest<Result>;
