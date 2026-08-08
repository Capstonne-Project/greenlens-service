using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.CommunityCleanup.VerifyCommunityCleanup;

/// <summary>LEO approves the submitted evidence. PendingVerification → Completed, Report → Resolved. Draft BR-CMU-010.</summary>
public sealed record VerifyCommunityCleanupCommand(Guid EventId) : IRequest<Result>;
