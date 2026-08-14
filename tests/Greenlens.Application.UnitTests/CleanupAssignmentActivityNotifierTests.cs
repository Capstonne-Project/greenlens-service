using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Notifications;
using Greenlens.Application.UnitTests.Helpers;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Greenlens.Application.UnitTests;

public sealed class CleanupAssignmentActivityNotifierTests
{
    private readonly INotificationService _notifications = Substitute.For<INotificationService>();
    private readonly IEnvironmentalTeamRepository _teams = Substitute.For<IEnvironmentalTeamRepository>();
    private readonly IEnvironmentalServiceCompanyRepository _companies = Substitute.For<IEnvironmentalServiceCompanyRepository>();
    private readonly ICompanyManagerRecipientQuery _companyManagers = Substitute.For<ICompanyManagerRecipientQuery>();
    private readonly CleanupAssignmentActivityNotifier _sut;

    public CleanupAssignmentActivityNotifierTests()
    {
        _companyManagers.GetActiveManagerIdsByCompanyAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Guid>());

        _sut = new CleanupAssignmentActivityNotifier(
            _notifications,
            _teams,
            _companies,
            _companyManagers,
            NotificationTestDbFactory.CreateEmpty(),
            NullLogger<CleanupAssignmentActivityNotifier>.Instance);
    }

    [Fact]
    public async Task NotifyDeclinedAsync_SendsDeclinedTemplateWithTeamName_BR_CLN_007()
    {
        var assignerId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var reportId = Guid.NewGuid();

        _teams.GetByIdAsync(teamId, Arg.Any<CancellationToken>())
            .Returns(EnvironmentalTeam.Create("Đội Dọn Xanh", Guid.NewGuid(), TeamType.Cleanup));

        await _sut.NotifyDeclinedAsync(
            assignerId,
            teamId,
            reportId,
            "RPT-901",
            "Đội đang quá tải nhiệm vụ khác",
            CancellationToken.None);

        await _notifications.Received(1).SendFromTemplateAsync(
            assignerId,
            NotificationType.CleanupTaskDeclined,
            Arg.Is<Dictionary<string, string>>(d =>
                d["report_code"] == "RPT-901"
                && d["team_name"] == "Đội Dọn Xanh"
                && d["decline_reason"] == "Đội đang quá tải nhiệm vụ khác"),
            reportId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotifyProgressUpdatedAsync_SendsProgressTemplate_BR_CLN_004()
    {
        var assignerId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var reportId = Guid.NewGuid();

        _teams.GetByIdAsync(teamId, Arg.Any<CancellationToken>())
            .Returns(EnvironmentalTeam.Create("Đội Công ty A", Guid.NewGuid(), TeamType.Cleanup));

        await _sut.NotifyProgressUpdatedAsync(
            assignerId,
            teamId,
            reportId,
            "RPT-902",
            65,
            CancellationToken.None);

        await _notifications.Received(1).SendFromTemplateAsync(
            assignerId,
            NotificationType.CleanupProgressUpdated,
            Arg.Is<Dictionary<string, string>>(d =>
                d["report_code"] == "RPT-902"
                && d["team_name"] == "Đội Công ty A"
                && d["progress_percent"] == "65"),
            reportId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotifyCompletedAsync_AllTeamsDone_IncludesResolutionNote_BR_CLN_005()
    {
        var assignerId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var reportId = Guid.NewGuid();

        _teams.GetByIdAsync(teamId, Arg.Any<CancellationToken>())
            .Returns(EnvironmentalTeam.Create("Đội Phường 1", Guid.NewGuid(), TeamType.Cleanup));

        await _sut.NotifyCompletedAsync(
            assignerId,
            teamId,
            reportId,
            "RPT-903",
            reportFullyResolved: true,
            CancellationToken.None);

        await _notifications.Received(1).SendFromTemplateAsync(
            assignerId,
            NotificationType.CleanupTaskCompleted,
            Arg.Is<Dictionary<string, string>>(d =>
                d["report_code"] == "RPT-903"
                && d["team_name"] == "Đội Phường 1"
                && d["resolution_note"].Contains("Đã xử lý")),
            reportId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotifyAcceptedAsync_SendsAcceptedTemplate_BR_CLN_001()
    {
        var assignerId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var reportId = Guid.NewGuid();

        _teams.GetByIdAsync(teamId, Arg.Any<CancellationToken>())
            .Returns(EnvironmentalTeam.Create("Đội Tình nguyện", Guid.NewGuid(), TeamType.Cleanup));

        await _sut.NotifyAcceptedAsync(
            assignerId,
            teamId,
            reportId,
            "RPT-904",
            CancellationToken.None);

        await _notifications.Received(1).SendFromTemplateAsync(
            assignerId,
            NotificationType.CleanupTaskAccepted,
            Arg.Is<Dictionary<string, string>>(d =>
                d["report_code"] == "RPT-904"
                && d["team_name"] == "Đội Tình nguyện"),
            reportId,
            Arg.Any<CancellationToken>());
    }
}
