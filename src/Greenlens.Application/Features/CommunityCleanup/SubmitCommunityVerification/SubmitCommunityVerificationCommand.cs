using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.CommunityCleanup.SubmitCommunityVerification;

/// <summary>Leader submits after-cleanup evidence for LEO review. Draft BR-CMU-009.</summary>
public sealed record SubmitCommunityVerificationCommand(
    Guid EventId,
    List<string> AfterImageUrls) : IRequest<Result>;
