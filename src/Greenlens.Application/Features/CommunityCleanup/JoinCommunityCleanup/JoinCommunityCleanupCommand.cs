using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.CommunityCleanup.JoinCommunityCleanup;

/// <summary>Citizen Joins (= Votes) an open program. Draft BR-CMU-004, BR-CMU-005.</summary>
public sealed record JoinCommunityCleanupCommand(Guid EventId) : IRequest<Result>;
