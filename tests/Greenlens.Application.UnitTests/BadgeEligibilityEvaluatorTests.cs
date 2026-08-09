using Greenlens.Application.Features.Gamification;
using Greenlens.Domain.Entities;

namespace Greenlens.Application.UnitTests;

public sealed class BadgeEligibilityEvaluatorTests
{
    private static BadgeEligibilityMetrics Metrics(
        int verified = 0,
        int duplicate = 0,
        int maxReporterCount = 0,
        int streakDays = 0,
        int cleanups = 0) =>
        new(verified, duplicate, maxReporterCount, streakDays, cleanups);

    [Theory]
    [InlineData("first_report", 1, true)]
    [InlineData("first_report", 0, false)]
    [InlineData("eco_warrior", 10, true)]
    [InlineData("eco_warrior", 9, false)]
    [InlineData("green_champion", 50, true)]
    [InlineData("green_champion", 49, false)]
    [InlineData("earth_guardian", 100, true)]
    [InlineData("earth_guardian", 99, false)]
    public void IsEligible_MilestoneBadges_VerifiedReportCount_BR_GAM_004(
        string code, int verifiedCount, bool expected)
    {
        var badge = CreateBadge(code, requiredReportCount: code switch
        {
            "first_report" => 1,
            "eco_warrior" => 10,
            "green_champion" => 50,
            "earth_guardian" => 100,
            _ => null
        });

        Assert.Equal(expected, BadgeEligibilityEvaluator.IsEligible(badge, 0, Metrics(verified: verifiedCount)));
    }

    [Fact]
    public void IsEligible_CleanupHero_TwoCompletedCleanups_ReturnsTrue_BR_GAM_004()
    {
        var badge = CreateBadge("cleanup_hero");

        Assert.True(BadgeEligibilityEvaluator.IsEligible(badge, 0, Metrics(cleanups: 2)));
    }

    [Fact]
    public void IsEligible_CleanupHero_OneCompletedCleanup_ReturnsFalse_BR_GAM_004()
    {
        var badge = CreateBadge("cleanup_hero");

        Assert.False(BadgeEligibilityEvaluator.IsEligible(badge, 0, Metrics(cleanups: 1)));
    }

    [Fact]
    public void IsEligible_DuplicateFinder_FiveDuplicates_ReturnsTrue_BR_GAM_004()
    {
        var badge = CreateBadge("duplicate_finder");

        Assert.True(BadgeEligibilityEvaluator.IsEligible(badge, 0, Metrics(duplicate: 5)));
    }

    [Fact]
    public void IsEligible_DuplicateFinder_FourDuplicates_ReturnsFalse_BR_GAM_004()
    {
        var badge = CreateBadge("duplicate_finder");

        Assert.False(BadgeEligibilityEvaluator.IsEligible(badge, 0, Metrics(duplicate: 4)));
    }

    [Fact]
    public void IsEligible_CommunityVoice_ReporterCount10_ReturnsTrue_BR_GAM_004()
    {
        var badge = CreateBadge("community_voice");

        Assert.True(BadgeEligibilityEvaluator.IsEligible(
            badge, 0, Metrics(maxReporterCount: 10)));
    }

    [Fact]
    public void IsEligible_Streak30d_ThirtyDayStreak_ReturnsTrue_BR_GAM_004()
    {
        var badge = CreateBadge("streak_30d");

        Assert.True(BadgeEligibilityEvaluator.IsEligible(badge, 0, Metrics(streakDays: 30)));
    }

    [Fact]
    public void IsEligible_Streak7d_SevenDayStreak_ReturnsTrue_BR_GAM_004()
    {
        var badge = CreateBadge("streak_7d");

        Assert.True(BadgeEligibilityEvaluator.IsEligible(badge, 0, Metrics(streakDays: 7)));
    }

    [Theory]
    [InlineData("duplicate_finder", 3, 5, "duplicate_reports")]
    [InlineData("cleanup_hero", 1, 2, "cleanup_events")]
    [InlineData("community_voice", 7, 10, "reporter_count")]
    [InlineData("streak_7d", 4, 7, "streak_days")]
    [InlineData("rising_star", 80, 100, "points")]
    [InlineData("eco_warrior", 27, 10, "verified_reports")]
    public void GetProgressValues_AllBadges_ReturnCurrentTargetAndMetric_BR_GAM_004(
        string code, int currentMetricValue, int expectedTarget, string expectedMetric)
    {
        var badge = CreateBadge(code);
        var metrics = code switch
        {
            "duplicate_finder" => Metrics(duplicate: currentMetricValue),
            "cleanup_hero" => Metrics(cleanups: currentMetricValue),
            "community_voice" => Metrics(maxReporterCount: currentMetricValue),
            "streak_7d" => Metrics(streakDays: currentMetricValue),
            "eco_warrior" => Metrics(verified: currentMetricValue),
            _ => Metrics()
        };
        var totalPoints = code == "rising_star" ? currentMetricValue : 0;

        Assert.Equal(currentMetricValue, BadgeEligibilityEvaluator.GetCurrentProgressValue(badge, totalPoints, metrics));
        Assert.Equal(expectedTarget, BadgeEligibilityEvaluator.GetTargetValue(badge));
        Assert.Equal(expectedMetric, BadgeEligibilityEvaluator.GetProgressMetric(badge));
    }

    private static Badge CreateBadge(string code, int? requiredReportCount = null) =>
        Badge.Create(code, "Tên VI", "Name EN", requiredReportCount: requiredReportCount);
}
