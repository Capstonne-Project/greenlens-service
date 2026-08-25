using Greenlens.Application.Common.Map;
using Greenlens.Application.Features.Map.GetMapViewportSummary;
using Greenlens.Application.UnitTests.TestDoubles;

namespace Greenlens.Application.UnitTests;

public sealed class GetMapViewportSummaryQueryValidatorTests
{
    private readonly GetMapViewportSummaryQueryValidator _sut = new(new DefaultSystemSettingsProvider());

    private static GetMapViewportSummaryQuery ValidQuery() =>
        new(
            MinLat: 10m,
            MaxLat: 11m,
            MinLng: 106m,
            MaxLng: 107m,
            Days: 30,
            CategoryId: null);

    [Fact]
    public void Validate_ValidQuery_IsValid_BR_MAP_012()
    {
        var result = _sut.Validate(ValidQuery());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_BoundingBoxTooWide_IsInvalid_BR_MAP_012()
    {
        var q = new GetMapViewportSummaryQuery(
            MinLat: 10m,
            MaxLat: 10m + PublicMapQueryLimits.MaxBoundingLatSpan + 0.1m,
            MinLng: 106m,
            MaxLng: 107m);

        var result = _sut.Validate(q);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_DaysBelowMin_IsInvalid_BR_MAP_012()
    {
        var q = ValidQuery() with { Days = PublicMapViewportSummaryLimits.MinDays - 1 };

        var result = _sut.Validate(q);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_DaysAboveMax_IsInvalid_BR_MAP_012()
    {
        var q = ValidQuery() with { Days = PublicMapViewportSummaryLimits.MaxDays + 1 };

        var result = _sut.Validate(q);

        Assert.False(result.IsValid);
    }
}
