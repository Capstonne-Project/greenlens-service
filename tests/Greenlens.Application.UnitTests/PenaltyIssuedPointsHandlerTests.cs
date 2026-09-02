using FluentAssertions;
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

public sealed class PenaltyIssuedPointsHandlerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();

    [Fact]
    public async Task Handle_WithReporter_AwardsPenaltyIssuedPoints_BR_GAM_001()
    {
        var reporterId = Guid.NewGuid();
        var reportId = Guid.NewGuid();
        var inspectionId = Guid.NewGuid();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"penalty-points-{Guid.NewGuid():N}")
            .Options;

        await using var ctx = new ApplicationDbContext(options);
        ctx.GamificationConfigs.Add(
            GamificationConfig.Create(PointReason.PenaltyIssued, 20, "Penalty issued"));
        ctx.Reports.Add(Report.Create(
            "RPT-PEN-001",
            reporterId,
            Guid.NewGuid(),
            Severity.Medium,
            "Test",
            10.7626m,
            106.6602m,
            null,
            null,
            null));
        await ctx.SaveChangesAsync();

        var report = await ctx.Reports.FirstAsync();
        var sut = new PenaltyIssuedPointsHandler(
            _sender,
            ctx,
            new ReportRepository(ctx),
            NullLogger<PenaltyIssuedPointsHandler>.Instance);

        _sender.Send(Arg.Any<AwardPointsCommand>(), Arg.Any<CancellationToken>())
            .Returns(new AwardPointsResponse(20, 20, 1, WasSkipped: false));

        await sut.Handle(
            new PenaltyIssuedEvent(inspectionId, report.Id, 1_000_000m, "QD-001", ViolationLevel.Moderate),
            CancellationToken.None);

        await _sender.Received(1).Send(
            Arg.Is<AwardPointsCommand>(c =>
                c.UserId == reporterId
                && c.Points == 20
                && c.Reason == PointReason.PenaltyIssued
                && c.ReportId == report.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AnonymousReport_SkipsPoints_BR_GAM_001()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"penalty-points-anon-{Guid.NewGuid():N}")
            .Options;

        await using var ctx = new ApplicationDbContext(options);
        ctx.GamificationConfigs.Add(
            GamificationConfig.Create(PointReason.PenaltyIssued, 20, "Penalty issued"));
        ctx.Reports.Add(Report.Create(
            "RPT-PEN-ANON",
            reporterId: null,
            Guid.NewGuid(),
            Severity.Medium,
            "Test",
            10.7626m,
            106.6602m,
            null,
            null,
            null));
        await ctx.SaveChangesAsync();

        var report = await ctx.Reports.FirstAsync();
        var sut = new PenaltyIssuedPointsHandler(
            _sender,
            ctx,
            new ReportRepository(ctx),
            NullLogger<PenaltyIssuedPointsHandler>.Instance);

        await sut.Handle(
            new PenaltyIssuedEvent(Guid.NewGuid(), report.Id, 1_000_000m, "QD-001", ViolationLevel.Moderate),
            CancellationToken.None);

        await _sender.DidNotReceive().Send(
            Arg.Any<AwardPointsCommand>(),
            Arg.Any<CancellationToken>());
    }
}
