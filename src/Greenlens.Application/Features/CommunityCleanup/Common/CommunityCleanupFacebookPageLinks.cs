using Greenlens.Application.Common;
using Greenlens.Domain.Entities;

namespace Greenlens.Application.Features.CommunityCleanup.Common;

internal static class CommunityCleanupFacebookPageLinks
{
    public static CommunityCleanupFacebookPageLinkDto? FromEvent(CommunityCleanupEvent ev)
    {
        if (string.IsNullOrWhiteSpace(ev.FacebookPostId) || string.IsNullOrWhiteSpace(ev.FacebookPageUrl))
            return null;

        return new CommunityCleanupFacebookPageLinkDto(
            ev.FacebookPageUrl,
            FacebookPostUrl.BuildShareLabel(ev.Title),
            ev.FacebookPageSharedAt);
    }

    public static CommunityCleanupFacebookPageLinkDto FromPost(CommunityCleanupEvent ev, string postId)
    {
        var href = FacebookPostUrl.FromPostId(postId);
        return new CommunityCleanupFacebookPageLinkDto(
            href,
            FacebookPostUrl.BuildShareLabel(ev.Title),
            DateTime.UtcNow);
    }
}
