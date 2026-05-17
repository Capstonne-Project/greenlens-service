using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.RemoveTeamMember;

public sealed record RemoveTeamMemberCommand(Guid TeamId, Guid UserId) : IRequest<Result>;
