using System.Globalization;

namespace Greenlens.Application.Common;

/// <summary>
/// Builds public Facebook post permalinks and display labels from Graph API post ids.
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

    public static string BuildShareLabel(string title)
    {
        var vi = CultureInfo.GetCultureInfo("vi-VN");
        return $"Greenlens – 🌱 THÔNG TIN THAM GIA {title.ToUpper(vi)} CÙNG GREENLENS 🌱";
    }
}
