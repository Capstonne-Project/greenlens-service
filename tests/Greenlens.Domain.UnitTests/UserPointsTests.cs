using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;

namespace Greenlens.Domain.UnitTests;

/// <summary>Unit tests for UserPoints entity — covers BR-GAM-001, 003, 006.</summary>
public sealed class UserPointsTests
{
    private readonly Guid _userId = Guid.NewGuid();

    private UserPoints CreateSut() => UserPoints.Create(_userId);

    // ── BR-GAM-001: Point formula ──

    [Fact]
    public void AwardPoints_Verified_Adds10Points()
    {
        var sut = CreateSut();

        var tx = sut.AwardPoints(10, PointReason.ReportVerified, Guid.NewGuid());

        Assert.NotNull(tx);
        Assert.Equal(10, sut.TotalPoints);
        Assert.Single(sut.Transactions);
    }

    [Fact]
    public void AwardPoints_Resolved_Adds20Points()
    {
        var sut = CreateSut();

        sut.AwardPoints(10, PointReason.ReportVerified, Guid.NewGuid());
        var tx = sut.AwardPoints(20, PointReason.ReportResolved, Guid.NewGuid());

        Assert.NotNull(tx);
        Assert.Equal(30, sut.TotalPoints);
    }

    [Fact]
    public void AwardPoints_Rejected_Deducts5Points()
    {
        var sut = CreateSut();
        sut.AwardPoints(10, PointReason.ReportVerified, Guid.NewGuid());

        sut.AwardPoints(-5, PointReason.ReportRejected, Guid.NewGuid());

        Assert.Equal(5, sut.TotalPoints);
    }

    [Fact]
    public void AwardPoints_NegativeTotal_FloorsAtZero()
    {
        var sut = CreateSut();

        sut.AwardPoints(-5, PointReason.ReportRejected, Guid.NewGuid());

        Assert.Equal(0, sut.TotalPoints);
    }

    // ── BR-GAM-001: Idempotency ──

    [Fact]
    public void AwardPoints_SameReportAndReason_ReturnNull_Idempotent()
    {
        var sut = CreateSut();
        var reportId = Guid.NewGuid();

        var first = sut.AwardPoints(10, PointReason.ReportVerified, reportId);
        var second = sut.AwardPoints(10, PointReason.ReportVerified, reportId);

        Assert.NotNull(first);
        Assert.Null(second); // idempotent skip
        Assert.Equal(10, sut.TotalPoints); // not doubled
        Assert.Single(sut.Transactions);
    }

    [Fact]
    public void AwardPoints_SameReportDifferentReason_AllowsBoth()
    {
        var sut = CreateSut();
        var reportId = Guid.NewGuid();

        var first = sut.AwardPoints(10, PointReason.ReportVerified, reportId);
        var second = sut.AwardPoints(20, PointReason.ReportResolved, reportId);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(30, sut.TotalPoints);
        Assert.Equal(2, sut.Transactions.Count);
    }

    // ── BR-GAM-003: Level computation ──

    [Theory]
    [InlineData(0, 1)]
    [InlineData(50, 1)]
    [InlineData(99, 1)]
    [InlineData(100, 2)]
    [InlineData(499, 2)]
    [InlineData(500, 3)]
    [InlineData(1499, 3)]
    [InlineData(1500, 4)]
    [InlineData(4999, 4)]
    [InlineData(5000, 5)]
    [InlineData(10000, 5)]
    public void Level_ComputesCorrectly(int totalPoints, int expectedLevel)
    {
        var sut = CreateSut();
        // Award exact amount of points
        sut.AwardPoints(totalPoints, PointReason.ReportVerified, Guid.NewGuid());

        Assert.Equal(expectedLevel, sut.Level);
    }

    // ── BR-GAM-003: Level-up event ──

    [Fact]
    public void AwardPoints_LevelUp_RaisesDomainEvent()
    {
        var sut = CreateSut();

        // Go from 0 to 100 → L1 → L2
        sut.AwardPoints(100, PointReason.ReportVerified, Guid.NewGuid());

        var levelUpEvent = sut.DomainEvents
            .OfType<LevelUpEvent>()
            .SingleOrDefault();

        Assert.NotNull(levelUpEvent);
        Assert.Equal(1, levelUpEvent.PreviousLevel);
        Assert.Equal(2, levelUpEvent.NewLevel);
    }

    // ── BR-GAM-006: Lock mechanism ──

    [Fact]
    public void Lock_DeductsAllPoints_SetsLockedState()
    {
        var sut = CreateSut();
        sut.AwardPoints(100, PointReason.ReportVerified, Guid.NewGuid());

        var deducted = sut.Lock("Fraud detected", 30);

        Assert.Equal(0, sut.TotalPoints);
        Assert.True(sut.IsLocked);
        Assert.NotNull(sut.LockedUntil);
        Assert.Equal("Fraud detected", sut.LockedReason);
        Assert.True(deducted < 0); // negative = deducted
    }

    [Fact]
    public void AwardPoints_WhenLocked_ReturnsNull()
    {
        var sut = CreateSut();
        sut.Lock("Test", 30);

        var tx = sut.AwardPoints(10, PointReason.ReportVerified, Guid.NewGuid());

        Assert.Null(tx);
        Assert.Equal(0, sut.TotalPoints);
    }

    [Fact]
    public void Unlock_ClearsLockState()
    {
        var sut = CreateSut();
        sut.Lock("Test", 30);

        sut.Unlock();

        Assert.False(sut.IsLocked);
        Assert.Null(sut.LockedUntil);
        Assert.Null(sut.LockedReason);
    }
}
