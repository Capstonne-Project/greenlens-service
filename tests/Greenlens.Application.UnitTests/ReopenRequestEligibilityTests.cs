using Greenlens.Application.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.UnitTests;

public sealed class ReopenRequestEligibilityTests
{
    [Fact]
    public void ValidateCitizenCanRequest_FromClosed_ReturnsCannotReopenFromClosed_BR_REP_015()
    {
        var report = CreateResolvedReport();
        report.Close();

        var error = ReopenRequestEligibility.ValidateCitizenCanRequest(report, DateTime.UtcNow, 7, 1);

        Assert.NotNull(error);
        Assert.Equal("CANNOT_REOPEN_FROM_CLOSED", error!.Code);
    }

    [Fact]
    public void ValidateCitizenCanRequest_FromInProgress_ReturnsCannotReopenNotResolved_BR_REP_015()
    {
        var report = CreateTestReport();
        report.Verify(Guid.NewGuid());
        report.Assign(Guid.NewGuid());

        var error = ReopenRequestEligibility.ValidateCitizenCanRequest(report, DateTime.UtcNow, 7, 1);

        Assert.NotNull(error);
        Assert.Equal("CANNOT_REOPEN_NOT_RESOLVED", error!.Code);
    }

    [Fact]
    public void ValidateCitizenCanRequest_PendingExists_ReturnsPendingReopenRequestExists_BR_REP_015()
    {
        var report = CreateResolvedReport();
        report.MarkPendingReopenRequest();

        var error = ReopenRequestEligibility.ValidateCitizenCanRequest(report, DateTime.UtcNow, 7, 1);

        Assert.NotNull(error);
        Assert.Equal("PENDING_REOPEN_REQUEST_EXISTS", error!.Code);
    }

    [Fact]
    public void ValidateCitizenCanRequest_AfterApprovedLimit_ReturnsReopenLimitReached_BR_REP_015()
    {
        var report = CreateResolvedReport();
        report.ApproveReopen(Guid.NewGuid());
        report.Assign(Guid.NewGuid());
        report.Resolve();

        var error = ReopenRequestEligibility.ValidateCitizenCanRequest(report, DateTime.UtcNow, 7, 1);

        Assert.NotNull(error);
        Assert.Equal("REOPEN_LIMIT_REACHED", error!.Code);
    }

    [Fact]
    public void ValidateCitizenCanRequest_FromResolvedWithinWindow_ReturnsNull_BR_REP_015()
    {
        var report = CreateResolvedReport();

        var error = ReopenRequestEligibility.ValidateCitizenCanRequest(report, DateTime.UtcNow, 7, 1);

        Assert.Null(error);
    }

    private static Report CreateTestReport() =>
        Report.Create(
            code: "RPT-2026-000001",
            reporterId: Guid.NewGuid(),
            categoryId: Guid.NewGuid(),
            severity: Severity.Medium,
            description: "Test pollution report",
            latitude: 10.7626m,
            longitude: 106.6602m,
            address: "123 Đường ABC, Quận 1, TP.HCM",
            wardCode: "00001",
            provinceCode: "79");

    private static Report CreateResolvedReport()
    {
        var report = CreateTestReport();
        report.Verify(Guid.NewGuid());
        report.Assign(Guid.NewGuid());
        report.Resolve();
        return report;
    }
}
