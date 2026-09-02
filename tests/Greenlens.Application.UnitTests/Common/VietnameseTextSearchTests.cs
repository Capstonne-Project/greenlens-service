using FluentAssertions;
using Greenlens.Application.Common;

namespace Greenlens.Application.UnitTests.Common;

public sealed class VietnameseTextSearchTests
{
    [Fact]
    public void Tokenize_MultiWordSearch_ReturnsDistinctTokens()
    {
        var tokens = VietnameseTextSearch.Tokenize("  Gia   Định  ");

        tokens.Should().Equal("Gia", "Định");
    }

    [Fact]
    public void ToContainsPattern_WrapsTokenWithWildcards()
    {
        VietnameseTextSearch.ToContainsPattern("Gia").Should().Be("%Gia%");
    }

    [Fact]
    public void Tokenize_EscapesLikeWildcards()
    {
        var tokens = VietnameseTextSearch.Tokenize("100%");

        tokens.Should().ContainSingle().Which.Should().Be(@"100\%");
    }
}
