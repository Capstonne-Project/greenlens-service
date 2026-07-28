using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.CommunityCleanup.StartCommunityCleanup;

/// <summary>Leader starts the cleanup on-site. {OpenForJoin, JoinClosed} → InProgress.</summary>
public sealed record StartCommunityCleanupCommand(Guid EventId) : IRequest<Result>;
