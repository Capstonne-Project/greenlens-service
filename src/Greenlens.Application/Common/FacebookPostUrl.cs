namespace Greenlens.Application.Common;

/// <summary>
/// Builds public Facebook post permalinks from Graph API post ids ({pageId}_{storyId}).
/// </summary>
internal static class FacebookPostUrl
{
    public static string FromPostId(string postId)
    {
        var underscore = postId.IndexOf('_');
        if (underscore > 0 && underscore < postId.Length - 1)
        {
            var pageId = postId[..underscore];
            var storyId = postId[(underscore + 1)..];
            return $"https://www.facebook.com/{pageId}/posts/{storyId}";
        }

        return $"https://www.facebook.com/{postId}";
    }
}
