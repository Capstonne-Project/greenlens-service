using System.ComponentModel.DataAnnotations;

namespace Greenlens.Application.Common.Options;

/// <summary>
/// Public Next.js web base URL for community-cleanup share links and OG landing pages.
/// </summary>
public sealed class PublicWebOptions
{
    public const string SectionName = "PublicWeb";

    /// <summary>
    /// Origin only — no trailing slash. Used to build /c/community/{eventId} share URLs.
    /// </summary>
    [Required]
    [Url]
    public string BaseUrl { get; init; } = "http://localhost:3000";

    /// <summary>Path template relative to <see cref="BaseUrl"/>. Placeholder: {eventId}.</summary>
    public string CommunityCleanupPathTemplate { get; init; } = "/c/community/{eventId}";

    public string BuildCommunityCleanupUrl(Guid eventId)
    {
        var path = CommunityCleanupPathTemplate.Replace("{eventId}", eventId.ToString("D"), StringComparison.Ordinal);
        return $"{BaseUrl.TrimEnd('/')}{path}";
    }
}
