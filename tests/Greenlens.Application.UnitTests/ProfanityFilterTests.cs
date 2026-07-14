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

    private sealed class StubBlockedWordCache(string[] words) : IBlockedWordCache
    {
        public IReadOnlyList<string> GetActiveWords() => words;
        public Task RefreshAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
