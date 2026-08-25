using FluentAssertions;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Options;
using Greenlens.Application.Features.Notifications;
using Greenlens.Application.UnitTests.TestDoubles;
using Greenlens.Application.Features.Reports.DeclineAssignment;
using Greenlens.Application.Features.Reports.ReassignCompanyTeam;
using Greenlens.Application.Features.Reports.ReassignTeam;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Greenlens.Application.UnitTests;

public sealed class DeclineAssignmentCommandHandlerTests
{
    private readonly IReportRepository _reports = Substitute.For<IReportRepository>();
    private readonly IReportAssignmentRepository _assignments = Substitute.For<IReportAssignmentRepository>();
    private readonly ICleanupAssignmentActivityNotifier _activityNotifier = Substitute.For<ICleanupAssignmentActivityNotifier>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly DeclineAssignmentCommandHandler _sut;

    public DeclineAssignmentCommandHandlerTests()
    {
        _sut = new DeclineAssignmentCommandHandler(
            _reports,
            _assignments,
            _activityNotifier,
            new DefaultSystemSettingsProvider(),
            _uow,
            NullLogger<DeclineAssignmentCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_TeamDeclines_KeepsReportInProgress_BR_CLN_007()
    {
        var leoId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var report = Report.Create(
            "RPT-DECLINE",
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

        var assignment = ReportAssignment.Create(report.Id, teamId, leoId);
        var reason = "Đội đang quá tải nhiệm vụ khác trong khu vực";

        _reports.GetByIdAsync(report.Id, Arg.Any<CancellationToken>()).Returns(report);
        _assignments.GetByReportIdAsync(report.Id, Arg.Any<CancellationToken>())
            .Returns([assignment]);

        var result = await _sut.Handle(
            new DeclineAssignmentCommand(report.Id, teamId, reason),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        report.Status.Should().Be(ReportStatus.InProgress);
        assignment.Status.Should().Be(AssignmentStatus.Declined);
        assignment.DeclineReason.Should().Be(reason);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public sealed class ReassignTeamCommandHandlerTests
{
    private readonly IReportRepository _reports = Substitute.For<IReportRepository>();
    private readonly IEnvironmentalTeamRepository _teams = Substitute.For<IEnvironmentalTeamRepository>();
    private readonly ITeamMemberRepository _teamMembers = Substitute.For<ITeamMemberRepository>();
    private readonly IReportAssignmentRepository _assignments = Substitute.For<IReportAssignmentRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ICleanupTaskAssignedNotifier _taskNotifier = Substitute.For<ICleanupTaskAssignedNotifier>();
    private readonly IAuditLogger _auditLogger = Substitute.For<IAuditLogger>();
    private readonly ReassignTeamCommandHandler _sut;

    public ReassignTeamCommandHandlerTests()
    {
        _sut = new ReassignTeamCommandHandler(
            _reports,
            _teams,
            _teamMembers,
            _assignments,
            _currentUser,
            _uow,
            _taskNotifier,
            Options.Create(new WorkloadLimitsOptions { MaxTasksPerTeam = 6, WarningThreshold = 5 }),
            _auditLogger,
            NullLogger<ReassignTeamCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_AlreadyDeclinedAssignment_CreatesNewAssignment_BR_OFF_012()
    {
        var leoId = Guid.NewGuid();
        var officeId = Guid.NewGuid();
        var oldTeamId = Guid.NewGuid();
        var newTeamId = Guid.NewGuid();

        var oldTeam = EnvironmentalTeam.Create("Old Team", officeId, TeamType.Cleanup);
        var newTeam = EnvironmentalTeam.Create("New Team", officeId, TeamType.Cleanup);

        var report = Report.Create(
            "RPT-REASSIGN",
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

        var declined = ReportAssignment.Create(report.Id, oldTeamId, leoId);
        declined.Decline("Đội đang quá tải nhiệm vụ khác trong khu vực");

        ReportAssignment? addedAssignment = null;
        _assignments.When(x => x.Add(Arg.Any<ReportAssignment>()))
            .Do(call => addedAssignment = call.Arg<ReportAssignment>());

        _currentUser.UserId.Returns(leoId);
        _reports.GetByIdAsync(report.Id, Arg.Any<CancellationToken>()).Returns(report);
        _teams.GetByIdAsync(oldTeamId, Arg.Any<CancellationToken>()).Returns(oldTeam);
        _teams.GetByIdAsync(newTeamId, Arg.Any<CancellationToken>()).Returns(newTeam);
        _teamMembers.HasMembersAsync(newTeamId, Arg.Any<CancellationToken>()).Returns(true);
        _assignments.CountInProgressByTeamAsync(newTeamId, Arg.Any<CancellationToken>()).Returns(0);
        _assignments.GetByReportIdAsync(report.Id, Arg.Any<CancellationToken>()).Returns([declined]);

        var leoReason = "Phân công lại cho đội có năng lực xử lý tại khu vực";
        var result = await _sut.Handle(
            new ReassignTeamCommand(report.Id, oldTeamId, newTeamId, leoReason),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        declined.Status.Should().Be(AssignmentStatus.Declined);
        declined.DeclineReason.Should().Be("Đội đang quá tải nhiệm vụ khác trong khu vực");
        addedAssignment.Should().NotBeNull();
        addedAssignment!.TeamId.Should().Be(newTeamId);
        addedAssignment.Status.Should().Be(AssignmentStatus.Assigned);
        await _taskNotifier.Received(1).NotifyTeamAsync(newTeamId, report.Id, report.Code, Arg.Any<CancellationToken>());
    }
}

public sealed class ReassignCompanyTeamCommandHandlerTests
{
    private readonly IReportRepository _reports = Substitute.For<IReportRepository>();
    private readonly IEnvironmentalTeamRepository _teams = Substitute.For<IEnvironmentalTeamRepository>();
    private readonly ITeamMemberRepository _teamMembers = Substitute.For<ITeamMemberRepository>();
    private readonly IReportAssignmentRepository _assignments = Substitute.For<IReportAssignmentRepository>();
    private readonly ICompanyStaffRepository _companyStaff = Substitute.For<ICompanyStaffRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ICleanupTaskAssignedNotifier _taskNotifier = Substitute.For<ICleanupTaskAssignedNotifier>();
    private readonly IAuditLogger _auditLogger = Substitute.For<IAuditLogger>();
    private readonly ReassignCompanyTeamCommandHandler _sut;

    public ReassignCompanyTeamCommandHandlerTests()
    {
        _sut = new ReassignCompanyTeamCommandHandler(
            _reports,
            _teams,
            _teamMembers,
            _assignments,
            _companyStaff,
            _currentUser,
            _uow,
            _taskNotifier,
            Options.Create(new WorkloadLimitsOptions { MaxTasksPerTeam = 6, WarningThreshold = 5 }),
            _auditLogger,
            NullLogger<ReassignCompanyTeamCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_DeclinedAssignment_ReassignsWithinCompany_KeepsReportInProgress_BR_CMP_005()
    {
        var cmId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var oldTeamId = Guid.NewGuid();
        var newTeamId = Guid.NewGuid();

        var oldTeam = EnvironmentalTeam.CreateCompanyTeam("Old Team", TeamType.Cleanup, companyId);
        var newTeam = EnvironmentalTeam.CreateCompanyTeam("New Team", TeamType.Cleanup, companyId);

        var report = Report.Create(
            "RPT-CM-REASSIGN",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Severity.Medium,
            "Test",
            10.7626m,
            106.6602m,
            null,
            null,
            null);
        report.Verify(cmId);
        report.DispatchToCompany(companyId, cmId);

        var declined = ReportAssignment.Create(report.Id, oldTeamId, cmId);
        declined.Decline("Đội đang quá tải nhiệm vụ khác trong khu vực");

        ReportAssignment? addedAssignment = null;
        _assignments.When(x => x.Add(Arg.Any<ReportAssignment>()))
            .Do(call => addedAssignment = call.Arg<ReportAssignment>());

        _currentUser.UserId.Returns(cmId);
        _companyStaff.GetByUserIdAsync(cmId, Arg.Any<CancellationToken>())
            .Returns(CompanyStaff.Create(cmId, companyId));
        _reports.GetByIdAsync(report.Id, Arg.Any<CancellationToken>()).Returns(report);
        _teams.GetByIdAsync(oldTeamId, Arg.Any<CancellationToken>()).Returns(oldTeam);
        _teams.GetByIdAsync(newTeamId, Arg.Any<CancellationToken>()).Returns(newTeam);
        _teamMembers.HasMembersAsync(newTeamId, Arg.Any<CancellationToken>()).Returns(true);
        _assignments.CountInProgressByTeamAsync(newTeamId, Arg.Any<CancellationToken>()).Returns(0);
        _assignments.GetByReportIdAsync(report.Id, Arg.Any<CancellationToken>()).Returns([declined]);

        var reason = "Phân công lại cho đội có năng lực xử lý tại khu vực";
        var result = await _sut.Handle(
            new ReassignCompanyTeamCommand(report.Id, oldTeamId, newTeamId, reason),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        report.Status.Should().Be(ReportStatus.InProgress);
        declined.Status.Should().Be(AssignmentStatus.Declined);
        addedAssignment.Should().NotBeNull();
        addedAssignment!.TeamId.Should().Be(newTeamId);
        addedAssignment.Status.Should().Be(AssignmentStatus.Assigned);
        await _taskNotifier.Received(1).NotifyTeamAsync(newTeamId, report.Id, report.Code, Arg.Any<CancellationToken>());
    }
}
