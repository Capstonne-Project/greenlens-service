using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Features.Reports;
using Greenlens.Application.Features.Reports.CheckExifLocation;
using Greenlens.Application.UnitTests.TestDoubles;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Greenlens.Application.UnitTests.Features.Reports.CheckExifLocation;

public sealed class CheckExifLocationCommandHandlerTests
{
    private const decimal SelectedLat = 10.7626m;
    private const decimal SelectedLng = 106.6602m;
    private const decimal ExifLat = 10.8000m;
    private const decimal ExifLng = 106.6602m;
    private const string TempImageId = "0123456789abcdef0123456789abcdef";

    [Fact]
    public async Task Handle_TempImageWithGpsMismatch_ReturnsShouldWarnTrue_BR_REP_011()
    {
        var tempStore = Substitute.For<ITempImageStore>();
        tempStore.GetAsync(TempImageId, Arg.Any<CancellationToken>())
            .Returns(new TempImageEntry(
                [0x01, 0x02],
                "photo.jpg",
                "image/jpeg",
                DateTime.UtcNow.AddMinutes(10),
                null,
                null,
                null));

        var exifAnalyzer = Substitute.For<IImageExifAnalyzer>();
        exifAnalyzer.Analyze(Arg.Any<ReadOnlyMemory<byte>>(), SelectedLat, SelectedLng)
            .Returns(new ImageExifAnalysis(
                true,
                DateTime.UtcNow,
                ExifLat,
                ExifLng,
                null,
                [ExifSuspicionEvaluator.GpsMismatchReason]));

        var sut = new CheckExifLocationCommandHandler(
            tempStore,
            Substitute.For<IFileStorageService>(),
            exifAnalyzer,
            new DefaultSystemSettingsProvider(),
            Substitute.For<ILogger<CheckExifLocationCommandHandler>>());

        var result = await sut.Handle(
            new CheckExifLocationCommand(
                SelectedLat,
                SelectedLng,
                TempImageId,
                null,
                null,
                null,
                null,
                null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.HasExifGps);
        Assert.True(result.Value.ShouldWarn);
        Assert.Equal(ExifLat, result.Value.ExifLatitude);
        Assert.Equal(ExifLng, result.Value.ExifLongitude);
        Assert.NotNull(result.Value.DistanceMeters);
        Assert.True(result.Value.DistanceMeters > 200);
        Assert.Equal(200, result.Value.ThresholdMeters);
    }

    [Fact]
    public async Task Handle_TempImageWithoutExifGps_ReturnsShouldWarnFalse_BR_REP_011()
    {
        var tempStore = Substitute.For<ITempImageStore>();
        tempStore.GetAsync(TempImageId, Arg.Any<CancellationToken>())
            .Returns(new TempImageEntry(
                [0x01],
                "photo.jpg",
                "image/jpeg",
                DateTime.UtcNow.AddMinutes(10),
                null,
                null,
                null));

        var exifAnalyzer = Substitute.For<IImageExifAnalyzer>();
        exifAnalyzer.Analyze(Arg.Any<ReadOnlyMemory<byte>>(), SelectedLat, SelectedLng)
            .Returns(new ImageExifAnalysis(false, null, null, null, null, []));

        var sut = new CheckExifLocationCommandHandler(
            tempStore,
            Substitute.For<IFileStorageService>(),
            exifAnalyzer,
            new DefaultSystemSettingsProvider(),
            Substitute.For<ILogger<CheckExifLocationCommandHandler>>());

        var result = await sut.Handle(
            new CheckExifLocationCommand(
                SelectedLat,
                SelectedLng,
                TempImageId,
                null,
                null,
                null,
                null,
                null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.HasExifGps);
        Assert.False(result.Value.ShouldWarn);
        Assert.Null(result.Value.DistanceMeters);
    }

    [Fact]
    public async Task Handle_ExpiredTempImage_ReturnsTempImageNotFound()
    {
        var tempStore = Substitute.For<ITempImageStore>();
        tempStore.GetAsync(TempImageId, Arg.Any<CancellationToken>())
            .Returns((TempImageEntry?)null);

        var sut = new CheckExifLocationCommandHandler(
            tempStore,
            Substitute.For<IFileStorageService>(),
            Substitute.For<IImageExifAnalyzer>(),
            new DefaultSystemSettingsProvider(),
            Substitute.For<ILogger<CheckExifLocationCommandHandler>>());

        var result = await sut.Handle(
            new CheckExifLocationCommand(
                SelectedLat,
                SelectedLng,
                TempImageId,
                null,
                null,
                null,
                null,
                null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("TEMP_IMAGE_NOT_FOUND", result.Error!.Code);
    }
}
