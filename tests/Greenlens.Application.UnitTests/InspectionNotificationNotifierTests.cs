using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Notifications;
using Greenlens.Application.UnitTests.Helpers;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Greenlens.Application.UnitTests;

public sealed class InspectionTaskAssignedNotifierTests
{
    private readonly INotificationService _notifications = Substitute.For<INotificationService>();
    private readonly IEnvironmentalTeamRepository _teams = Substitute.For<IEnvironmentalTeamRepository>();
    private readonly ITeamMemberRecipientQuery _teamRecipients = Substitute.For<ITeamMemberRecipientQuery>();
    private readonly InspectionTaskAssignedNotifier _sut;

    public InspectionTaskAssignedNotifierTests()
    {
        _sut = new InspectionTaskAssignedNotifier(
            _notifications,
            _teams,
            _teamRecipients,
            NotificationTestDbFactory.CreateEmpty(),
            NullLogger<InspectionTaskAssignedNotifier>.Instance);
    }

    [Fact]
    public async Task NotifyTeamAsync_AllMembersReceiveTemplate_BR_INS_001()
    {
        var teamId = Guid.NewGuid();
        var reportId = Guid.NewGuid();
        var member1 = Guid.NewGuid();
        var member2 = Guid.NewGuid();

        _teams.GetByIdAsync(teamId, Arg.Any<CancellationToken>())
            .Returns(EnvironmentalTeam.Create("Đội Thanh tra A", Guid.NewGuid(), TeamType.Inspection));

        _teamRecipients.GetActiveMemberUserIdsAsync(teamId, Arg.Any<CancellationToken>())
            .Returns(new[] { member1, member2 });

        await _sut.NotifyTeamAsync(teamId, reportId, Guid.NewGuid(), "RPT-800", CancellationToken.None);

        await _notifications.Received(2).SendFromTemplateAsync(
            Arg.Any<Guid>(),
            NotificationType.InspectionTaskAssigned,
            Arg.Is<Dictionary<string, string>>(d =>
                d["report_code"] == "RPT-800" && d["team_name"] == "Đội Thanh tra A"),
            reportId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotifyTeamAsync_NoMembers_SkipsNotification_BR_INS_001()
    {
        var teamId = Guid.NewGuid();

        _teams.GetByIdAsync(teamId, Arg.Any<CancellationToken>())
            .Returns(EnvironmentalTeam.Create("Đội Trống", Guid.NewGuid(), TeamType.Inspection));

        _teamRecipients.GetActiveMemberUserIdsAsync(teamId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Guid>());

        await _sut.NotifyTeamAsync(teamId, Guid.NewGuid(), Guid.NewGuid(), "RPT-801", CancellationToken.None);

        await _notifications.DidNotReceive().SendFromTemplateAsync(
            Arg.Any<Guid>(),
            Arg.Any<NotificationType>(),
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }
}

public sealed class InspectionTaskDeclinedNotifierTests
{
    private readonly IInspectionAssignmentActivityNotifier _activity =
        Substitute.For<IInspectionAssignmentActivityNotifier>();
    private readonly InspectionTaskDeclinedNotifier _sut;

    public InspectionTaskDeclinedNotifierTests()
    {
        _sut = new InspectionTaskDeclinedNotifier(
            _activity,
            NullLogger<InspectionTaskDeclinedNotifier>.Instance);
    }

    [Fact]
    public async Task NotifyLeoAsync_DelegatesToActivityNotifierWithTeamName_BR_INS_003()
    {
        var leoId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var reportId = Guid.NewGuid();
        var inspectionId = Guid.NewGuid();

        await _sut.NotifyLeoAsync(
            leoId,
            teamId,
            reportId,
            inspectionId,
            "RPT-802",
            "Đội đang quá tải nhiệm vụ",
            CancellationToken.None);

        await _activity.Received(1).NotifyDeclinedAsync(
            leoId,
            teamId,
            reportId,
            inspectionId,
            "RPT-802",
            "Đội đang quá tải nhiệm vụ",
            Arg.Any<CancellationToken>());
    }
}

public sealed class InspectionClosedNoViolationNotifierTests
{
    private readonly INotificationService _notifications = Substitute.For<INotificationService>();
    private readonly InspectionClosedNoViolationNotifier _sut;

    public InspectionClosedNoViolationNotifierTests()
    {
        _sut = new InspectionClosedNoViolationNotifier(
            _notifications,
            NullLogger<InspectionClosedNoViolationNotifier>.Instance);
    }

    [Fact]
    public async Task NotifyReporterAsync_AnonymousReport_SkipsNotification_BR_INS_013()
    {
        await _sut.NotifyReporterAsync(
            Guid.NewGuid(),
            "RPT-803",
            reporterId: null,
            "Không đủ bằng chứng",
            CancellationToken.None);

        await _notifications.DidNotReceive().SendFromTemplateAsync(
            Arg.Any<Guid>(),
            Arg.Any<NotificationType>(),
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotifyReporterAsync_RegisteredReporter_SendsTemplate_BR_INS_013()
    {
        var reporterId = Guid.NewGuid();
        var reportId = Guid.NewGuid();

        await _sut.NotifyReporterAsync(
            reportId,
            "RPT-804",
            reporterId,
            "Không đủ bằng chứng xử phạt",
            CancellationToken.None);

        await _notifications.Received(1).SendFromTemplateAsync(
            reporterId,
            NotificationType.InspectionClosedNoViolation,
            Arg.Is<Dictionary<string, string>>(d =>
                d["report_code"] == "RPT-804"
                && d["reason"] == "Không đủ bằng chứng xử phạt"),
            reportId,
            Arg.Any<CancellationToken>());
    }
}
