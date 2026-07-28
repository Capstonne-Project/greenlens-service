using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.CommunityCleanup.RejectCommunityVerification;

/// <summary>LEO rejects the submitted evidence. PendingVerification → InProgress. Draft BR-CMU-011.</summary>
public sealed record RejectCommunityVerificationCommand(Guid EventId, string Reason) : IRequest<Result>;
