using Greenlens.Domain.Entities;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;

namespace Greenlens.Domain.UnitTests;

public sealed class ReportTests
{
    private static Report CreateTestReport(
        Severity severity = Severity.Medium) =>
        Report.Create(
            code: "RPT-2026-000001",
            reporterId: Guid.NewGuid(),
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
        Assert.Equal(1, report.ReporterCount);
        Assert.Equal(0, report.ReopenedCount);
        Assert.NotNull(report.SlaVerifyDueAt);
        Assert.NotNull(report.ReporterId);
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

        // BR-ORG-015: Status stays Submitted — re-queued to Department
        Assert.Equal(ReportStatus.Submitted, report.Status);
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
        Assert.Equal(officerId, report.AssignedByOfficerId);
        var inProgressEvt = Assert.Single(report.DomainEvents.OfType<ReportInProgressEvent>());
        Assert.Equal(report.Id, inProgressEvt.ReportId);
    }

    [Fact]
    public void Assign_FromSubmitted_ShouldThrow()
    {
        var report = CreateTestReport();

        Assert.Throws<InvalidOperationException>(() => report.Assign(Guid.NewGuid()));
    }

    // ── Company dispatch ──

    [Fact]
    public void DispatchToCompany_FromVerified_ShouldMoveToInProgress()
    {
        var report = CreateTestReport();
        report.Verify(Guid.NewGuid());
        var leoId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        report.DispatchToCompany(companyId, leoId);

        Assert.Equal(ReportStatus.InProgress, report.Status);
        Assert.Equal(companyId, report.AssignedCompanyId);
        Assert.Equal(leoId, report.AssignedByOfficerId);
        Assert.NotNull(report.DispatchedToCompanyAt);
        Assert.Contains(report.DomainEvents, e => e is ReportInProgressEvent);
    }

    [Fact]
    public void AssignByCompanyManager_FromInProgress_ShouldKeepInProgress()
    {
        var report = CreateTestReport();
        report.Verify(Guid.NewGuid());
        var leoId = Guid.NewGuid();
        var cmId = Guid.NewGuid();
        report.DispatchToCompany(Guid.NewGuid(), leoId);

        report.AssignByCompanyManager(cmId);

        Assert.Equal(ReportStatus.InProgress, report.Status);
        Assert.Equal(cmId, report.AssignedByOfficerId);
    }

