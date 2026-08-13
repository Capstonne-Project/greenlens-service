using FluentAssertions;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Reports.AcceptAssignment;
using Greenlens.Application.Features.Reports.GetCompanyQueue;
using Greenlens.Application.Features.Reports.ReassignTeam;
using Greenlens.Application.Features.Notifications;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Persistence;
using Greenlens.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Greenlens.Application.Common.Options;
using NSubstitute;

namespace Greenlens.Application.UnitTests;

public sealed class ReopenAssignmentFlowTests
{
    [Fact]
    public async Task AcceptAssignment_PicksLatestNotCompletedRow_BR_REP_015()
    {
        var leoId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var leaderUserId = Guid.NewGuid();

        var report = Report.Create(
            "RPT-REOPEN",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Severity.Medium,
            "Test",
            10.7626m,
            106.6602m,
            null,
            null,
            null);
        report.Verify(leoId);
        report.Assign(leoId);

        var oldCompleted = ReportAssignment.Create(report.Id, teamId, leoId);
        oldCompleted.Accept();
        oldCompleted.Complete();
        SetAssignedAt(oldCompleted, DateTime.UtcNow.AddDays(-3));

        var newAssigned = ReportAssignment.Create(report.Id, teamId, leoId);
        SetAssignedAt(newAssigned, DateTime.UtcNow);

        var reports = Substitute.For<IReportRepository>();
        var assignments = Substitute.For<IReportAssignmentRepository>();
        var teamMembers = Substitute.For<ITeamMemberRepository>();
        var activityNotifier = Substitute.For<ICleanupAssignmentActivityNotifier>();
        var uow = Substitute.For<IUnitOfWork>();
        var currentUser = Substitute.For<ICurrentUser>();

        currentUser.UserId.Returns(leaderUserId);
        teamMembers.GetLeaderByUserIdAsync(leaderUserId, Arg.Any<CancellationToken>())
            .Returns(TeamMember.Create(teamId, leaderUserId, isLeader: true));
        reports.GetByIdAsync(report.Id, Arg.Any<CancellationToken>()).Returns(report);
        assignments.GetByReportIdAsync(report.Id, Arg.Any<CancellationToken>())
            .Returns([oldCompleted, newAssigned]);

        var sut = new AcceptAssignmentCommandHandler(
            reports,
            assignments,
            teamMembers,
            activityNotifier,
            currentUser,
            uow,
            NullLogger<AcceptAssignmentCommandHandler>.Instance);

        var result = await sut.Handle(new AcceptAssignmentCommand(report.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        newAssigned.Status.Should().Be(AssignmentStatus.InProgress);
        oldCompleted.Status.Should().Be(AssignmentStatus.Completed);
    }

    [Fact]
    public async Task GetCompanyQueue_AfterReopen_ShowsReportDespiteOldCompleted_BR_CMP_005()
    {
        await using var ctx = CreateDb();
        var companyId = Guid.NewGuid();
        var cmUserId = Guid.NewGuid();
        var category = PollutionCategory.Create("waste", "Rác thải", "Waste");
        ctx.PollutionCategories.Add(category);
        await ctx.SaveChangesAsync();

        var leoId = Guid.NewGuid();
        var report = Report.Create(
            "RPT-CQ",
            Guid.NewGuid(),
            category.Id,
            Severity.Medium,
            "Test",
            10.7626m,
            106.6602m,
            null,
            null,
            null);
        report.Verify(leoId);
        report.Assign(leoId);

        var oldCompleted = ReportAssignment.Create(report.Id, Guid.NewGuid(), leoId);
        oldCompleted.Accept();
        oldCompleted.Complete();

        report.Resolve();
        report.ApproveReopen(leoId);
        report.DispatchToCompany(companyId, leoId);

        ctx.Reports.Add(report);
        ctx.ReportAssignments.Add(oldCompleted);
        ctx.CompanyStaff.Add(CompanyStaff.Create(cmUserId, companyId));
        await ctx.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(cmUserId);

        var sut = new GetCompanyQueueQueryHandler(
            new ReportRepository(ctx),
            new ReportMediaRepository(ctx),
            new CompanyStaffRepository(ctx),
            currentUser,
            NullLogger<GetCompanyQueueQueryHandler>.Instance);

        var result = await sut.Handle(new GetCompanyQueueQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle(i => i.ReportId == report.Id);
    }

    [Fact]
    public async Task ReassignTeam_AllowsNewTeamWhenOldLatestIsCompletedHistory_BR_OFF_011()
    {
        var leoId = Guid.NewGuid();
        var officeId = Guid.NewGuid();
        var oldTeamId = Guid.NewGuid();
        var newTeamId = Guid.NewGuid();

        var oldTeam = EnvironmentalTeam.Create("Old Team", officeId, TeamType.Cleanup);
        var newTeam = EnvironmentalTeam.Create("New Team", officeId, TeamType.Cleanup);

        var report = Report.Create(
            "RPT-REASSIGN-REOPEN",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Severity.Medium,
            "Test",
            10.7626m,
            106.6602m,
            null,
            null,
            null);
        report.Verify(leoId);
        report.Assign(leoId);

        var oldCompleted = ReportAssignment.Create(report.Id, oldTeamId, leoId);
        oldCompleted.Accept();
        oldCompleted.Complete();
        SetAssignedAt(oldCompleted, DateTime.UtcNow.AddDays(-5));

        var declined = ReportAssignment.Create(report.Id, oldTeamId, leoId);
        declined.Decline("Đội không thể tiếp nhận thêm nhiệm vụ trong tuần này");
        SetAssignedAt(declined, DateTime.UtcNow.AddDays(-1));

        ReportAssignment? addedAssignment = null;
        var assignments = Substitute.For<IReportAssignmentRepository>();
        assignments.When(x => x.Add(Arg.Any<ReportAssignment>()))
            .Do(call => addedAssignment = call.Arg<ReportAssignment>());
        assignments.GetByReportIdAsync(report.Id, Arg.Any<CancellationToken>())
            .Returns([oldCompleted, declined]);
        assignments.CountInProgressByTeamAsync(newTeamId, Arg.Any<CancellationToken>()).Returns(0);

        var teams = Substitute.For<IEnvironmentalTeamRepository>();
        teams.GetByIdAsync(oldTeamId, Arg.Any<CancellationToken>()).Returns(oldTeam);
        teams.GetByIdAsync(newTeamId, Arg.Any<CancellationToken>()).Returns(newTeam);

        var reports = Substitute.For<IReportRepository>();
        reports.GetByIdAsync(report.Id, Arg.Any<CancellationToken>()).Returns(report);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(leoId);

        var teamMembers = Substitute.For<ITeamMemberRepository>();
        teamMembers.HasMembersAsync(newTeamId, Arg.Any<CancellationToken>()).Returns(true);

        var sut = new ReassignTeamCommandHandler(
            reports,
            teams,
            teamMembers,
            assignments,
            currentUser,
            Substitute.For<IUnitOfWork>(),
            Substitute.For<ICleanupTaskAssignedNotifier>(),
            Options.Create(new WorkloadLimitsOptions { MaxTasksPerTeam = 6, WarningThreshold = 5 }),
            Substitute.For<IAuditLogger>(),
            NullLogger<ReassignTeamCommandHandler>.Instance);

        var reason = "Phân công lại sau reopen cho đội sẵn sàng xử lý";
        var result = await sut.Handle(
            new ReassignTeamCommand(report.Id, oldTeamId, newTeamId, reason),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        addedAssignment.Should().NotBeNull();
        addedAssignment!.TeamId.Should().Be(newTeamId);
    }

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"reopen-assignment-{Guid.NewGuid():N}")
            .Options;

        return new ApplicationDbContext(options);
    }

    private static void SetAssignedAt(ReportAssignment assignment, DateTime assignedAt)
    {
        typeof(ReportAssignment)
            .GetProperty(nameof(ReportAssignment.AssignedAt))!
            .SetValue(assignment, assignedAt);
    }
}
