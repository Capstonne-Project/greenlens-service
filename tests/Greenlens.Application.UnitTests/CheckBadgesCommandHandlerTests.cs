using FluentAssertions;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Features.Gamification;
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

public sealed class CheckBadgesCommandHandlerTests
{
    private readonly INotificationService _notifications = Substitute.For<INotificationService>();

    [Fact]
    public async Task Handle_AlreadyEarnedBadge_SkipsSecondNotification_BR_GAM_004()
    {
        var userId = Guid.NewGuid();
        var badge = Badge.Create(
            "first_report",
            "Người Khởi Đầu",
            "First Reporter",
            requiredReportCount: 1);

        await using var ctx = CreateDb();
        ctx.Badges.Add(badge);
        ctx.UserBadges.Add(UserBadge.Create(userId, badge.Id));
        ctx.Reports.Add(CreateVerifiedReport(userId));
        await ctx.SaveChangesAsync();

        var sut = CreateHandler(ctx);

        var result = await sut.Handle(new CheckBadgesCommand(userId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.NewlyAwarded.Should().BeEmpty();
        await _notifications.DidNotReceive().SendFromTemplateAsync(
            Arg.Any<Guid>(),
            Arg.Any<NotificationType>(),
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingBadgeEarnedNotification_SkipsResend_BR_NTF_002()
    {
        var userId = Guid.NewGuid();
        var badge = Badge.Create(
            "first_report",
            "Người Khởi Đầu",
            "First Reporter",
            requiredReportCount: 1);

        await using var ctx = CreateDb();
        ctx.Badges.Add(badge);
        ctx.Reports.Add(CreateVerifiedReport(userId));
        ctx.Notifications.Add(Notification.Create(
            userId,
            NotificationType.BadgeEarned,
            "Chúc mừng!",
            "Bạn vừa nhận được huy hiệu Người Khởi Đầu",
            referenceId: badge.Id));
        await ctx.SaveChangesAsync();

        var sut = CreateHandler(ctx);

        var result = await sut.Handle(new CheckBadgesCommand(userId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.NewlyAwarded.Should().ContainSingle().Which.Should().Be("first_report");
        (await ctx.UserBadges.CountAsync()).Should().Be(1);
        await _notifications.DidNotReceive().SendFromTemplateAsync(
            Arg.Any<Guid>(),
            Arg.Any<NotificationType>(),
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OneStepFromEcoWarrior_SendsBadgeProgressNear_BR_NTF_002()
    {
        var userId = Guid.NewGuid();
        var badge = Badge.Create(
            "eco_warrior",
            "Chiến Binh Xanh",
            "Eco Warrior",
            requiredReportCount: 10);

        await using var ctx = CreateDb();
        ctx.Badges.Add(badge);
        for (var i = 0; i < 9; i++)
            ctx.Reports.Add(CreateVerifiedReport(userId));
        await ctx.SaveChangesAsync();

        var sut = CreateHandler(ctx);

        var result = await sut.Handle(new CheckBadgesCommand(userId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _notifications.Received(1).SendFromTemplateAsync(
            userId,
            NotificationType.BadgeProgressNear,
            GamificationNotificationPlaceholders.Empty,
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MultipleNearBadges_SendsSingleBadgeProgressNear_BR_NTF_002()
    {
        var userId = Guid.NewGuid();
        var ecoWarrior = Badge.Create(
            "eco_warrior", "Chiến Binh Xanh", "Eco Warrior", requiredReportCount: 10);
        var duplicateFinder = Badge.Create(
            "duplicate_finder", "Người Phát Hiện Trùng", "Duplicate Finder");

        await using var ctx = CreateDb();
        ctx.Badges.AddRange(ecoWarrior, duplicateFinder);
        for (var i = 0; i < 9; i++)
            ctx.Reports.Add(CreateVerifiedReport(userId));
        ctx.Reports.Add(CreateDuplicateReport(userId));
        for (var i = 0; i < 3; i++)
            ctx.Reports.Add(CreateDuplicateReport(userId));
        await ctx.SaveChangesAsync();

        var sut = CreateHandler(ctx);

        await sut.Handle(new CheckBadgesCommand(userId), CancellationToken.None);

        await _notifications.Received(1).SendFromTemplateAsync(
            userId,
            NotificationType.BadgeProgressNear,
            GamificationNotificationPlaceholders.Empty,
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_HalfwayEcoWarrior_SendsBadgeProgressNear_BR_NTF_002()
    {
        var userId = Guid.NewGuid();
        var badge = Badge.Create(
            "eco_warrior",
            "Chiến Binh Xanh",
            "Eco Warrior",
            requiredReportCount: 10);

        await using var ctx = CreateDb();
        ctx.Badges.Add(badge);
        for (var i = 0; i < 5; i++)
            ctx.Reports.Add(CreateVerifiedReport(userId));
        await ctx.SaveChangesAsync();

        var sut = CreateHandler(ctx);

        await sut.Handle(new CheckBadgesCommand(userId), CancellationToken.None);

        await _notifications.Received(1).SendFromTemplateAsync(
            userId,
            NotificationType.BadgeProgressNear,
            GamificationNotificationPlaceholders.Empty,
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BadgeProgressNearAlreadySent_SkipsResend_BR_NTF_002()
    {
        var userId = Guid.NewGuid();
        var badge = Badge.Create(
            "eco_warrior",
            "Chiến Binh Xanh",
            "Eco Warrior",
            requiredReportCount: 10);

        await using var ctx = CreateDb();
        ctx.Badges.Add(badge);
        for (var i = 0; i < 9; i++)
            ctx.Reports.Add(CreateVerifiedReport(userId));
        ctx.Notifications.Add(Notification.Create(
            userId,
            NotificationType.BadgeProgressNear,
            "Sắp đạt huy hiệu mới",
            "Bạn đang rất gần với một danh hiệu mới.",
            referenceId: null));
        await ctx.SaveChangesAsync();

        var sut = CreateHandler(ctx);

        await sut.Handle(new CheckBadgesCommand(userId), CancellationToken.None);

        await _notifications.DidNotReceive().SendFromTemplateAsync(
            userId,
            NotificationType.BadgeProgressNear,
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }

    private CheckBadgesCommandHandler CreateHandler(ApplicationDbContext ctx)
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

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"check-badges-{Guid.NewGuid():N}")
            .Options;

        return new ApplicationDbContext(options);
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
            "Duplicate test",
            10.7626m,
            106.6602m,
            null,
            null,
            null);
        report.MarkDuplicate(Guid.NewGuid());
        return report;
    }
}
