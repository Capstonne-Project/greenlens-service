using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.GetMyInvitations;

/// <summary>BR-ORG-021: Citizen views their pending invitations.</summary>
public sealed record GetMyInvitationsQuery : IRequest<Result<List<InvitationDto>>>;