    [Fact]
    public void AssignByCompanyManager_WithoutDispatch_ShouldThrow()
    {
        var report = CreateTestReport();
        report.Verify(Guid.NewGuid());
        report.Assign(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => report.AssignByCompanyManager(Guid.NewGuid()));
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

    // ── Reopen (BR-REP-015 v1.2: citizen request + LEO approve) ──

    [Fact]
    public void CanRequestReopen_FromResolved_ShouldReturnTrue_BR_REP_015()
    {
        var report = CreateTestReport();
        report.Verify(Guid.NewGuid());
        report.Assign(Guid.NewGuid());
        report.Resolve();

        Assert.True(report.CanRequestReopen(DateTime.UtcNow));
    }

    [Fact]
    public void ApproveReopen_FromResolved_ChangesToReopened_BR_REP_015()
    {
        var report = CreateTestReport();
        report.Verify(Guid.NewGuid());
        report.Assign(Guid.NewGuid());
        report.Resolve();
        report.MarkPendingReopenRequest();

        var result = report.ApproveReopen(Guid.NewGuid());

        Assert.True(result);
        Assert.Equal(ReportStatus.Reopened, report.Status);
        Assert.Equal(1, report.ReopenedCount);
        Assert.False(report.HasPendingReopenRequest);
        Assert.Null(report.ResolvedAt);
    }

    [Fact]
    public void ApproveReopen_SecondTime_ShouldFail_BR_REP_015()
    {
        var report = CreateTestReport();
        report.Verify(Guid.NewGuid());
        report.Assign(Guid.NewGuid());
        report.Resolve();
        report.ApproveReopen(Guid.NewGuid());
        report.Assign(Guid.NewGuid());
        report.Resolve();

        var result = report.ApproveReopen(Guid.NewGuid());

        Assert.False(result);
        Assert.Equal(ReportStatus.Resolved, report.Status);
        Assert.Equal(1, report.ReopenedCount);
    }

    [Fact]
    public void CanRequestReopen_AfterOneApprove_ShouldReturnFalse_BR_REP_015()
    {
        var report = CreateTestReport();
        report.Verify(Guid.NewGuid());
        report.Assign(Guid.NewGuid());
        report.Resolve();
        report.ApproveReopen(Guid.NewGuid());
        report.Assign(Guid.NewGuid());
        report.Resolve();

        Assert.False(report.CanRequestReopen(DateTime.UtcNow));
    }

    [Fact]
    public void Assign_FromReopened_MovesToInProgress_BR_OFF_011()
    {
        var report = CreateTestReport();
        report.Verify(Guid.NewGuid());
        report.Assign(Guid.NewGuid());
        report.Resolve();
        report.ApproveReopen(Guid.NewGuid());

        report.Assign(Guid.NewGuid());

        Assert.Equal(ReportStatus.InProgress, report.Status);
    }

    // ── Duplicate ──

    [Fact]
    public void MarkDuplicate_FromSubmitted_ShouldSucceed_BR_REP_032()
    {
        var report = CreateTestReport();
        var primaryId = Guid.NewGuid();

        report.MarkDuplicate(primaryId);

        Assert.Equal(ReportStatus.Duplicate, report.Status);
        Assert.Equal(primaryId, report.ParentReportId);
    }

    [Fact]
    public void MarkDuplicate_FromVerified_ShouldSucceed_BR_REP_032()
    {
        var report = CreateTestReport();
        report.Verify(Guid.NewGuid());

        report.MarkDuplicate(Guid.NewGuid());

        Assert.Equal(ReportStatus.Duplicate, report.Status);
    }

    [Fact]
    public void MarkDuplicate_FromInProgress_ShouldThrow_BR_REP_032()
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

    // ── Duplicate detection (BR-REP-030..033) ──

    [Fact]
    public void MarkPossibleDuplicate_Tier1_SetsFlagAndSource_BR_REP_030()
    {
        var report = CreateTestReport();
        var candidateId = Guid.NewGuid();

        report.MarkPossibleDuplicate(candidateId, DuplicateDetectionSources.Tier1);

        Assert.True(report.IsPossibleDuplicate);
        Assert.Equal(candidateId, report.PossibleDuplicateOfReportId);
        Assert.Equal(DuplicateDetectionSources.Tier1, report.DuplicateDetectionSource);
        Assert.Null(report.AiSimilarityScore);
    }

    [Fact]
    public void MarkPossibleDuplicate_RaisesFlaggedEvent_BR_REP_031()
    {
        var report = CreateTestReport();
        var candidateId = Guid.NewGuid();

        report.MarkPossibleDuplicate(candidateId, DuplicateDetectionSources.Tier1);

        var evt = Assert.Single(report.DomainEvents.OfType<ReportPossibleDuplicateFlaggedEvent>());
        Assert.Equal(report.Id, evt.ReportId);
        Assert.Equal(candidateId, evt.CandidateReportId);
    }

    [Fact]
    public void DismissDuplicate_ClearsFlag_BR_REP_031()
    {
        var report = CreateTestReport();
        report.MarkPossibleDuplicate(Guid.NewGuid(), DuplicateDetectionSources.Tier1);

        report.DismissDuplicate();

        Assert.False(report.IsPossibleDuplicate);
        Assert.Null(report.PossibleDuplicateOfReportId);
        Assert.Null(report.DuplicateDetectionSource);
        Assert.Null(report.AiSimilarityScore);
    }

    [Fact]
    public void ApplyDuplicateAiResult_SameScene_UpgradesSourceAndScore_BR_AI_002()
    {
        var report = CreateTestReport();
        report.MarkPossibleDuplicate(Guid.NewGuid(), DuplicateDetectionSources.Tier1);

        report.ApplyDuplicateAiResult(isSameScene: true, confidence: 0.87m);

        Assert.True(report.IsPossibleDuplicate);
        Assert.Equal(DuplicateDetectionSources.Tier2Ai, report.DuplicateDetectionSource);
        Assert.Equal(0.87m, report.AiSimilarityScore);
    }

    [Fact]
    public void ApplyDuplicateAiResult_DifferentScene_DismissesFlag_BR_AI_002()
    {
        var report = CreateTestReport();
        report.MarkPossibleDuplicate(Guid.NewGuid(), DuplicateDetectionSources.Tier1);

        report.ApplyDuplicateAiResult(isSameScene: false, confidence: 0.20m);

        Assert.False(report.IsPossibleDuplicate);
        Assert.Null(report.PossibleDuplicateOfReportId);
        Assert.Null(report.DuplicateDetectionSource);
    }

    [Fact]
    public void ApplyDuplicateAiResult_WhenNotFlagged_IsNoOp_BR_AI_002()
    {
        var report = CreateTestReport();

        report.ApplyDuplicateAiResult(isSameScene: true, confidence: 0.99m);

        Assert.False(report.IsPossibleDuplicate);
        Assert.Null(report.DuplicateDetectionSource);
        Assert.Null(report.AiSimilarityScore);
    }

    [Fact]
    public void ApplyDuplicateAiResult_WhenAlreadyDuplicate_IsNoOp_BR_REP_031()
    {
        var report = CreateTestReport();
        report.MarkPossibleDuplicate(Guid.NewGuid(), DuplicateDetectionSources.Tier1);
        report.MarkDuplicate(Guid.NewGuid());

        report.ApplyDuplicateAiResult(isSameScene: true, confidence: 0.99m);

        Assert.Equal(ReportStatus.Duplicate, report.Status);
        Assert.False(report.IsPossibleDuplicate);
        Assert.Equal(DuplicateDetectionSources.Tier1, report.DuplicateDetectionSource);
        Assert.Null(report.AiSimilarityScore);
    }

    [Fact]
    public void MarkDuplicate_ClearsPossibleDuplicateFlag_BR_REP_032()
    {
        var report = CreateTestReport();
        report.MarkPossibleDuplicate(Guid.NewGuid(), DuplicateDetectionSources.Tier1);

        report.MarkDuplicate(Guid.NewGuid());

        Assert.Equal(ReportStatus.Duplicate, report.Status);
        Assert.False(report.IsPossibleDuplicate);
    }

    [Fact]
    public void MarkDuplicate_RaisesMergedEvent_BR_REP_032()
    {
        var report = CreateTestReport();
        var primaryId = Guid.NewGuid();

        report.MarkDuplicate(primaryId);

        var evt = Assert.Single(report.DomainEvents.OfType<ReportMarkedDuplicateEvent>());
        Assert.Equal(report.Id, evt.ReportId);
        Assert.Equal(primaryId, evt.PrimaryReportId);
    }

    // ── AI ──

    [Fact]
    public void Create_WithoutPreSubmitAi_DoesNotSetAiPending_BR_AI_001()
    {
        var report = CreateTestReport();

        Assert.False(report.AiPending);
    }

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

    [Fact]
    public void AnonymizeReporter_ShouldClearReporterIdAndHideName_BR_AUTH_021()
    {
        var reporterId = Guid.NewGuid();
        var report = Report.Create(
            code: "RPT-2026-000099",
            reporterId: reporterId,
            categoryId: Guid.NewGuid(),
            severity: Severity.Medium,
            description: "Test",
            latitude: 10.7626m,
            longitude: 106.6602m,
            address: "123 ABC",
            wardCode: "00001",
            provinceCode: "79");

        report.AnonymizeReporter();

        Assert.Null(report.ReporterId);
        Assert.True(report.HideReporterName);
    }
}
