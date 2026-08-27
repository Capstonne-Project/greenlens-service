using FluentAssertions;
using Greenlens.Application.Common.Options;
using Greenlens.Application.Features.CommunityCleanup.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.UnitTests;

public sealed class CommunityCleanupShareBuilderTests
{
    private static readonly PublicWebOptions PublicWeb = new()
    {
        BaseUrl = "https://leo.greenlens.test",
        CommunityCleanupPathTemplate = "/c/community/{eventId}"
    };

    [Fact]
    public void Build_IncludesLandingUrlAndSocialShareLinks_BR_CMU_001()
    {
        var ev = CommunityCleanupEvent.Create(
            reportId: Guid.NewGuid(),
            createdByLeoId: Guid.NewGuid(),
            leaderUserId: Guid.NewGuid(),
            leaderTeamId: Guid.NewGuid(),
            title: "Dọn rác Hiệp Bình",
            description: "Cùng dọn sạch khu phố",
            startsAt: new DateTime(2026, 8, 27, 7, 0, 0, DateTimeKind.Utc),
            endsAt: null,
            joinClosesAt: null,
            maxParticipants: 40,
            meetingNote: "Cổng chào",
            meetingLatitude: 10.78m,
            meetingLongitude: 106.69m);

        var report = Report.Create(
            "RPT-2026-0099",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Severity.Medium,
            "Báo cáo test",
            10.78m,
            106.69m,
            "Phường Hiệp Bình, TP.HCM",
            null,
            null);

        var share = CommunityCleanupShareBuilder.Build(ev, report, "https://cdn.test/thumb.jpg", PublicWeb);

        share.Url.Should().Be($"https://leo.greenlens.test/c/community/{ev.Id:D}");
        share.ImageUrl.Should().Be("https://cdn.test/thumb.jpg");
        share.Caption.Should().Contain("Dọn rác Hiệp Bình");
        share.Caption.Should().Contain("Cùng dọn sạch khu phố");
        share.Caption.Should().Contain("https://leo.greenlens.test/c/community/");
        share.FacebookShareUrl.Should().Contain("facebook.com/sharer/sharer.php?u=");
        share.TwitterShareUrl.Should().Contain("twitter.com/intent/tweet");
        share.LinkedInShareUrl.Should().Contain("linkedin.com/sharing/share-offsite");
        share.Hashtags.Should().Contain("GreenLens");
    }
}
