using FluentAssertions;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Gamification.CheckBadges;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Persistence;
using Greenlens.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Greenlens.Application.UnitTests;

/// <summary>
/// Đảm bảo badge unlock + BadgeEarned notification ngay khi đủ điều kiện (BR-GAM-004, BR-NTF-002).
/// </summary>
public sealed class BadgeImmediateUnlockTests
{
    private readonly INotificationService _notifications = Substitute.For<INotificationService>();

    [Fact]
    public async Task CheckBadges_FirstVerifiedReport_AwardsAndNotifiesImmediately_BR_GAM_004()
    {
        var userId = Guid.NewGuid();
        var badge = Badge.Create("first_report", "Người Khởi Đầu", "First Reporter", requiredReportCount: 1);

        await using var ctx = CreateDb();
        ctx.Badges.Add(badge);
        ctx.Reports.Add(CreateVerifiedReport(userId));
        await ctx.SaveChangesAsync();

        var result = await CreateCheckBadgesHandler(ctx)
            .Handle(new CheckBadgesCommand(userId), CancellationToken.None);

        result.Value!.NewlyAwarded.Should().ContainSingle().Which.Should().Be("first_report");
        await _notifications.Received(1).SendFromTemplateAsync(
            userId,
            NotificationType.BadgeEarned,
            Arg.Any<Dictionary<string, string>>(),
            badge.Id,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckBadges_FifthDuplicateReport_AwardsDuplicateFinder_BR_GAM_004()
    {
        var userId = Guid.NewGuid();
        var badge = Badge.Create("duplicate_finder", "Người Phát Hiện Trùng", "Duplicate Finder", requiredActionCount: 5);

        await using var ctx = CreateDb();
        ctx.Badges.Add(badge);
        for (var i = 0; i < 5; i++)
            ctx.Reports.Add(CreateDuplicateReport(userId));
        await ctx.SaveChangesAsync();

        var result = await CreateCheckBadgesHandler(ctx)
            .Handle(new CheckBadgesCommand(userId), CancellationToken.None);

        result.Value!.NewlyAwarded.Should().Contain("duplicate_finder");
        await _notifications.Received(1).SendFromTemplateAsync(
            userId,
            NotificationType.BadgeEarned,
            Arg.Any<Dictionary<string, string>>(),
            badge.Id,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckBadges_ReporterCount10_AwardsCommunityVoice_BR_GAM_004()
    {
        var userId = Guid.NewGuid();
        var badge = Badge.Create("community_voice", "Tiếng Nói Cộng Đồng", "Community Voice", requiredActionCount: 10);
        var report = CreateVerifiedReport(userId);
        typeof(Report).GetProperty(nameof(Report.ReporterCount))!.SetValue(report, 10);

        await using var ctx = CreateDb();
        ctx.Badges.Add(badge);
        ctx.Reports.Add(report);
        await ctx.SaveChangesAsync();

        var result = await CreateCheckBadgesHandler(ctx)
            .Handle(new CheckBadgesCommand(userId), CancellationToken.None);

        result.Value!.NewlyAwarded.Should().Contain("community_voice");
    }

    [Fact]
    public async Task CheckBadges_TwoCompletedCleanups_AwardsCleanupHero_BR_GAM_004()
    {
        var userId = Guid.NewGuid();
        var badge = Badge.Create("cleanup_hero", "Anh Hùng Dọn Dẹp", "Cleanup Hero", requiredActionCount: 2);

        await using var ctx = CreateDb();
        ctx.Badges.Add(badge);
        for (var i = 0; i < 2; i++)
        {
            var ev = CreateCompletedCleanupEvent();
            ctx.CommunityCleanupEvents.Add(ev);
            var participant = CommunityCleanupParticipant.Create(ev.Id, userId, CommunityCleanupParticipantRole.Member);
            participant.CheckIn(10.7626m, 106.6602m);
            ctx.CommunityCleanupParticipants.Add(participant);
        }

        await ctx.SaveChangesAsync();

        var result = await CreateCheckBadgesHandler(ctx)
            .Handle(new CheckBadgesCommand(userId), CancellationToken.None);

        result.Value!.NewlyAwarded.Should().Contain("cleanup_hero");
        await _notifications.Received(1).SendFromTemplateAsync(
            userId,
            NotificationType.BadgeEarned,
            Arg.Any<Dictionary<string, string>>(),
            badge.Id,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckBadges_100TotalPoints_AwardsRisingStarImmediately_BR_GAM_004()
    {
        var userId = Guid.NewGuid();
        var badge = Badge.Create("rising_star", "Ngôi Sao Đang Lên", "Rising Star", requiredPoints: 100);
        var userPoints = UserPoints.Create(userId);
        userPoints.AwardPoints(100, PointReason.ReportVerified, Guid.NewGuid());

        await using var ctx = CreateDb();
        ctx.Badges.Add(badge);
        ctx.UserPoints.Add(userPoints);
        await ctx.SaveChangesAsync();

        var result = await CreateCheckBadgesHandler(ctx)
            .Handle(new CheckBadgesCommand(userId), CancellationToken.None);

        result.Value!.NewlyAwarded.Should().Contain("rising_star");
        await _notifications.Received(1).SendFromTemplateAsync(
            userId,
            NotificationType.BadgeEarned,
            Arg.Any<Dictionary<string, string>>(),
            badge.Id,
            Arg.Any<CancellationToken>());
    }

    private CheckBadgesCommandHandler CreateCheckBadgesHandler(ApplicationDbContext ctx)
    {
        var unitOfWork = new UnitOfWork(
            ctx,
            Substitute.For<IPublisher>(),
            Substitute.For<ITransactionManager>(),
            Substitute.For<IDomainEventCollector>());

        return new CheckBadgesCommandHandler(
            new UserPointsRepository(ctx),
            new BadgeRepository(ctx),
            new UserBadgeRepository(ctx),
            new ReportRepository(ctx),
            ctx,
            _notifications,
            unitOfWork,
            new ChangeTrackerCleaner(ctx),
            NullLogger<CheckBadgesCommandHandler>.Instance);
    }

    private static CommunityCleanupEvent CreateCompletedCleanupEvent()
    {
        var ev = CommunityCleanupEvent.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Cleanup test",
            null,
            DateTime.UtcNow.AddHours(-2),
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow.AddHours(-3),
            50,
            null,
            10.7626m,
            106.6602m);
        typeof(CommunityCleanupEvent).GetProperty(nameof(CommunityCleanupEvent.Status))!
            .SetValue(ev, CommunityCleanupStatus.Completed);
        return ev;
    }

    private static Report CreateVerifiedReport(Guid userId)
    {
        var report = Report.Create(
            $"RPT-{Guid.NewGuid():N}"[..12],
            userId,
            Guid.NewGuid(),
            Severity.Medium,
            "Test",
            10.7626m,
            106.6602m,
            null,
            null,
            null);
        report.Verify(Guid.NewGuid());
        return report;
    }

    private static Report CreateDuplicateReport(Guid userId)
    {
        var report = Report.Create(
            $"RPT-{Guid.NewGuid():N}"[..12],
            userId,
            Guid.NewGuid(),
            Severity.Medium,
            "Dup",
            10.7626m,
            106.6602m,
            null,
            null,
            null);
        report.MarkDuplicate(Guid.NewGuid());
        return report;
    }

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"badge-immediate-{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options);
    }
}
