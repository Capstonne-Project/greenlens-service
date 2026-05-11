using Greenlens.Application.Common.Map;
using Greenlens.Application.Features.Map.GetPublicMapReports;

namespace Greenlens.Application.UnitTests;

public sealed class GetPublicMapReportsQueryValidatorTests
{
    private readonly GetPublicMapReportsQueryValidator _sut = new();

    private static GetPublicMapReportsQuery ValidDetailQuery() =>
        new(
            MinLat: 10m,
            MaxLat: 11m,
            MinLng: 106m,
            MaxLng: 107m,
            Mode: "detail",
            Limit: 100,
            GridLevel: null,
            CategoryId: null);

    [Fact]
    public void Validate_ValidDetailQuery_IsValid_BR_MAP_012()
    {
        var result = _sut.Validate(ValidDetailQuery());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_BoundingBoxTooWide_IsInvalid_BR_MAP_012()
    {
        var q = new GetPublicMapReportsQuery(
            MinLat: 10m,
            MaxLat: 10m + PublicMapQueryLimits.MaxBoundingLatSpan + 0.1m,
            MinLng: 106m,
            MaxLng: 107m);

        var result = _sut.Validate(q);

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("detail")]
    [InlineData("aggregate")]
    [InlineData("DETAIL")]
    public void Validate_ModeVariants_IsValid_BR_MAP_012(string mode)
    {
        var q = ValidDetailQuery() with { Mode = mode };
        var result = _sut.Validate(q);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_InvalidMode_IsInvalid_BR_MAP_012()
    {
        var q = ValidDetailQuery() with { Mode = "heatmap" };
        var result = _sut.Validate(q);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_MinLatOutsideVietnam_IsInvalid_BR_REP_003()
    {
        var q = ValidDetailQuery() with { MinLat = 7m };
        var result = _sut.Validate(q);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_LimitOverMax_IsInvalid_BR_MAP_012()
    {
        var q = ValidDetailQuery() with { Limit = PublicMapQueryLimits.MaxDetailLimit + 1 };
        var result = _sut.Validate(q);

        Assert.False(result.IsValid);
    }
}
