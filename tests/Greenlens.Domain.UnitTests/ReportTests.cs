using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;

namespace Greenlens.Domain.UnitTests;

public sealed class ReportTests
{
    private static Report CreateTestReport(
        Severity severity = Severity.Medium,
        bool isAnonymous = false) =>
        Report.Create(
            code: "RPT-2026-000001",
            reporterId: Guid.NewGuid(),
            isAnonymous: isAnonymous,
            categoryId: Guid.NewGuid(),
            severity: severity,
            description: "Test pollution report",
            latitude: 10.7626m,
            longitude: 106.6602m,
            address: "123 Đường ABC, Quận 1, TP.HCM",
            wardCode: "00001",
            provinceCode: "79");

    // ── Factory ──

    [Fact]
    public void Create_ShouldSetDefaultValues()
    {
        var report = CreateTestReport();

        Assert.Equal("RPT-2026-000001", report.Code);
        Assert.Equal(ReportStatus.Submitted, report.Status);
        Assert.Equal(Severity.Medium, report.Severity);
        Assert.Equal(SeveritySource.User, report.SeveritySetBy);
        Assert.True(report.AiPending);
        Assert.Equal(1, report.ReporterCount);
        Assert.Equal(0, report.ReopenedCount);
        Assert.NotNull(report.SlaVerifyDueAt);
        Assert.False(report.IsAnonymous);
        Assert.NotNull(report.ReporterId);
    }

    [Fact]
    public void Create_Anonymous_ShouldNullifyReporterId()
    {
        var report = Report.Create(
            code: "RPT-2026-000002",
            reporterId: Guid.NewGuid(),
            isAnonymous: true,
            categoryId: Guid.NewGuid(),
            severity: Severity.Low,
            description: null,
            latitude: 10m,
            longitude: 106m,
            address: null,
            wardCode: null,
            provinceCode: null);

        Assert.True(report.IsAnonymous);
        Assert.Null(report.ReporterId);
    }

    [Fact]
    public void Create_ShouldSetSlaVerifyDueAt24H()
    {
        var before = DateTime.UtcNow.AddHours(23);
        var report = CreateTestReport();
        var after = DateTime.UtcNow.AddHours(25);

        Assert.InRange(report.SlaVerifyDueAt!.Value, before, after);
    }

    // ── Verify ──

    [Fact]
    public void Verify_FromSubmitted_ShouldSucceed()
    {
        var report = CreateTestReport();
        var officerId = Guid.NewGuid();

        report.Verify(officerId);

        Assert.Equal(ReportStatus.Verified, report.Status);
        Assert.Equal(officerId, report.VerifiedBy);
        Assert.NotNull(report.VerifiedAt);
        Assert.NotNull(report.SlaResolveDueAt);
    }

    [Fact]
    public void Verify_WithOverrides_ShouldApplyOverrides()
    {
        var report = CreateTestReport(Severity.Low);
        var newCategoryId = Guid.NewGuid();

        report.Verify(Guid.NewGuid(), overrideSeverity: Severity.Critical, overrideCategoryId: newCategoryId);

        Assert.Equal(Severity.Critical, report.Severity);
        Assert.Equal(SeveritySource.Officer, report.SeveritySetBy);
        Assert.Equal(newCategoryId, report.CategoryId);
    }

    [Fact]
    public void Verify_SlaResolveDue_Critical_ShouldBe3Days()
    {
        var report = CreateTestReport();
        var before = DateTime.UtcNow.AddDays(2);
        var after = DateTime.UtcNow.AddDays(4);

        report.Verify(Guid.NewGuid(), overrideSeverity: Severity.Critical);

        Assert.InRange(report.SlaResolveDueAt!.Value, before, after);
    }

    [Fact]
    public void Verify_FromNonSubmitted_ShouldThrow()
    {
        var report = CreateTestReport();
        report.Verify(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => report.Verify(Guid.NewGuid()));
    }

    // ── Reject ──

    [Fact]
    public void Reject_FromSubmitted_ShouldSucceed()
    {
        var report = CreateTestReport();

        report.Reject("Ảnh không phản ánh ô nhiễm thực tế");

        Assert.Equal(ReportStatus.Rejected, report.Status);
        Assert.Equal("Ảnh không phản ánh ô nhiễm thực tế", report.RejectedReason);
    }

    [Fact]
    public void Reject_FromVerified_ShouldThrow()
    {
        var report = CreateTestReport();
        report.Verify(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => report.Reject("reason"));
    }

    // ── Assign ──

    [Fact]
    public void Assign_FromVerified_ShouldMoveToInProgress()
    {
        var report = CreateTestReport();
        report.Verify(Guid.NewGuid());
        var officerId = Guid.NewGuid();

        report.Assign(officerId);

        Assert.Equal(ReportStatus.InProgress, report.Status);
        Assert.Equal(officerId, report.AssignedOfficerId);
        Assert.NotNull(report.StartedAt);
    }

