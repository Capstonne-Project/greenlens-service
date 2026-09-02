using FluentAssertions;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Gamification.AwardPoints;
using Greenlens.Application.Features.Gamification.CheckBadges;
using Greenlens.Application.Features.Gamification.EventHandlers;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Persistence;
using Greenlens.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Greenlens.Application.UnitTests;

public sealed class GamificationReportPointsFlowTests
{
    private readonly ISender _sender = Substitute.For<ISender>();

    [Fact]
    public async Task ReportVerifiedEvent_DispatchesAwardPoints_BR_GAM_001()
    {
        var reporterId = Guid.NewGuid();
        var reportId = Guid.NewGuid();

        await using var ctx = CreateDb();
        SeedConfigs(ctx);

        _sender.Send(Arg.Any<AwardPointsCommand>(), Arg.Any<CancellationToken>())
            .Returns(new AwardPointsResponse(10, 10, 1, WasSkipped: false));

        var sut = new ReportVerifiedPointsHandler(
            _sender, ctx, NullLogger<ReportVerifiedPointsHandler>.Instance);

        await sut.Handle(new ReportVerifiedEvent(reportId, reporterId), CancellationToken.None);

        await _sender.Received(1).Send(
            Arg.Is<AwardPointsCommand>(c =>
                c.UserId == reporterId
                && c.Points == 10
                && c.Reason == PointReason.ReportVerified
                && c.ReportId == reportId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReportResolvedEvent_DispatchesAwardPointsImmediately_BR_GAM_001()
    {
        var reporterId = Guid.NewGuid();
        var reportId = Guid.NewGuid();

        await using var ctx = CreateDb();
        SeedConfigs(ctx);

        _sender.Send(Arg.Any<AwardPointsCommand>(), Arg.Any<CancellationToken>())
            .Returns(new AwardPointsResponse(20, 20, 1, WasSkipped: false));

        var sut = new ReportResolvedPointsHandler(
            _sender, ctx, NullLogger<ReportResolvedPointsHandler>.Instance);

        await sut.Handle(new ReportResolvedEvent(reportId, reporterId), CancellationToken.None);

        await _sender.Received(1).Send(
            Arg.Is<AwardPointsCommand>(c =>
                c.UserId == reporterId
                && c.Points == 20
                && c.Reason == PointReason.ReportResolved
                && c.ReportId == reportId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AwardPointsHandler_PersistsReportResolvedPoints_BR_GAM_001()
    {
        var user = User.Create($"user-{Guid.NewGuid():N}@test.local", "hash", "Test User", UserRole.Citizen);
        var reportId = Guid.NewGuid();

        await using var ctx = CreateDb();
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        var sender = Substitute.For<ISender>();
        var handler = new AwardPointsCommandHandler(
            new UserPointsRepository(ctx),
            new UnitOfWork(
                ctx,
                Substitute.For<IPublisher>(),
                Substitute.For<ITransactionManager>(),
                Substitute.For<IDomainEventCollector>()),
            new ChangeTrackerCleaner(ctx),
            sender,
            NullLogger<AwardPointsCommandHandler>.Instance);

        var result = await handler.Handle(
            new AwardPointsCommand(user.Id, 20, PointReason.ReportResolved, reportId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.WasSkipped.Should().BeFalse();
        result.Value.PointsAwarded.Should().Be(20);

        var stored = await ctx.PointTransactions
            .Include(t => t.UserPointsAggregate)
            .SingleAsync(t => t.UserPointsAggregate!.UserId == user.Id);

        stored.Reason.Should().Be(PointReason.ReportResolved);
        stored.ReportId.Should().Be(reportId);
    }

    [Fact]
    public async Task AwardPointsHandler_SkipsDuplicateReportReason_BR_GAM_001()
    {
        var user = User.Create($"user-{Guid.NewGuid():N}@test.local", "hash", "Test User", UserRole.Citizen);
        var reportId = Guid.NewGuid();

        await using var ctx = CreateDb();
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        var sender = Substitute.For<ISender>();
        var handler = new AwardPointsCommandHandler(
            new UserPointsRepository(ctx),
            new UnitOfWork(
                ctx,
                Substitute.For<IPublisher>(),
                Substitute.For<ITransactionManager>(),
                Substitute.For<IDomainEventCollector>()),
            new ChangeTrackerCleaner(ctx),
            sender,
            NullLogger<AwardPointsCommandHandler>.Instance);

        var command = new AwardPointsCommand(user.Id, 10, PointReason.ReportVerified, reportId);

        (await handler.Handle(command, CancellationToken.None)).Value!.WasSkipped.Should().BeFalse();
        (await handler.Handle(command, CancellationToken.None)).Value!.WasSkipped.Should().BeTrue();
        (await ctx.PointTransactions.CountAsync()).Should().Be(1);
    }

    private static void SeedConfigs(ApplicationDbContext ctx)
    {
        ctx.GamificationConfigs.Add(GamificationConfig.Create(
            PointReason.ReportVerified, 10, "Verified"));
        ctx.GamificationConfigs.Add(GamificationConfig.Create(
            PointReason.ReportResolved, 20, "Resolved"));
    }

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"gamification-flow-{Guid.NewGuid():N}")
            .Options;

        return new ApplicationDbContext(options);
    }
}
