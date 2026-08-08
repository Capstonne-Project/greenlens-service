using FluentAssertions;
using Greenlens.Application.Common;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.UnitTests;

public sealed class DuplicateTier1PrimarySelectorTests
{
    private static readonly decimal Lat = 10.7626m;
    private static readonly decimal Lng = 106.6602m;

    [Fact]
    public void SelectPrimary_TenDuplicates_AllResolveToSameVerifiedPrimary_BR_REP_030()
    {
        var primaryId = Guid.NewGuid();
        var t0 = DateTime.UtcNow.AddHours(-2);
        var nearby = new List<DuplicateNearbyReport>
        {
            new(primaryId, Lat, Lng, ReportStatus.Verified, t0),
        };

        for (var i = 1; i <= 10; i++)
        {
            nearby.Add(new(Guid.NewGuid(), Lat, Lng, ReportStatus.Submitted, t0.AddMinutes(i)));
        }

        foreach (var duplicate in nearby.Skip(1))
        {
            var selected = DuplicateTier1PrimarySelector.SelectPrimary(Lat, Lng, nearby.Where(n => n.Id != duplicate.Id));
            selected.Should().Be(primaryId);
        }
    }

    [Fact]
    public void SelectPrimary_PrefersVerifiedPrimaryOverOlderSubmitted_BR_REP_030()
    {
        var olderSubmitted = Guid.NewGuid();
        var verifiedPrimary = Guid.NewGuid();
        var t0 = DateTime.UtcNow.AddHours(-3);

        var nearby = new[]
        {
            new DuplicateNearbyReport(olderSubmitted, Lat, Lng, ReportStatus.Submitted, t0),
            new DuplicateNearbyReport(verifiedPrimary, Lat, Lng, ReportStatus.Verified, t0.AddHours(1)),
        };

        DuplicateTier1PrimarySelector.SelectPrimary(Lat, Lng, nearby)
            .Should().Be(verifiedPrimary);
    }

    [Fact]
    public void SelectPrimary_OnlySubmittedPicksOldest_BR_REP_030()
    {
        var oldest = Guid.NewGuid();
        var newer = Guid.NewGuid();
        var t0 = DateTime.UtcNow.AddHours(-1);

        var nearby = new[]
        {
            new DuplicateNearbyReport(newer, Lat, Lng, ReportStatus.Submitted, t0.AddMinutes(30)),
            new DuplicateNearbyReport(oldest, Lat, Lng, ReportStatus.Submitted, t0),
        };

        DuplicateTier1PrimarySelector.SelectPrimary(Lat, Lng, nearby)
            .Should().Be(oldest);
    }

    [Fact]
    public void SelectPrimary_InProgressPrimary_StillSelectedForNewDuplicate_BR_REP_030()
    {
        var primaryId = Guid.NewGuid();
        var newDuplicateId = Guid.NewGuid();
        var t0 = DateTime.UtcNow.AddHours(-2);

        var nearby = new[]
        {
            new DuplicateNearbyReport(primaryId, Lat, Lng, ReportStatus.InProgress, t0),
            new DuplicateNearbyReport(newDuplicateId, Lat, Lng, ReportStatus.Submitted, t0.AddMinutes(5)),
        };

        DuplicateTier1PrimarySelector.SelectPrimary(Lat, Lng, nearby.Where(n => n.Id != newDuplicateId))
            .Should().Be(primaryId);
    }

    [Fact]
    public void SelectPrimary_IgnoresClosedAutoClosedPrimary_BR_REP_016()
    {
        var t0 = DateTime.UtcNow.AddDays(-30);
        var nearby = new[]
        {
            new DuplicateNearbyReport(Guid.NewGuid(), Lat, Lng, ReportStatus.Closed, t0),
        };

        DuplicateTier1PrimarySelector.SelectPrimary(Lat, Lng, nearby)
            .Should().BeNull();
    }

    [Fact]
    public void SelectPrimary_SkipsClosedAndUsesActivePrimary_BR_REP_030()
    {
        var activePrimary = Guid.NewGuid();
        var t0 = DateTime.UtcNow.AddDays(-30);

        var nearby = new[]
        {
            new DuplicateNearbyReport(Guid.NewGuid(), Lat, Lng, ReportStatus.Closed, t0),
            new DuplicateNearbyReport(activePrimary, Lat, Lng, ReportStatus.Verified, t0.AddDays(1)),
        };

        DuplicateTier1PrimarySelector.SelectPrimary(Lat, Lng, nearby)
            .Should().Be(activePrimary);
    }
}
