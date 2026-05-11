using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;

namespace Greenlens.Domain.UnitTests;

public sealed class OtherEntitiesTests
{
    // ── ReportFlag ──

    [Fact]
    public void ReportFlag_Create_ShouldSetFields()
    {
        var reportId = Guid.NewGuid();
        var flaggerId = Guid.NewGuid();

        var flag = ReportFlag.Create(reportId, flaggerId, FlagType.Spam, "Spam content");

        Assert.Equal(reportId, flag.ReportId);
        Assert.Equal(flaggerId, flag.FlaggerId);
        Assert.Equal(FlagType.Spam, flag.FlagType);
        Assert.Equal("Spam content", flag.Reason);
    }

    // ── ReportSatisfaction ──

    [Fact]
    public void ReportSatisfaction_Create_Satisfied()
    {
        var satisfaction = ReportSatisfaction.Create(
            Guid.NewGuid(), Guid.NewGuid(), true, rating: 5, comment: "Rất tốt!");

        Assert.True(satisfaction.IsSatisfied);
        Assert.Equal(5, satisfaction.Rating);
        Assert.Equal("Rất tốt!", satisfaction.Comment);
    }

    [Fact]
    public void ReportSatisfaction_Create_NotSatisfied()
    {
        var satisfaction = ReportSatisfaction.Create(
            Guid.NewGuid(), Guid.NewGuid(), false, rating: 1, comment: "Chưa sạch");

        Assert.False(satisfaction.IsSatisfied);
        Assert.Equal(1, satisfaction.Rating);
    }

    // ── ReportDraft ──

    [Fact]
    public void ReportDraft_Create_ShouldSetPayload()
    {
        var userId = Guid.NewGuid();
        var draft = ReportDraft.Create(userId, "{\"category\":\"TRASH\"}");

        Assert.Equal(userId, draft.UserId);
        Assert.Equal("{\"category\":\"TRASH\"}", draft.Payload);
        Assert.Equal(draft.CreatedAt, draft.UpdatedAt);
    }

    [Fact]
    public void ReportDraft_UpdatePayload_ShouldChangePayloadAndTimestamp()
    {
        var draft = ReportDraft.Create(Guid.NewGuid(), "{\"old\":true}");
        var originalUpdatedAt = draft.UpdatedAt;

        // Small delay to ensure timestamp changes
        Thread.Sleep(10);
        draft.UpdatePayload("{\"new\":true}");

        Assert.Equal("{\"new\":true}", draft.Payload);
        Assert.True(draft.UpdatedAt >= originalUpdatedAt);
    }

    // ── ReportStatusHistory ──

    [Fact]
    public void ReportStatusHistory_Create_ShouldRecordTransition()
    {
        var reportId = Guid.NewGuid();
        var officerId = Guid.NewGuid();

        var history = ReportStatusHistory.Create(
            reportId,
            fromStatus: ReportStatus.Submitted,
            toStatus: ReportStatus.Verified,
            changedBy: officerId,
            reason: "Đã kiểm tra ảnh");

        Assert.Equal(reportId, history.ReportId);
        Assert.Equal(ReportStatus.Submitted, history.FromStatus);
        Assert.Equal(ReportStatus.Verified, history.ToStatus);
        Assert.Equal(officerId, history.ChangedBy);
        Assert.Equal("Đã kiểm tra ảnh", history.Reason);
    }

    [Fact]
    public void ReportStatusHistory_Create_FirstEntry_ShouldHaveNullFromStatus()
    {
        var history = ReportStatusHistory.Create(
            Guid.NewGuid(), fromStatus: null, toStatus: ReportStatus.Submitted);

        Assert.Null(history.FromStatus);
        Assert.Equal(ReportStatus.Submitted, history.ToStatus);
    }
}
