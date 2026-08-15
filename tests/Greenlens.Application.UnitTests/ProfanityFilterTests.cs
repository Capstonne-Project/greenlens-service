using FluentAssertions;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Infrastructure.Moderation;

namespace Greenlens.Application.UnitTests;

public sealed class ProfanityFilterTests
{
    private static ProfanityFilter CreateFilter(params string[] words) =>
        new(new StubBlockedWordCache(words));

    [Fact]
    public void ContainsProfanity_CleanText_ReturnsFalse_BR_CMT_003()
    {
        CreateFilter("vcl", "fuck").ContainsProfanity("Cảm ơn bạn đã báo cáo.")
            .Should().BeFalse();
    }

    [Fact]
    public void ContainsProfanity_BlockedWord_ReturnsTrue_BR_CMT_003()
    {
        CreateFilter("vcl", "fuck").ContainsProfanity("Nội dung có từ vcl không phù hợp")
            .Should().BeTrue();
    }

    [Fact]
    public void ContainsProfanity_CaseInsensitive_ReturnsTrue_BR_CMT_003()
    {
        CreateFilter("fuck").ContainsProfanity("FUCK this")
            .Should().BeTrue();
    }

    [Fact]
    public void ContainsProfanity_SubstringInsideWord_ReturnsFalse_BR_REP_004()
    {
        CreateFilter("đụ").ContainsProfanity("Hộ bà con lo nước đục bám vào lá rau")
            .Should().BeFalse();
    }

    [Fact]
    public void ContainsProfanity_WholeBlockedWord_ReturnsTrue_BR_REP_004()
    {
        CreateFilter("đụ").ContainsProfanity("nội dung đụ không chấp nhận")
            .Should().BeTrue();
    }

    [Fact]
    public void ContainsProfanity_PollutionReportDescription_ReturnsFalse_BR_REP_004()
    {
        const string description =
            "Công trình sửa nhà ven sông Sào vứt gạch vỡ, xi măng và tấm lợp cũ xuống bờ mương thoát nước, "
            + "chặn gần hết luồng chảy. Mưa to tuần trước khiến nước tràn lên đường làng, xe máy không qua được. "
            + "Hộ bà con trồng rau ven mương lo nước đục bám vào lá rau; trẻ em đi học phải cởi dép lội nước. "
            + "Tình trạng kéo dài 5 ngày, chưa thấy ai dọn dẹp.";

        var seedWords = new[] { "địt", "đụ", "lồn", "cặc", "đéo", "vcl", "vl", "fuck", "shit", "bitch" };
        CreateFilter(seedWords).ContainsProfanity(description)
            .Should().BeFalse();
    }

    [Fact]
    public void ContainsProfanity_ShortBlockedWord_NotInsideLongerWord_ReturnsFalse_BR_CMT_003()
    {
        CreateFilter("vl").ContainsProfanity("level dev civil")
            .Should().BeFalse();
    }

    private sealed class StubBlockedWordCache(string[] words) : IBlockedWordCache
    {
        public IReadOnlyList<string> GetActiveWords() => words;
        public Task RefreshAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
