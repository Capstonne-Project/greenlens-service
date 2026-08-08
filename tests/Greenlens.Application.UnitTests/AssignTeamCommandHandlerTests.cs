using FluentAssertions;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Options;
using Greenlens.Application.Features.Notifications;
using Greenlens.Application.Features.Reports.AssignTeam;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Greenlens.Application.UnitTests;

public sealed class AssignTeamCommandHandlerTests
{
    private readonly IReportRepository _reports = Substitute.For<IReportRepository>();
    private readonly IEnvironmentalTeamRepository _teams = Substitute.For<IEnvironmentalTeamRepository>();
    private readonly ITeamMemberRepository _teamMembers = Substitute.For<ITeamMemberRepository>();
    private readonly IReportAssignmentRepository _assignments = Substitute.For<IReportAssignmentRepository>();
    private readonly IReportStatusHistoryRepository _statusHistory = Substitute.For<IReportStatusHistoryRepository>();
    private readonly IWasteTagRepository _wasteTags = Substitute.For<IWasteTagRepository>();
    private readonly IReportWasteTagRepository _reportWasteTags = Substitute.For<IReportWasteTagRepository>();
    private readonly ICommunityCleanupEventRepository _communityCleanupEvents = Substitute.For<ICommunityCleanupEventRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ICleanupTaskAssignedNotifier _taskNotifier = Substitute.For<ICleanupTaskAssignedNotifier>();
    private readonly IAuditLogger _auditLogger = Substitute.For<IAuditLogger>();
    private readonly AssignTeamCommandHandler _sut;

    public AssignTeamCommandHandlerTests()
    {
        _sut = new AssignTeamCommandHandler(
            _reports,
            _teams,
            _teamMembers,
            _assignments,
            _statusHistory,
            _wasteTags,
            _reportWasteTags,
            _communityCleanupEvents,
            _currentUser,
            _uow,
            _taskNotifier,
            Options.Create(new WorkloadLimitsOptions { MaxTasksPerTeam = 6, WarningThreshold = 5 }),
            _auditLogger,
            NullLogger<AssignTeamCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_TeamHasNoMembers_ReturnsTeamHasNoMembers_BR_OFF_011()
    {
        var leoId = Guid.NewGuid();
        var officeId = Guid.NewGuid();
        var team = EnvironmentalTeam.Create("Cleanup A", officeId, TeamType.Cleanup);
        var report = Report.Create(
            "RPT-TEST-001",
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

        _currentUser.UserId.Returns(leoId);
        _reports.GetByIdAsync(report.Id, Arg.Any<CancellationToken>()).Returns(report);
        _communityCleanupEvents.GetActiveByReportIdAsync(report.Id, Arg.Any<CancellationToken>())
            .Returns((CommunityCleanupEvent?)null);
        _teams.GetByIdAsync(team.Id, Arg.Any<CancellationToken>()).Returns(team);
        _teamMembers.HasMembersAsync(team.Id, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.Handle(
            new AssignTeamCommand(report.Id, [new TeamAssignmentItem(team.Id, null)], null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("TEAM_HAS_NO_MEMBERS");
        result.Error.Type.Should().Be(ErrorType.BusinessRule);
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
