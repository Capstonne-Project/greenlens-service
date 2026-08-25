using Greenlens.Domain.Entities;

namespace Greenlens.Domain.UnitTests;

public sealed class BadgeThresholdTests
{
    [Fact]
    public void UpdateThreshold_EcoWarrior_UpdatesRequiredReportCount_BR_ADM_005()
    {
        var badge = Badge.Create("eco_warrior", "VI", "EN", requiredReportCount: 10);

        badge.UpdateThreshold(15);

        Assert.Equal(15, badge.RequiredReportCount);
        Assert.Null(badge.RequiredPoints);
        Assert.Null(badge.RequiredStreakDays);
        Assert.Null(badge.RequiredActionCount);
    }

    [Fact]
    public void UpdateThreshold_DuplicateFinder_UpdatesRequiredActionCount_BR_ADM_005()
    {
        var badge = Badge.Create("duplicate_finder", "VI", "EN", requiredActionCount: 5);

        badge.UpdateThreshold(8);

        Assert.Equal(8, badge.RequiredActionCount);
        Assert.Null(badge.RequiredReportCount);
    }
}
