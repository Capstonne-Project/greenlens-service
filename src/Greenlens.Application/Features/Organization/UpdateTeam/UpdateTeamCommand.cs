using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.UpdateTeam;

public sealed record UpdateTeamCommand(Guid Id, string Name) : IRequest<Result>;
