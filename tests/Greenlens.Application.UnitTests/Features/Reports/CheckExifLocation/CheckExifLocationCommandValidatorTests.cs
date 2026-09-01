using Greenlens.Application.Features.Reports.CheckExifLocation;
using Greenlens.Application.UnitTests.TestDoubles;

namespace Greenlens.Application.UnitTests.Features.Reports.CheckExifLocation;

public sealed class CheckExifLocationCommandValidatorTests
{
    private readonly CheckExifLocationCommandValidator _sut = new(new DefaultSystemSettingsProvider());

    [Fact]
    public void Validate_NoImageSource_ReturnsValidationError()
    {
        var result = _sut.Validate(new CheckExifLocationCommand(
            10.7626m,
            106.6602m,
            null,
            null,
            null,
            null,
            null,
            null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ImageSource");
    }

    [Fact]
    public void Validate_TempImageIdOnly_IsValid()
    {
        var result = _sut.Validate(new CheckExifLocationCommand(
            10.7626m,
            106.6602m,
            "0123456789abcdef0123456789abcdef",
            null,
            null,
            null,
            null,
            null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_LatitudeOutsideVietnam_ReturnsValidationError_BR_REP_003()
    {
        var result = _sut.Validate(new CheckExifLocationCommand(
            30m,
            106.6602m,
            "0123456789abcdef0123456789abcdef",
            null,
            null,
            null,
            null,
            null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CheckExifLocationCommand.Latitude));
    }
}
