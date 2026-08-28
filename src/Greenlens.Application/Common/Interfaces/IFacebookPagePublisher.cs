using Greenlens.Domain.Common;

namespace Greenlens.Application.Common.Interfaces;

/// <summary>
/// Publishes photo posts to a connected Facebook Page via Meta Graph API.
/// </summary>
public interface IFacebookPagePublisher
{
    /// <summary>
    /// Creates a Page photo post from a public image URL and caption.
    /// </summary>
    /// <returns>Graph API post id (format: {pageId}_{postId}) when available.</returns>
    Task<Result<string>> PublishPhotoPostAsync(
        string caption,
        string imageUrl,
        CancellationToken ct = default);
}
