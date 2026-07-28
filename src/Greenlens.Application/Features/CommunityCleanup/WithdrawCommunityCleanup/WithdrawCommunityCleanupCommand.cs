using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.CommunityCleanup.WithdrawCommunityCleanup;

/// <summary>Citizen withdraws before check-in. Draft BR-CMU-006.</summary>
public sealed record WithdrawCommunityCleanupCommand(Guid EventId) : IRequest<Result>;
