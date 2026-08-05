using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Notifications;
using Greenlens.Application.Features.Notifications.EventHandlers;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Greenlens.Application.UnitTests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Greenlens.Application.UnitTests;

public sealed class ReportInProgressNotificationHandlerTests
{
    private readonly INotificationService _notifications = Substitute.For<INotificationService>();
    private readonly ReportInProgressNotificationHandler _sut;

    public ReportInProgressNotificationHandlerTests()
    {
        _sut = new ReportInProgressNotificationHandler(
            _notifications,
            Substitute.For<IReportRepository>(),
            NullLogger<ReportInProgressNotificationHandler>.Instance);
    }

    [Fact]
    public async Task Handle_AnonymousReport_SkipsNotification_BR_NTF_002()
    {
        var evt = new ReportInProgressEvent(Guid.NewGuid(), ReporterId: null);

        await _sut.Handle(evt, CancellationToken.None);

        await _notifications.DidNotReceive().SendFromTemplateAsync(
            Arg.Any<Guid>(),
            Arg.Any<NotificationType>(),
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }
}

public sealed class CleanupTaskAssignedNotifierTests
{
    private readonly INotificationService _notifications = Substitute.For<INotificationService>();
    private readonly IEnvironmentalTeamRepository _teams = Substitute.For<IEnvironmentalTeamRepository>();
    private readonly ITeamMemberRecipientQuery _teamRecipients = Substitute.For<ITeamMemberRecipientQuery>();
    private readonly CleanupTaskAssignedNotifier _sut;

    public CleanupTaskAssignedNotifierTests()
    {
        _sut = new CleanupTaskAssignedNotifier(
            _notifications,
            _teams,
            _teamRecipients,
            NotificationTestDbFactory.CreateEmpty(),
            NullLogger<CleanupTaskAssignedNotifier>.Instance);
    }

    [Fact]
    public async Task NotifyTeamAsync_AllMembersReceiveTemplate_BR_CLN_001()
    {
        var teamId = Guid.NewGuid();
        var reportId = Guid.NewGuid();
        var member1 = Guid.NewGuid();
        var member2 = Guid.NewGuid();

        _teams.GetByIdAsync(teamId, Arg.Any<CancellationToken>())
            .Returns(EnvironmentalTeam.Create("Đội Xanh", Guid.NewGuid(), TeamType.Cleanup));

        _teamRecipients.GetActiveMemberUserIdsAsync(teamId, Arg.Any<CancellationToken>())
            .Returns(new[] { member1, member2 });

        await _sut.NotifyTeamAsync(teamId, reportId, "RPT-700", CancellationToken.None);

        await _notifications.Received(2).SendFromTemplateAsync(
            Arg.Any<Guid>(),
            NotificationType.CleanupTaskAssigned,
            Arg.Is<Dictionary<string, string>>(d =>
                d["report_code"] == "RPT-700" && d["team_name"] == "Đội Xanh"),
            reportId,
            Arg.Any<CancellationToken>());

        await _notifications.Received(1).SendFromTemplateAsync(
            member1,
            NotificationType.CleanupTaskAssigned,
            Arg.Any<Dictionary<string, string>>(),
            reportId,
            Arg.Any<CancellationToken>());

        await _notifications.Received(1).SendFromTemplateAsync(
            member2,
            NotificationType.CleanupTaskAssigned,
            Arg.Any<Dictionary<string, string>>(),
            reportId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotifyTeamAsync_NoMembers_SkipsNotification_BR_CLN_001()
    {
        var teamId = Guid.NewGuid();

        _teams.GetByIdAsync(teamId, Arg.Any<CancellationToken>())
            .Returns(EnvironmentalTeam.Create("Đội Trống", Guid.NewGuid(), TeamType.Cleanup));

        _teamRecipients.GetActiveMemberUserIdsAsync(teamId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Guid>());

        await _sut.NotifyTeamAsync(teamId, Guid.NewGuid(), "RPT-701", CancellationToken.None);

        await _notifications.DidNotReceive().SendFromTemplateAsync(
            Arg.Any<Guid>(),
            Arg.Any<NotificationType>(),
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }
}
