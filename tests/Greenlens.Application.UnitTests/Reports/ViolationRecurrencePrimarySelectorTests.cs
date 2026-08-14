using Greenlens.Application.Common;
using Greenlens.Domain.Enums;
using Xunit;

namespace Greenlens.Application.UnitTests.Reports;

public sealed class ViolationRecurrencePrimarySelectorTests
{
    private const string WardCode = "26808";
    private const string ProvinceCode = "79";

    [Fact]
    public void SelectPrimary_Within25m_Included_BR_REP_034()
    {
        var id = Guid.NewGuid();
        var baseLat = 21.0285m;
        var baseLng = 105.8542m;
        var offset25m = (decimal)(GeoMath.ProximityMatchRadiusMeters / 111_320.0);

        var nearby = new[]
        {
            new ViolationRecurrenceNearbyReport(
                id, baseLat + offset25m, baseLng, WardCode, ProvinceCode, DateTime.UtcNow.AddDays(-5)),
        };

        var selected = ViolationRecurrencePrimarySelector.SelectPrimary(
            baseLat, baseLng, WardCode, ProvinceCode, nearby);

        Assert.Equal(id, selected);
    }

    [Fact]
    public void SelectPrimary_Beyond25m_ReturnsNull_BR_REP_034()
    {
        var id = Guid.NewGuid();
        var baseLat = 21.0285m;
        var baseLng = 105.8542m;
        var offset26m = (decimal)(26.0 / 111_320.0);

        var nearby = new[]
        {
            new ViolationRecurrenceNearbyReport(
                id, baseLat + offset26m, baseLng, WardCode, ProvinceCode, DateTime.UtcNow.AddDays(-5)),
        };

        var selected = ViolationRecurrencePrimarySelector.SelectPrimary(
            baseLat, baseLng, WardCode, ProvinceCode, nearby);

        Assert.Null(selected);
    }

    [Fact]
    public void BlocksRecurrenceDetection_VerifiedInProgressReopened_BR_REP_034()
    {
        Assert.True(ViolationRecurrencePrimarySelector.BlocksRecurrenceDetection(ReportStatus.Verified));
        Assert.True(ViolationRecurrencePrimarySelector.BlocksRecurrenceDetection(ReportStatus.InProgress));
        Assert.True(ViolationRecurrencePrimarySelector.BlocksRecurrenceDetection(ReportStatus.Reopened));
        Assert.False(ViolationRecurrencePrimarySelector.BlocksRecurrenceDetection(ReportStatus.Submitted));
        Assert.False(ViolationRecurrencePrimarySelector.BlocksRecurrenceDetection(ReportStatus.Closed));
        Assert.False(ViolationRecurrencePrimarySelector.BlocksRecurrenceDetection(ReportStatus.Resolved));
    }

    [Fact]
    public void SelectPrimary_MultipleClosed_PicksMostRecentClosedAt_BR_REP_034()
    {
        var olderId = Guid.NewGuid();
        var newerId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var nearby = new[]
        {
            new ViolationRecurrenceNearbyReport(
                olderId, 21.0285m, 105.8542m, WardCode, ProvinceCode, now.AddDays(-10)),
            new ViolationRecurrenceNearbyReport(
                newerId, 21.0286m, 105.8543m, WardCode, ProvinceCode, now.AddDays(-3)),
        };

        var selected = ViolationRecurrencePrimarySelector.SelectPrimary(
            21.0285m, 105.8542m, WardCode, ProvinceCode, nearby);

        Assert.Equal(newerId, selected);
    }

    [Fact]
    public void SelectPrimary_OutsideRadius_ReturnsNull_BR_REP_034()
    {
        var farId = Guid.NewGuid();
        var nearby = new[]
        {
            new ViolationRecurrenceNearbyReport(
                farId, 21.10m, 106.00m, WardCode, ProvinceCode, DateTime.UtcNow.AddDays(-5)),
        };

        var selected = ViolationRecurrencePrimarySelector.SelectPrimary(
            21.0285m, 105.8542m, WardCode, ProvinceCode, nearby);

        Assert.Null(selected);
    }

    [Fact]
    public void SelectPrimary_DifferentWardWithin25m_ReturnsNull_BR_REP_034()
    {
        var id = Guid.NewGuid();
        var baseLat = 21.0285m;
        var baseLng = 105.8542m;

        var nearby = new[]
        {
            new ViolationRecurrenceNearbyReport(
                id, baseLat, baseLng, "26809", ProvinceCode, DateTime.UtcNow.AddDays(-5)),
        };

        var selected = ViolationRecurrencePrimarySelector.SelectPrimary(
            baseLat, baseLng, WardCode, ProvinceCode, nearby);

        Assert.Null(selected);
    }
}
