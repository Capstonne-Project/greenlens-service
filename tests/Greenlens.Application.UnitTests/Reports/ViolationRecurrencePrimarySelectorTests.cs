using Greenlens.Application.Common;
using Greenlens.Domain.Enums;
using Xunit;

namespace Greenlens.Application.UnitTests.Reports;

public sealed class ViolationRecurrencePrimarySelectorTests
{
    [Fact]
    public void SelectPrimary_MultipleClosed_PicksMostRecentClosedAt_BR_REP_034()
    {
        var olderId = Guid.NewGuid();
        var newerId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var nearby = new[]
        {
            new ViolationRecurrenceNearbyReport(olderId, 21.0285m, 105.8542m, now.AddDays(-10)),
            new ViolationRecurrenceNearbyReport(newerId, 21.0286m, 105.8543m, now.AddDays(-3)),
        };

        var selected = ViolationRecurrencePrimarySelector.SelectPrimary(21.0285m, 105.8542m, nearby);

        Assert.Equal(newerId, selected);
    }

    [Fact]
    public void SelectPrimary_OutsideRadius_ReturnsNull_BR_REP_034()
    {
        var farId = Guid.NewGuid();
        var nearby = new[]
        {
            new ViolationRecurrenceNearbyReport(farId, 21.10m, 106.00m, DateTime.UtcNow.AddDays(-5)),
        };

        var selected = ViolationRecurrencePrimarySelector.SelectPrimary(21.0285m, 105.8542m, nearby);

        Assert.Null(selected);
    }
}
