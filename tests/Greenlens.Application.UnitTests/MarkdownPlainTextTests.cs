using FluentAssertions;
using Greenlens.Application.Common;

namespace Greenlens.Application.UnitTests;

public sealed class MarkdownPlainTextTests
{
    [Fact]
    public void ToPlain_StripsCommonMarkdownSyntax()
    {
        const string markdown = """
            ## Giới thiệu
            **Cùng dọn** khu phố — xem [chi tiết](https://example.com)
            - Mục tiêu 1
            > Ghi chú quan trọng
            """;

        var plain = MarkdownPlainText.ToPlain(markdown);

        plain.Should().Contain("Giới thiệu");
        plain.Should().Contain("Cùng dọn");
        plain.Should().Contain("chi tiết");
        plain.Should().NotContain("**");
        plain.Should().NotContain("##");
        plain.Should().NotContain("](https://");
        plain.Should().Contain("• Mục tiêu 1");
    }
}
