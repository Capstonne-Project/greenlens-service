using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.AcceptInvitation;

/// <summary>BR-ORG-021: Citizen accepts a staff invitation.</summary>
public sealed record AcceptInvitationCommand(Guid InvitationId) : IRequest<Result<AcceptInvitationResponse>>;
