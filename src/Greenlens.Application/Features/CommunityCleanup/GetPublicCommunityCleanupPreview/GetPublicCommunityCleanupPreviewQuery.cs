using Greenlens.Application.Features.CommunityCleanup.Common;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.CommunityCleanup.GetPublicCommunityCleanupPreview;

public sealed record GetPublicCommunityCleanupPreviewQuery(Guid EventId)
    : IRequest<Result<CommunityCleanupPublicPreviewResponse>>;
