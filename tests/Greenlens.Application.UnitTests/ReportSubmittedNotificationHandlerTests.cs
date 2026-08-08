using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Features.Notifications;
using Greenlens.Application.Features.Notifications.EventHandlers;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Persistence;
using Greenlens.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Greenlens.Application.UnitTests;

public sealed class ReportSubmittedNotificationHandlerTests
{
    private readonly INotificationService _notifications = Substitute.For<INotificationService>();

    [Fact]
    public async Task Handle_NormalReport_SendsVerificationNeeded_BR_NTF_002()
    {
        var officeId = Guid.NewGuid();
        var (ctx, reportRepo, reportId, leoId) = await CreateFixtureAsync(
            isPossibleDuplicate: false,
            isSuspectedViolationRecurrence: false,
            officeId);

        var sut = CreateSut(reportRepo, ctx);

        await sut.Handle(new ReportSubmittedEvent(reportId), CancellationToken.None);

        await _notifications.Received(1).SendFromTemplateAsync(
            leoId,
            NotificationType.ReportVerificationNeeded,
            Arg.Is<Dictionary<string, string>>(d => d["report_code"] == "RPT-FLAG-001"),
            reportId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PossibleDuplicate_SkipsVerificationNeeded_BR_REP_030()
    {
        var officeId = Guid.NewGuid();
        var (ctx, reportRepo, reportId, _) = await CreateFixtureAsync(
            isPossibleDuplicate: true,
            isSuspectedViolationRecurrence: false,
            officeId);

        var sut = CreateSut(reportRepo, ctx);

        await sut.Handle(new ReportSubmittedEvent(reportId), CancellationToken.None);

        await _notifications.DidNotReceive().SendFromTemplateAsync(
            Arg.Any<Guid>(),
            NotificationType.ReportVerificationNeeded,
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SuspectedViolationRecurrence_SkipsVerificationNeeded_BR_REP_034()
    {
        var officeId = Guid.NewGuid();
        var (ctx, reportRepo, reportId, _) = await CreateFixtureAsync(
            isPossibleDuplicate: false,
            isSuspectedViolationRecurrence: true,
            officeId);

        var sut = CreateSut(reportRepo, ctx);

        await sut.Handle(new ReportSubmittedEvent(reportId), CancellationToken.None);

        await _notifications.DidNotReceive().SendFromTemplateAsync(
            Arg.Any<Guid>(),
            NotificationType.ReportVerificationNeeded,
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }

    private ReportSubmittedNotificationHandler CreateSut(
        ReportRepository reportRepo,
        ApplicationDbContext ctx) =>
        new(
            _notifications,
            reportRepo,
            ctx,
            NullLogger<ReportSubmittedNotificationHandler>.Instance);

    private static async Task<(ApplicationDbContext Ctx, ReportRepository Repo, Guid ReportId, Guid LeoId)>
        CreateFixtureAsync(
            bool isPossibleDuplicate,
            bool isSuspectedViolationRecurrence,
            Guid officeId)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"report-submitted-noti-{Guid.NewGuid():N}")
            .Options;

        var ctx = new ApplicationDbContext(options);

        var leo = User.Create("leo.officer@test.local", "hash", "LEO Officer", UserRole.LEO);
        leo.AssignToLocalOffice(officeId);
        ctx.Users.Add(leo);

        var report = Report.Create(
            code: "RPT-FLAG-001",
            reporterId: Guid.NewGuid(),
            categoryId: Guid.NewGuid(),
            severity: Severity.Medium,
            description: "Test",
            latitude: 10.7626m,
            longitude: 106.6602m,
            address: "Test address",
            wardCode: "00001",
            provinceCode: "79");

        report.RouteToLocalOffice(officeId, Guid.NewGuid());

        if (isPossibleDuplicate)
            report.MarkPossibleDuplicate(Guid.NewGuid(), DuplicateDetectionSources.Tier1);

        if (isSuspectedViolationRecurrence)
            report.MarkSuspectedViolationRecurrence(Guid.NewGuid());

        ctx.Reports.Add(report);
        await ctx.SaveChangesAsync();

        return (ctx, new ReportRepository(ctx), report.Id, leo.Id);
    }
}

public sealed class PossibleDuplicateFlaggedNotificationHandlerTests
{
    private readonly INotificationService _notifications = Substitute.For<INotificationService>();

    [Fact]
    public async Task Handle_Tier1Flag_SendsDuplicateReviewNeeded_BR_REP_030()
    {
        var officeId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"possible-dup-noti-{Guid.NewGuid():N}")
            .Options;

        var ctx = new ApplicationDbContext(options);

        var leo = User.Create("leo.dup@test.local", "hash", "LEO Dup", UserRole.LEO);
        leo.AssignToLocalOffice(officeId);
        ctx.Users.Add(leo);

        var primary = Report.Create(
            code: "RPT-PRIMARY",
            reporterId: Guid.NewGuid(),
            categoryId: Guid.NewGuid(),
            severity: Severity.Low,
            description: "Primary",
            latitude: 10.7626m,
            longitude: 106.6602m,
            address: "Primary address",
            wardCode: "00001",
            provinceCode: "79");

        var report = Report.Create(
            code: "RPT-DUP-NEW",
            reporterId: Guid.NewGuid(),
            categoryId: Guid.NewGuid(),
            severity: Severity.Medium,
            description: "Duplicate candidate",
            latitude: 10.7627m,
            longitude: 106.6603m,
            address: "New address",
            wardCode: "00001",
            provinceCode: "79");
        report.RouteToLocalOffice(officeId, Guid.NewGuid());
        report.MarkPossibleDuplicate(primary.Id, DuplicateDetectionSources.Tier1);

        ctx.Reports.AddRange(primary, report);
        await ctx.SaveChangesAsync();

        var sut = new PossibleDuplicateFlaggedNotificationHandler(
            _notifications,
            new ReportRepository(ctx),
            ctx,
            NullLogger<PossibleDuplicateFlaggedNotificationHandler>.Instance);

        await sut.Handle(
            new ReportPossibleDuplicateFlaggedEvent(report.Id, primary.Id),
            CancellationToken.None);

        await _notifications.Received(1).SendFromTemplateAsync(
            leo.Id,
            NotificationType.DuplicateReviewNeeded,
            Arg.Is<Dictionary<string, string>>(d =>
                d["report_code"] == "RPT-DUP-NEW"
                && d["detection_summary"].Contains("RPT-PRIMARY", StringComparison.Ordinal)),
            report.Id,
            Arg.Any<CancellationToken>());
    }
}
