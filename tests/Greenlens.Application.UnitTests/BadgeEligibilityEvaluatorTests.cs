using Greenlens.Application.Features.Gamification;
using Greenlens.Domain.Entities;

namespace Greenlens.Application.UnitTests;

public sealed class BadgeEligibilityEvaluatorTests
{
    [Fact]
    public void IsEligible_DuplicateFinder_FiveDuplicates_ReturnsTrue_BR_GAM_004()
    {
        var badge = CreateBadge("duplicate_finder");
        var metrics = new BadgeEligibilityMetrics(0, 5, false, 0, 0);

        Assert.True(BadgeEligibilityEvaluator.IsEligible(badge, 0, metrics));
    }

    [Fact]
    public void IsEligible_DuplicateFinder_FourDuplicates_ReturnsFalse_BR_GAM_004()
    {
        var badge = CreateBadge("duplicate_finder");
        var metrics = new BadgeEligibilityMetrics(0, 4, false, 0, 0);

        Assert.False(BadgeEligibilityEvaluator.IsEligible(badge, 0, metrics));
    }

    [Fact]
    public void IsEligible_CommunityVoice_ReporterCount10_ReturnsTrue_BR_GAM_004()
    {
        var badge = CreateBadge("community_voice");
        var metrics = new BadgeEligibilityMetrics(0, 0, true, 0, 0);

        Assert.True(BadgeEligibilityEvaluator.IsEligible(badge, 0, metrics));
    }

    [Fact]
    public void IsEligible_Streak30d_ThirtyDayStreak_ReturnsTrue_BR_GAM_004()
    {
        var badge = CreateBadge("streak_30d");
        var metrics = new BadgeEligibilityMetrics(0, 0, false, 30, 0);

        Assert.True(BadgeEligibilityEvaluator.IsEligible(badge, 0, metrics));
    }

    [Fact]
    public void IsEligible_Streak7d_SevenDayStreak_ReturnsTrue_BR_GAM_004()
    {
        var badge = CreateBadge("streak_7d");
        var metrics = new BadgeEligibilityMetrics(0, 0, false, 7, 0);

        Assert.True(BadgeEligibilityEvaluator.IsEligible(badge, 0, metrics));
    }

    private static Badge CreateBadge(string code) =>
        Badge.Create(code, "Tên VI", "Name EN");
}
