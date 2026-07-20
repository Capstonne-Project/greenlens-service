using FluentAssertions;
using Greenlens.Application.Common;

namespace Greenlens.Application.UnitTests;

public sealed class GeoMathTests
{
    [Fact]
    public void HaversineMeters_IdenticalCoordinates_ReturnsZero_BR_REP_030()
    {
        var distance = GeoMath.HaversineMeters(10.8231m, 106.6297m, 10.8231m, 106.6297m);

        distance.Should().Be(0);
    }

    [Fact]
    public void HaversineMeters_NearbyCoordinates_WithinFiftyMeters_BR_REP_030()
    {
        // ~11 m north at HCMC latitude
        var distance = GeoMath.HaversineMeters(10.8231m, 106.6297m, 10.8232m, 106.6297m);

        distance.Should().BeLessThanOrEqualTo(50);
        distance.Should().BeGreaterThan(0);
    }

    [Fact]
    public void HaversineMeters_WhenIntermediateAExceedsOne_DoesNotReturnNaN_BR_REP_030()
    {
        // Reproduce FP edge case: unclamped a > 1 makes sqrt(1 - a) NaN.
        const double overflowA = 1.0000000000000002;
        var broken = 6_371_000.0 * 2 * Math.Atan2(Math.Sqrt(overflowA), Math.Sqrt(1 - overflowA));

        broken.Should().Be(double.NaN);

        // Clamped Asin formulation used by GeoMath stays finite.
        var fixedDistance = GeoMath.HaversineMeters(0m, 0m, 0m, 180m);

        fixedDistance.Should().NotBe(double.NaN);
        fixedDistance.Should().BeApproximately(20_015_086.8, 1);
    }
}
