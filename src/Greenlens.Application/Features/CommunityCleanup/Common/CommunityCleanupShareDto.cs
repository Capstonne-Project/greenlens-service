namespace Greenlens.Application.Features.CommunityCleanup.Common;

/// <summary>
/// Pre-composed share payload for LEO Web (Next.js) success dialog and social share buttons.
/// </summary>
public sealed record CommunityCleanupShareDto(
    string Url,
    string Caption,
    string? ImageUrl,
    string FacebookShareUrl,
    string TwitterShareUrl,
    string LinkedInShareUrl,
    IReadOnlyList<string> Hashtags);
