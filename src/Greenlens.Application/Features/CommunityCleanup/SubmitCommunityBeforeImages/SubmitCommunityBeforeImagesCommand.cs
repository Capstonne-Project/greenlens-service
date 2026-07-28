using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.CommunityCleanup.SubmitCommunityBeforeImages;

/// <summary>Leader persists "before" images already uploaded to R2 via presigned URL.</summary>
public sealed record SubmitCommunityBeforeImagesCommand(
    Guid EventId,
    List<string> ImageUrls) : IRequest<Result<SubmitCommunityBeforeImagesResponse>>;

public sealed record SubmitCommunityBeforeImagesResponse(List<string> SavedImageUrls);
