using FluentAssertions;
using Greenlens.Application.Common;

namespace Greenlens.Application.UnitTests;

public sealed class FacebookPostUrlTests
{
    [Fact]
    public void FromPostId_SplitsPageAndStoryId()
    {
        FacebookPostUrl.FromPostId("642977455573500_122189729354922710")
            .Should()
            .Be("https://www.facebook.com/642977455573500/posts/122189729354922710");
    }
}
