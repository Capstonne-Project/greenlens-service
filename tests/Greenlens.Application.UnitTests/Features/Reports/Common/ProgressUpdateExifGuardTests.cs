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
    public async Task Validate_SubmittedExifCoordsTooFarWithImages_ReturnsPhotoTooFarError_BR_CLN_004()
    {
        var geoDistance = Substitute.For<IGeoDistanceService>();
        geoDistance.GetDistanceInMetersAsync(ExifLat, ExifLng, SiteLat, SiteLng, Arg.Any<CancellationToken>())
            .Returns(500d);

        var error = await ProgressUpdateExifGuard.ValidateAsync(
            ExifLat,
            ExifLng,
            SiteLat,
            SiteLng,
            [ImageUrl],
            Substitute.For<IFileStorageService>(),
            Substitute.For<IImageExifAnalyzer>(),
            geoDistance,
            new DefaultSystemSettingsProvider(),
            CancellationToken.None);

        Assert.NotNull(error);
        Assert.Equal("PROGRESS_TOO_FAR", error.Code);
    }

    [Fact]
    public async Task Validate_SubmittedExifCoordsNearSite_ReturnsNull_BR_CLN_004()
    {
        var geoDistance = Substitute.For<IGeoDistanceService>();
        geoDistance.GetDistanceInMetersAsync(SiteLat + 0.0001m, SiteLng, SiteLat, SiteLng, Arg.Any<CancellationToken>())
            .Returns(11d);

        var error = await ProgressUpdateExifGuard.ValidateAsync(
            SiteLat + 0.0001m,
            SiteLng,
            SiteLat,
            SiteLng,
            [],
            Substitute.For<IFileStorageService>(),
            Substitute.For<IImageExifAnalyzer>(),
            geoDistance,
            new DefaultSystemSettingsProvider(),
            CancellationToken.None);

        Assert.Null(error);
    }

    [Fact]
    public async Task Validate_ImageFileExifTooFar_ReturnsPhotoTooFarError_BR_CLN_004()
    {
        var geoDistance = Substitute.For<IGeoDistanceService>();
        geoDistance.GetDistanceInMetersAsync(SiteLat, SiteLng, SiteLat, SiteLng, Arg.Any<CancellationToken>())
            .Returns(10d);

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

        var error = await ProgressUpdateExifGuard.ValidateAsync(
            SiteLat,
            SiteLng,
            SiteLat,
            SiteLng,
            [ImageUrl],
            fileStorage,
            exifAnalyzer,
            geoDistance,
            new DefaultSystemSettingsProvider(),
            CancellationToken.None);

        Assert.NotNull(error);
        Assert.Equal("PROGRESS_TOO_FAR", error.Code);
    }

    [Fact]
    public async Task Validate_ImageFileWithoutExif_SubmittedCoordsNearSite_ReturnsNull_BR_CLN_004()
    {
        var geoDistance = Substitute.For<IGeoDistanceService>();
        geoDistance.GetDistanceInMetersAsync(SiteLat, SiteLng, SiteLat, SiteLng, Arg.Any<CancellationToken>())
            .Returns(10d);

        var fileStorage = Substitute.For<IFileStorageService>();
        fileStorage.TryGetKeyFromOwnedPublicUrl(ImageUrl).Returns(ImageKey);
        fileStorage.DownloadAsync(ImageKey, Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new StoredFileDownload([0x01], "image/jpeg", 1));

        var exifAnalyzer = Substitute.For<IImageExifAnalyzer>();
        exifAnalyzer.Analyze(Arg.Any<ReadOnlyMemory<byte>>(), SiteLat, SiteLng)
            .Returns(new ImageExifAnalysis(false, null, null, null, null, []));

        var error = await ProgressUpdateExifGuard.ValidateAsync(
            SiteLat,
            SiteLng,
            SiteLat,
            SiteLng,
            [ImageUrl],
            fileStorage,
            exifAnalyzer,
            geoDistance,
            new DefaultSystemSettingsProvider(),
            CancellationToken.None);

        Assert.Null(error);
    }

    [Fact]
    public async Task Validate_MissingBodyCoords_UsesImageFileExifNearSite_ReturnsNull_BR_CLN_004()
    {
        var geoDistance = Substitute.For<IGeoDistanceService>();

        var fileStorage = Substitute.For<IFileStorageService>();
        fileStorage.TryGetKeyFromOwnedPublicUrl(ImageUrl).Returns(ImageKey);
        fileStorage.DownloadAsync(ImageKey, Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new StoredFileDownload([0x01], "image/jpeg", 1));

        var exifAnalyzer = Substitute.For<IImageExifAnalyzer>();
        exifAnalyzer.Analyze(Arg.Any<ReadOnlyMemory<byte>>(), SiteLat, SiteLng)
            .Returns(new ImageExifAnalysis(true, DateTime.UtcNow, SiteLat, SiteLng, null, []));

        var error = await ProgressUpdateExifGuard.ValidateAsync(
            0m,
            0m,
            SiteLat,
            SiteLng,
            [ImageUrl],
            fileStorage,
            exifAnalyzer,
            geoDistance,
            new DefaultSystemSettingsProvider(),
            CancellationToken.None);

        Assert.Null(error);
    }

    [Fact]
    public async Task Validate_MissingBodyCoordsAndNoImageExif_ReturnsLocationRequired_BR_CLN_004()
    {
        var geoDistance = Substitute.For<IGeoDistanceService>();

        var fileStorage = Substitute.For<IFileStorageService>();
        fileStorage.TryGetKeyFromOwnedPublicUrl(ImageUrl).Returns(ImageKey);
        fileStorage.DownloadAsync(ImageKey, Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new StoredFileDownload([0x01], "image/jpeg", 1));

        var exifAnalyzer = Substitute.For<IImageExifAnalyzer>();
        exifAnalyzer.Analyze(Arg.Any<ReadOnlyMemory<byte>>(), SiteLat, SiteLng)
            .Returns(new ImageExifAnalysis(false, null, null, null, null, []));

        var error = await ProgressUpdateExifGuard.ValidateAsync(
            0m,
            0m,
            SiteLat,
            SiteLng,
            [ImageUrl],
            fileStorage,
            exifAnalyzer,
            geoDistance,
            new DefaultSystemSettingsProvider(),
            CancellationToken.None);

        Assert.NotNull(error);
        Assert.Equal("PROGRESS_LOCATION_REQUIRED", error.Code);
    }
}
