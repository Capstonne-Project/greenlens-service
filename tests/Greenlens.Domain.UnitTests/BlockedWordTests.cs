using Greenlens.Domain.Entities;

namespace Greenlens.Domain.UnitTests;

public sealed class BlockedWordTests
{
    [Fact]
    public void NormalizeWord_TrimsAndLowercases()
    {
        Assert.Equal("vcl", BlockedWord.NormalizeWord("  VCL "));
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var word = BlockedWord.Create("test");
        word.Deactivate();
        Assert.False(word.IsActive);
    }
}
