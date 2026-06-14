using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.RemoveCompanyTeamMember;

/// <summary>
/// CompanyManager removes a member from a company team.
/// </summary>
/// <remarks>Implements: BR-CMP-004.</remarks>
public sealed record RemoveCompanyTeamMemberCommand(
    Guid TeamId,
    Guid UserId) : IRequest<Result>;
