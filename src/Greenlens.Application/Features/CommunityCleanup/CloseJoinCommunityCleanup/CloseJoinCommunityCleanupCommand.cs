using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.CommunityCleanup.CloseJoinCommunityCleanup;

/// <summary>LEO closes registration early. OpenForJoin → JoinClosed.</summary>
public sealed record CloseJoinCommunityCleanupCommand(Guid EventId) : IRequest<Result>;
