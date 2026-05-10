using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Features.Media.UploadReportImage;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Greenlens.Application.UnitTests;

public sealed class UploadReportImageCommandHandlerTests
{
    private readonly IFileStorageService _fileStorage = Substitute.For<IFileStorageService>();
    private readonly ILogger<UploadReportImageCommandHandler> _logger =
        Substitute.For<ILogger<UploadReportImageCommandHandler>>();
    private readonly UploadReportImageCommandHandler _sut;

    public UploadReportImageCommandHandlerTests()
    {
        _sut = new UploadReportImageCommandHandler(_fileStorage, _logger);
    }

    private static UploadReportImageCommand CreateCommand(
        string contentType = "image/jpeg",
        long fileSize = 1_000_000,
        string fileName = "photo.jpg") =>
        new(Stream.Null, fileName, contentType, fileSize);

    // ── Happy path ──

    [Fact]
    public async Task Handle_ValidImage_ShouldReturnSuccess()
    {
        _fileStorage.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new FileUploadResult("https://cdn/image.jpg", "reports/images/abc_photo.jpg"));

        var result = await _sut.Handle(CreateCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("https://cdn/image.jpg", result.Value.Url);
        Assert.Equal("reports/images/abc_photo.jpg", result.Value.Key);
    }

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    [InlineData("image/heic")]
    public async Task Handle_AllowedContentTypes_ShouldAccept(string contentType)
    {
        _fileStorage.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new FileUploadResult("url", "key"));

        var result = await _sut.Handle(CreateCommand(contentType: contentType), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    // ── Validation errors ──

    [Theory]
    [InlineData("image/gif")]
    [InlineData("image/bmp")]
    [InlineData("video/mp4")]
    [InlineData("application/pdf")]
    public async Task Handle_InvalidContentType_ShouldReturnError(string contentType)
    {
        var result = await _sut.Handle(CreateCommand(contentType: contentType), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_IMAGE_TYPE", result.Error.Code);
    }

    [Fact]
    public async Task Handle_FileTooLarge_ShouldReturnError()
    {
        var result = await _sut.Handle(
            CreateCommand(fileSize: 11 * 1024 * 1024), // 11MB
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("IMAGE_TOO_LARGE", result.Error.Code);
    }

    [Fact]
    public async Task Handle_ExactlyMaxSize_ShouldAccept()
    {
        _fileStorage.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new FileUploadResult("url", "key"));

        var result = await _sut.Handle(
            CreateCommand(fileSize: 10 * 1024 * 1024), // exactly 10MB
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    // ── Storage failure ──

    [Fact]
    public async Task Handle_StorageThrows_ShouldReturnStorageError()
    {
        _fileStorage.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("R2 access denied"));

        var result = await _sut.Handle(CreateCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("STORAGE_UPLOAD_FAILED", result.Error.Code);
    }

    // ── Correct folder ──

    [Fact]
    public async Task Handle_ShouldUploadToReportsImagesFolder()
    {
        _fileStorage.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new FileUploadResult("url", "key"));

        await _sut.Handle(CreateCommand(), CancellationToken.None);

        await _fileStorage.Received(1).UploadAsync(
            Arg.Any<Stream>(),
            Arg.Any<string>(),
            "image/jpeg",
            "reports/images",
            Arg.Any<CancellationToken>());
    }
}
