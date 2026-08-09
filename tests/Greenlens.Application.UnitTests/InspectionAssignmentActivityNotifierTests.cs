using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Notifications;
using Greenlens.Application.UnitTests.Helpers;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Greenlens.Application.UnitTests;

public sealed class InspectionAssignmentActivityNotifierTests
{
    private readonly INotificationService _notifications = Substitute.For<INotificationService>();
    private readonly IEnvironmentalTeamRepository _teams = Substitute.For<IEnvironmentalTeamRepository>();
    private readonly InspectionAssignmentActivityNotifier _sut;

    public InspectionAssignmentActivityNotifierTests()
    {
        _sut = new InspectionAssignmentActivityNotifier(
            _notifications,
            _teams,
            NotificationTestDbFactory.CreateEmpty(),
            NullLogger<InspectionAssignmentActivityNotifier>.Instance);
    }

    [Fact]
    public async Task NotifyAcceptedAsync_SendsAcceptedTemplate_BR_INS_001()
    {
        var leoId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var reportId = Guid.NewGuid();
        var inspectionId = Guid.NewGuid();

        _teams.GetByIdAsync(teamId, Arg.Any<CancellationToken>())
            .Returns(EnvironmentalTeam.Create("Đội Thanh tra A", Guid.NewGuid(), TeamType.Inspection));

        await _sut.NotifyAcceptedAsync(leoId, teamId, reportId, inspectionId, "RPT-810", CancellationToken.None);

        await _notifications.Received(1).SendFromTemplateAsync(
            leoId,
            NotificationType.InspectionTaskAccepted,
            Arg.Is<Dictionary<string, string>>(d =>
                d["report_code"] == "RPT-810" && d["team_name"] == "Đội Thanh tra A"),
            inspectionId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotifyProgressUpdatedAsync_SendsProgressTemplate_BR_INS_033()
    {
        var leoId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var reportId = Guid.NewGuid();
        var inspectionId = Guid.NewGuid();

        _teams.GetByIdAsync(teamId, Arg.Any<CancellationToken>())
            .Returns(EnvironmentalTeam.Create("Đội Thanh tra B", Guid.NewGuid(), TeamType.Inspection));

        await _sut.NotifyProgressUpdatedAsync(
            leoId,
            teamId,
            reportId,
            inspectionId,
            "RPT-811",
            InspectionActivityLabels.ChecklistUpdated,
            CancellationToken.None);

        await _notifications.Received(1).SendFromTemplateAsync(
            leoId,
            NotificationType.InspectionProgressUpdated,
            Arg.Is<Dictionary<string, string>>(d =>
                d["report_code"] == "RPT-811"
                && d["team_name"] == "Đội Thanh tra B"
                && d["activity_summary"] == InspectionActivityLabels.ChecklistUpdated),
            inspectionId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotifyCompletedAsync_SendsCompletedTemplate_BR_INS_012()
    {
        var leoId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var reportId = Guid.NewGuid();
        var inspectionId = Guid.NewGuid();

        _teams.GetByIdAsync(teamId, Arg.Any<CancellationToken>())
            .Returns(EnvironmentalTeam.Create("Đội Thanh tra C", Guid.NewGuid(), TeamType.Inspection));

        await _sut.NotifyCompletedAsync(
            leoId,
            teamId,
            reportId,
            inspectionId,
            "RPT-812",
            InspectionActivityLabels.PenaltyIssued,
            CancellationToken.None);

        await _notifications.Received(1).SendFromTemplateAsync(
            leoId,
            NotificationType.InspectionTaskCompleted,
            Arg.Is<Dictionary<string, string>>(d =>
                d["report_code"] == "RPT-812"
                && d["team_name"] == "Đội Thanh tra C"
                && d["outcome_summary"] == InspectionActivityLabels.PenaltyIssued),
            inspectionId,
            Arg.Any<CancellationToken>());
    }
}
