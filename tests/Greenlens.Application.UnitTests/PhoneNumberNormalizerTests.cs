using Greenlens.Application.Common;

namespace Greenlens.Application.UnitTests;

public sealed class PhoneNumberNormalizerTests
{
    [Theory]
    [InlineData("0912345678", "84912345678")]
    [InlineData("+84912345678", "84912345678")]
    [InlineData("84912345678", "84912345678")]
    [InlineData("0912-345-678", "84912345678")]
    public void Normalize_ValidVnPhone_ReturnsInternationalFormat(string input, string expected)
    {
        Assert.Equal(expected, PhoneNumberNormalizer.Normalize(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc123xyz")]
    [InlineData("12345")]
    [InlineData("+1-555-0100")]
    public void Normalize_UnrecognizedFormat_ReturnsNull(string? input)
    {
        Assert.Null(PhoneNumberNormalizer.Normalize(input));
    }
}
