using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.DeclineInvitation;

/// <summary>BR-ORG-021: Citizen declines a staff invitation.</summary>
public sealed record DeclineInvitationCommand(Guid InvitationId) : IRequest<Result>;