    [Fact]
    public void Assign_FromSubmitted_ShouldThrow()
    {
        var report = CreateTestReport();

        Assert.Throws<InvalidOperationException>(() => report.Assign(Guid.NewGuid()));
    }

    // ── Resolve ──

    [Fact]
    public void Resolve_FromInProgress_ShouldSucceed()
    {
        var report = CreateTestReport();
        report.Verify(Guid.NewGuid());
        report.Assign(Guid.NewGuid());

        report.Resolve();

        Assert.Equal(ReportStatus.Resolved, report.Status);
        Assert.NotNull(report.ResolvedAt);
    }

    [Fact]
    public void Resolve_FromVerified_ShouldThrow()
    {
        var report = CreateTestReport();
        report.Verify(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => report.Resolve());
    }

    // ── Close ──

    [Fact]
    public void Close_FromResolved_ShouldSucceed()
    {
        var report = CreateTestReport();
        report.Verify(Guid.NewGuid());
        report.Assign(Guid.NewGuid());
        report.Resolve();

        report.Close();

        Assert.Equal(ReportStatus.Closed, report.Status);
        Assert.NotNull(report.ClosedAt);
    }

    // ── Reopen ──

    [Fact]
    public void TryReopen_FromResolved_ShouldSucceed()
    {
        var report = CreateTestReport();
        report.Verify(Guid.NewGuid());
        report.Assign(Guid.NewGuid());
        report.Resolve();

        var result = report.TryReopen();

        Assert.True(result);
        Assert.Equal(ReportStatus.InProgress, report.Status);
        Assert.Equal(1, report.ReopenedCount);
        Assert.Null(report.ResolvedAt);
    }

    [Fact]
    public void TryReopen_ThirdTime_ShouldFail()
    {
        var report = CreateTestReport();
        report.Verify(Guid.NewGuid());
        report.Assign(Guid.NewGuid());

        // Reopen 1
        report.Resolve();
        report.TryReopen();

        // Reopen 2
        report.Resolve();
        report.TryReopen();

        // Reopen 3 → should fail (max 2)
        report.Resolve();
        var result = report.TryReopen();

        Assert.False(result);
        Assert.Equal(ReportStatus.Resolved, report.Status);
        Assert.Equal(2, report.ReopenedCount);
    }

    // ── Duplicate ──

    [Fact]
    public void MarkDuplicate_FromSubmitted_ShouldSucceed()
    {
        var report = CreateTestReport();
        var primaryId = Guid.NewGuid();

        report.MarkDuplicate(primaryId);

        Assert.Equal(ReportStatus.Duplicate, report.Status);
        Assert.Equal(primaryId, report.ParentReportId);
    }

    [Fact]
    public void MarkDuplicate_FromInProgress_ShouldThrow()
    {
        var report = CreateTestReport();
        report.Verify(Guid.NewGuid());
        report.Assign(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => report.MarkDuplicate(Guid.NewGuid()));
    }

    [Fact]
    public void IncrementReporterCount_ShouldIncrement()
    {
        var report = CreateTestReport();

        report.IncrementReporterCount();

        Assert.Equal(2, report.ReporterCount);
    }

    // ── AI ──

    [Fact]
    public void ApplyAiResults_ShouldSetAiFields()
    {
        var report = CreateTestReport();

        report.ApplyAiResults("TRASH", 0.92m, Severity.High);

        Assert.Equal("TRASH", report.AiClassifiedType);
        Assert.Equal(0.92m, report.AiConfidence);
        Assert.Equal(Severity.High, report.AiEstimatedSeverity);
        Assert.False(report.AiPending);
    }

    [Fact]
    public void FlagSuspicious_ShouldMarkSuspicious()
    {
        var report = CreateTestReport();

        report.FlagSuspicious("[\"EDITED_PHOTO\"]");

        Assert.True(report.IsSuspicious);
        Assert.Equal("[\"EDITED_PHOTO\"]", report.SuspiciousReasons);
    }

    // ── Soft Delete ──

    [Fact]
    public void SoftDelete_ShouldMarkDeleted()
    {
        var report = CreateTestReport();

        report.SoftDelete("user-id");

        Assert.True(report.IsDeleted);
        Assert.NotNull(report.DeletedAt);
    }

    // ── Full lifecycle ──

    [Fact]
    public void FullLifecycle_Submit_Verify_Assign_Resolve_Close()
    {
        var report = CreateTestReport(Severity.High);

        // Submit → Verify
        report.Verify(Guid.NewGuid(), overrideSeverity: Severity.Critical);
        Assert.Equal(ReportStatus.Verified, report.Status);

        // Verify → In Progress
        report.Assign(Guid.NewGuid());
        Assert.Equal(ReportStatus.InProgress, report.Status);

        // In Progress → Resolved
        report.Resolve();
        Assert.Equal(ReportStatus.Resolved, report.Status);

        // Resolved → Closed
        report.Close();
        Assert.Equal(ReportStatus.Closed, report.Status);
        Assert.NotNull(report.ClosedAt);
    }
}
