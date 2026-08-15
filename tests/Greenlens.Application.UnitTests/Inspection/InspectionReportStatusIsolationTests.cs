using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Inspection.AssignInspectionTeam;
using Greenlens.Application.Features.Inspection.CreateInspectionReport;
using Greenlens.Application.Features.Notifications;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Greenlens.Application.UnitTests.Inspection;

public sealed class InspectionReportStatusIsolationTests
{
    private static readonly Guid OfficerId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private static readonly Guid ReporterId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff");
    private static readonly Guid CategoryId = Guid.Parse("cccccccc-dddd-eeee-ffff-000000000001");
    private static readonly Guid TeamId = Guid.Parse("dddddddd-eeee-ffff-0000-111111111111");

    [Fact]
    public async Task CreateInspectionReport_WithTeam_KeepsReportVerified()
    {
        var report = Report.Create(
            "REP-INS-001", ReporterId, CategoryId, Severity.High,
            "desc", 10.5m, 106.5m, null, null, null);
        report.Verify(OfficerId, null, null);

        var reports = Substitute.For<IReportRepository>();
        reports.GetByIdAsync(report.Id, Arg.Any<CancellationToken>()).Returns(report);

        var inspections = Substitute.For<IInspectionReportRepository>();
        inspections.GetByReportIdAsync(report.Id, Arg.Any<CancellationToken>())
            .Returns(new List<InspectionReport>());

        var team = EnvironmentalTeam.Create("Inspection Team", Guid.NewGuid(), TeamType.Inspection);
        var teams = Substitute.For<IEnvironmentalTeamRepository>();
        teams.GetByIdAsync(TeamId, Arg.Any<CancellationToken>()).Returns(team);

        var handler = new CreateInspectionReportCommandHandler(
            reports,
            inspections,
            teams,
            Substitute.For<ICurrentUser>(),
            Substitute.For<IUnitOfWork>(),
            Substitute.For<IAuditLogger>(),
            Substitute.For<IInspectionTaskAssignedNotifier>(),
            NullLogger<CreateInspectionReportCommandHandler>.Instance);

        var result = await handler.Handle(
            new CreateInspectionReportCommand(
                report.Id,
                TeamId,
                "Violation description here",
                "Violator Name",
                null,
                null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ReportStatus.Verified, report.Status);
    }

    [Fact]
    public async Task AssignInspectionTeam_KeepsReportVerified()
    {
        var report = Report.Create(
            "REP-INS-002", ReporterId, CategoryId, Severity.High,
            "desc", 10.5m, 106.5m, null, null, null);
        report.Verify(OfficerId, null, null);

        var inspection = InspectionReport.Create(report.Id, OfficerId, Severity.High);

        var inspections = Substitute.For<IInspectionReportRepository>();
        inspections.GetByIdAsync(inspection.Id, Arg.Any<CancellationToken>()).Returns(inspection);

        var team = EnvironmentalTeam.Create("Inspection Team", Guid.NewGuid(), TeamType.Inspection);
        var teams = Substitute.For<IEnvironmentalTeamRepository>();
        teams.GetByIdAsync(TeamId, Arg.Any<CancellationToken>()).Returns(team);

        var teamMembers = Substitute.For<ITeamMemberRepository>();
        teamMembers.HasMembersAsync(TeamId, Arg.Any<CancellationToken>()).Returns(true);

        var reports = Substitute.For<IReportRepository>();
        reports.GetByIdAsync(report.Id, Arg.Any<CancellationToken>()).Returns(report);

        var handler = new AssignInspectionTeamCommandHandler(
            inspections,
            reports,
            teams,
            teamMembers,
            Substitute.For<ICurrentUser>(),
            Substitute.For<IUnitOfWork>(),
            Substitute.For<IAuditLogger>(),
            Substitute.For<IInspectionTaskAssignedNotifier>(),
            NullLogger<AssignInspectionTeamCommandHandler>.Instance);

        var result = await handler.Handle(
            new AssignInspectionTeamCommand(inspection.Id, TeamId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ReportStatus.Verified, report.Status);
    }
}
