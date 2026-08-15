using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Inspection.CreateInspectionReport;
using Greenlens.Application.Features.Notifications;
using Greenlens.Application.Features.Reports.RejectReport;
using Greenlens.Application.Features.Reports.VerifyReport;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Greenlens.Application.UnitTests.Reports;

public sealed class WorkflowAuditLogHandlerTests
{
    private static readonly Guid OfficerId = Guid.NewGuid();
    private static readonly Guid ReporterId = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();
    private static readonly Guid ReportId = Guid.NewGuid();

    private readonly IReportRepository _reports = Substitute.For<IReportRepository>();
    private readonly IReportStatusHistoryRepository _statusHistory = Substitute.For<IReportStatusHistoryRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IAuditLogger _auditLogger = Substitute.For<IAuditLogger>();

    public WorkflowAuditLogHandlerTests()
    {
        _currentUser.UserId.Returns(OfficerId);
    }

    [Fact]
    public async Task VerifyReport_Success_WritesAuditLog_BR_ADM_010()
    {
        var report = Report.Create(
            "REP-001", ReporterId, CategoryId, Severity.Medium,
            "desc", 10.5m, 106.5m, null, null, null);

        _reports.GetByIdAsync(report.Id, Arg.Any<CancellationToken>()).Returns(report);

        var handler = new VerifyReportCommandHandler(
            _reports,
            _statusHistory,
            Substitute.For<IPollutionCategoryRepository>(),
            Substitute.For<IWasteTagRepository>(),
            Substitute.For<IReportWasteTagRepository>(),
            Substitute.For<ILocalOfficeRepository>(),
            _currentUser,
            _uow,
            _auditLogger,
            NullLogger<VerifyReportCommandHandler>.Instance);

        var result = await handler.Handle(
            new VerifyReportCommand(report.Id, null, null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _auditLogger.Received(1).LogAsync(
            "VerifyReport",
            "Report",
            report.Id.ToString(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectReport_Success_CapturesStatusTransition_BR_ADM_010()
    {
        var report = Report.Create(
            "REP-002", ReporterId, CategoryId, Severity.Low,
            "desc", 10.5m, 106.5m, null, null, null);

        _reports.GetByIdAsync(report.Id, Arg.Any<CancellationToken>()).Returns(report);

        var handler = new RejectReportCommandHandler(
            _reports,
            _statusHistory,
            _currentUser,
            _uow,
            _auditLogger,
            NullLogger<RejectReportCommandHandler>.Instance);

        const string reason = "This is a valid rejection reason with enough chars";
        var result = await handler.Handle(
            new RejectReportCommand(report.Id, reason),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _auditLogger.Received(1).LogAsync(
            "RejectReport",
            "Report",
            report.Id.ToString(),
            Arg.Is<string>(s => s.Contains("Submitted")),
            Arg.Is<string>(s => s.Contains("Rejected")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateInspectionReport_Success_WritesAuditLog_BR_ADM_010()
    {
        var report = Report.Create(
            "REP-003", ReporterId, CategoryId, Severity.High,
            "desc", 10.5m, 106.5m, null, null, null);
        report.Verify(OfficerId, null, null);

        _reports.GetByIdAsync(report.Id, Arg.Any<CancellationToken>()).Returns(report);

        var inspections = Substitute.For<IInspectionReportRepository>();
        inspections.GetByReportIdAsync(report.Id, Arg.Any<CancellationToken>())
            .Returns(new List<InspectionReport>());

        var handler = new CreateInspectionReportCommandHandler(
            _reports,
            inspections,
            Substitute.For<IEnvironmentalTeamRepository>(),
            _currentUser,
            _uow,
            _auditLogger,
            Substitute.For<IInspectionTaskAssignedNotifier>(),
            NullLogger<CreateInspectionReportCommandHandler>.Instance);

        var result = await handler.Handle(
            new CreateInspectionReportCommand(
                report.Id,
                null,
                "Violation description here",
                "Violator Name",
                null,
                null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _auditLogger.Received(1).LogAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
