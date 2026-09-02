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
    public void Build_MatchesFacebookPageCaptionTemplate_BR_CMU_001()
    {
        var startsAt = new DateTime(2026, 8, 9, 2, 0, 0, DateTimeKind.Utc);
        var endsAt = new DateTime(2026, 8, 11, 2, 0, 0, DateTimeKind.Utc);

        var ev = CommunityCleanupEvent.Create(
            reportId: Guid.NewGuid(),
            createdByLeoId: Guid.NewGuid(),
            leaderUserId: Guid.NewGuid(),
            leaderTeamId: Guid.NewGuid(),
            title: "nhiêu lộc",
            description: """
                Nón, áo khoác hoặc găng tay chống nắng
                01 bộ đồ dự phòng (phòng trường hợp bị bẩn hoặc ướt)
                Tinh thần thật "xanh" và năng lượng thật tích cực 💚
                """,
            startsAt: startsAt,
            endsAt: endsAt,
            joinClosesAt: null,
            maxParticipants: 40,
            meetingNote: null,
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
            "19 Song Hành, Bình Trưng, Hồ Chí Minh",
            null,
            null);

        var share = CommunityCleanupShareBuilder.Build(ev, report, "https://cdn.test/thumb.jpg", PublicWeb);

        share.Url.Should().Be($"https://leo.greenlens.test/c/community/{ev.Id:D}");
        share.Caption.Should().NotContain("\r\n");
        var expected = CommunityCleanupShareBuilder.NormalizeLineEndings(
            """
            🌱 THÔNG TIN THAM GIA NHIÊU LỘC CÙNG GREENLENS 🌱

            🎒 Chuẩn bị cá nhân
            • Nón, áo khoác hoặc găng tay chống nắng
            • 01 bộ đồ dự phòng (phòng trường hợp bị bẩn hoặc ướt)
            • Tinh thần thật "xanh" và năng lượng thật tích cực 💚

            ⏰ Lịch trình hoạt động
            • Ngày bắt đầu: 09:00 Chủ Nhật, 09/08/2026
            • Ngày kết thúc: 09:00 Thứ Ba, 11/08/2026

            📍 Địa điểm tập trung
            19 Song Hành, Bình Trưng, Hồ Chí Minh

            👉 Khi đến nơi, bạn vui lòng:
            • Check-in theo hướng dẫn của Leader
            • Tuân thủ hướng dẫn an toàn tại khu vực

            ⚠️ Lưu ý quan trọng
            • Thời gian hoạt động có thể thay đổi tùy theo khối lượng rác thực tế
            • Hãy tuân thủ hướng dẫn an toàn và phối hợp cùng đội nhóm để đạt hiệu quả cao nhất

            📲 Tải ứng dụng GreenLens tại https://leo.greenlens.test để cùng chung tay bảo vệ môi trường nhé!

            📞 Hỗ trợ & liên hệ

            098 773 0708

            #GreenLens #DonDepCongDong #CaiNhinDonDep #ChamSocMoiTruong
            """);
        share.Caption.Should().Be(expected);
        share.FacebookShareUrl.Should().Contain("facebook.com/sharer/sharer.php?u=");
        share.Hashtags.Should().Contain("GreenLens");
    }

    [Fact]
    public void Build_MarkdownDescription_NormalizesToBulletLines()
    {
        var ev = CommunityCleanupEvent.Create(
            reportId: Guid.NewGuid(),
            createdByLeoId: Guid.NewGuid(),
            leaderUserId: Guid.NewGuid(),
            leaderTeamId: Guid.NewGuid(),
            title: "Dọn rác test",
            description: "- Găng tay\n- Nón",
            startsAt: new DateTime(2026, 8, 9, 2, 0, 0, DateTimeKind.Utc),
            endsAt: null,
            joinClosesAt: null,
            maxParticipants: 40,
            meetingNote: null,
            meetingLatitude: null,
            meetingLongitude: null);

        var report = Report.Create(
            "RPT-2026-0100",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Severity.Low,
            "Test",
            10m,
            106m,
            "TP.HCM",
            null,
            null);

        var share = CommunityCleanupShareBuilder.Build(ev, report, null, PublicWeb);

        share.Caption.Should().Contain("• Găng tay");
        share.Caption.Should().Contain("• Nón");
        share.Caption.Should().NotContain("• Ngày kết thúc:");
        share.Caption.Should().NotContain("- Găng tay");
    }

    [Fact]
    public void Build_SingleLineDescription_NoBulletPrefix()
    {
        var ev = CommunityCleanupEvent.Create(
            reportId: Guid.NewGuid(),
            createdByLeoId: Guid.NewGuid(),
            leaderUserId: Guid.NewGuid(),
            leaderTeamId: Guid.NewGuid(),
            title: "Dọn rác test",
            description: "Mang găng tay, nón và nước uống",
            startsAt: new DateTime(2026, 8, 9, 2, 0, 0, DateTimeKind.Utc),
            endsAt: null,
            joinClosesAt: null,
            maxParticipants: 40,
            meetingNote: null,
            meetingLatitude: null,
            meetingLongitude: null);

        var report = Report.Create(
            "RPT-2026-0101",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Severity.Low,
            "Test",
            10m,
            106m,
            "TP.HCM",
            null,
            null);

        var share = CommunityCleanupShareBuilder.Build(ev, report, null, PublicWeb);

        share.Caption.Should().Contain("🎒 Chuẩn bị cá nhân");
        share.Caption.Should().Contain("Mang găng tay, nón và nước uống");
        share.Caption.Should().NotContain("• Mang găng tay");
    }
}
