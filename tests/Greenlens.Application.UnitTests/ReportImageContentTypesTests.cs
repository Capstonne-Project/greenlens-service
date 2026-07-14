using Greenlens.Application.Common;

namespace Greenlens.Application.UnitTests;

public sealed class ReportImageContentTypesTests
{
    [Theory]
    [InlineData("IMG_0536.HEIC", "application/octet-stream", "image/heic")]
    [InlineData("photo.heif", "image/heif", "image/heif")]
    [InlineData("pic.JPG", "image/jpg", "image/jpeg")]
    [InlineData("IMG_0536.HEIC", "application/vnd.apple.heic", "image/heic")]
    [InlineData("IMG_0536.HEIC", "image/heic-sequence", "image/heic")]
    public void TryResolve_ValidInputs_ReturnsNormalizedMime(
        string fileName, string contentType, string expected)
    {
        var ok = ReportImageContentTypes.TryResolve(fileName, contentType, out var mime);

        Assert.True(ok);
        Assert.Equal(expected, mime);
    }

    [Theory]
    [InlineData("file.gif", "image/gif")]
    [InlineData("file.bin", "application/octet-stream")]
    public void TryResolve_UnsupportedType_ReturnsFalse(string fileName, string contentType)
    {
        Assert.False(ReportImageContentTypes.TryResolve(fileName, contentType, out _));
    }
}
