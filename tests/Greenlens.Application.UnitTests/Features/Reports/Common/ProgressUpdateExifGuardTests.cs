using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Features.Reports;
using Greenlens.Application.Features.Reports.Common;
using Greenlens.Application.UnitTests.TestDoubles;
using Greenlens.Domain.Common;
using NSubstitute;

namespace Greenlens.Application.UnitTests.Features.Reports.Common;

public sealed class ProgressUpdateExifGuardTests
{
    private const decimal SiteLat = 10.7626m;
    private const decimal SiteLng = 106.6602m;
    private const decimal ExifLat = 10.8000m;
    private const decimal ExifLng = 106.6602m;
    private const string ImageUrl = "https://cdn.test.local/reports/r1/progress/photo.jpg";
    private const string ImageKey = "reports/r1/progress/photo.jpg";

    [Fact]
    public async Task ValidateProgressImageUrls_ExifGpsTooFar_ReturnsPhotoTooFarError_BR_CLN_004()
    {
        var fileStorage = Substitute.For<IFileStorageService>();
        fileStorage.TryGetKeyFromOwnedPublicUrl(ImageUrl).Returns(ImageKey);
        fileStorage.DownloadAsync(ImageKey, Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new StoredFileDownload([0x01, 0x02], "image/jpeg", 2));

        var exifAnalyzer = Substitute.For<IImageExifAnalyzer>();
        exifAnalyzer.Analyze(Arg.Any<ReadOnlyMemory<byte>>(), SiteLat, SiteLng)
            .Returns(new ImageExifAnalysis(
                true,
                DateTime.UtcNow,
                ExifLat,
                ExifLng,
                null,
                [ExifSuspicionEvaluator.GpsMismatchReason]));

        var error = await ProgressUpdateExifGuard.ValidateProgressImageUrlsAsync(
            [ImageUrl],
            SiteLat,
            SiteLng,
            fileStorage,
            exifAnalyzer,
            new DefaultSystemSettingsProvider(),
            CancellationToken.None);

        Assert.NotNull(error);
        Assert.Equal("PROGRESS_PHOTO_TOO_FAR", error.Code);
    }

    [Fact]
    public async Task ValidateProgressImageUrls_ExifGpsNearSite_ReturnsNull_BR_CLN_004()
    {
        var fileStorage = Substitute.For<IFileStorageService>();
        fileStorage.TryGetKeyFromOwnedPublicUrl(ImageUrl).Returns(ImageKey);
        fileStorage.DownloadAsync(ImageKey, Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new StoredFileDownload([0x01], "image/jpeg", 1));

        var exifAnalyzer = Substitute.For<IImageExifAnalyzer>();
        exifAnalyzer.Analyze(Arg.Any<ReadOnlyMemory<byte>>(), SiteLat, SiteLng)
            .Returns(new ImageExifAnalysis(
                true,
                DateTime.UtcNow,
                SiteLat + 0.0001m,
                SiteLng,
                null,
                []));

        var error = await ProgressUpdateExifGuard.ValidateProgressImageUrlsAsync(
            [ImageUrl],
            SiteLat,
            SiteLng,
            fileStorage,
            exifAnalyzer,
            new DefaultSystemSettingsProvider(),
            CancellationToken.None);

        Assert.Null(error);
    }

    [Fact]
    public async Task ValidateProgressImageUrls_NoExifGps_ReturnsNull_BR_CLN_004()
    {
        var fileStorage = Substitute.For<IFileStorageService>();
        fileStorage.TryGetKeyFromOwnedPublicUrl(ImageUrl).Returns(ImageKey);
        fileStorage.DownloadAsync(ImageKey, Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new StoredFileDownload([0x01], "image/jpeg", 1));

        var exifAnalyzer = Substitute.For<IImageExifAnalyzer>();
        exifAnalyzer.Analyze(Arg.Any<ReadOnlyMemory<byte>>(), SiteLat, SiteLng)
            .Returns(new ImageExifAnalysis(false, null, null, null, null, []));

        var error = await ProgressUpdateExifGuard.ValidateProgressImageUrlsAsync(
            [ImageUrl],
            SiteLat,
            SiteLng,
            fileStorage,
            exifAnalyzer,
            new DefaultSystemSettingsProvider(),
            CancellationToken.None);

        Assert.Null(error);
    }
}
