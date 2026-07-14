using FluentAssertions;
using Greenlens.Infrastructure.Moderation;
using Microsoft.Extensions.Options;

namespace Greenlens.Application.UnitTests;

public sealed class ProfanityFilterTests
{
    private static ProfanityFilter CreateFilter() =>
        new(Options.Create(new ModerationOptions()));

    [Fact]
    public void ContainsProfanity_CleanText_ReturnsFalse_BR_CMT_003()
    {
        CreateFilter().ContainsProfanity("Cảm ơn bạn đã báo cáo.")
            .Should().BeFalse();
    }

    [Fact]
    public void ContainsProfanity_BlockedWord_ReturnsTrue_BR_CMT_003()
    {
        CreateFilter().ContainsProfanity("Nội dung có từ vcl không phù hợp")
            .Should().BeTrue();
    }

    [Fact]
    public void ContainsProfanity_CaseInsensitive_ReturnsTrue_BR_CMT_003()
    {
        CreateFilter().ContainsProfanity("FUCK this")
            .Should().BeTrue();
    }
}
