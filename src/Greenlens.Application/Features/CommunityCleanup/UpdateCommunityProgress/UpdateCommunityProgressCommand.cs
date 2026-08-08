using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.CommunityCleanup.UpdateCommunityProgress;

/// <summary>Leader updates cleanup progress with optional R2 image URLs. Draft BR-CMU-008.</summary>
public sealed record UpdateCommunityProgressCommand(
    Guid EventId,
    int ProgressPercent,
    string? ProgressNote,
    List<string> ImageUrls) : IRequest<Result<UpdateCommunityProgressResponse>>;

public sealed record UpdateCommunityProgressResponse(List<string> SavedImageUrls);
