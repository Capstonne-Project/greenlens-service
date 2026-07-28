using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;

namespace Greenlens.Domain.UnitTests;

public sealed class CommunityCleanupEventTests
{
    private static CommunityCleanupEvent CreateTestEvent() =>
        CommunityCleanupEvent.Create(
            reportId: Guid.NewGuid(),
            createdByLeoId: Guid.NewGuid(),
            leaderUserId: Guid.NewGuid(),
            leaderTeamId: Guid.NewGuid(),
            title: "Dọn rác kênh Nhiêu Lộc",
            description: "Mang găng tay, nước uống.",
            startsAt: DateTime.UtcNow.AddDays(5),
            endsAt: DateTime.UtcNow.AddDays(5).AddHours(3),
            joinClosesAt: DateTime.UtcNow.AddDays(4),
            maxParticipants: 40,
            meetingNote: "Cổng công viên",
            meetingLatitude: 10.782m,
            meetingLongitude: 106.695m);

    [Fact]
    public void Create_ShouldStartOpenForJoin_BR_CMU_001()
    {
        var ev = CreateTestEvent();

        Assert.Equal(CommunityCleanupStatus.OpenForJoin, ev.Status);
        Assert.True(ev.IsActive);
    }

    [Fact]
    public void CloseJoin_FromOpenForJoin_ShouldTransitionToJoinClosed()
    {
        var ev = CreateTestEvent();

        ev.CloseJoin();

        Assert.Equal(CommunityCleanupStatus.JoinClosed, ev.Status);
    }

    [Fact]
    public void CloseJoin_WhenNotOpenForJoin_ShouldThrow()
    {
        var ev = CreateTestEvent();
        ev.CloseJoin();

        Assert.Throws<InvalidOperationException>(() => ev.CloseJoin());
    }

    [Fact]
    public void Start_FromOpenForJoinOrJoinClosed_ShouldTransitionToInProgress()
    {
        var ev = CreateTestEvent();
        ev.Start();

        Assert.Equal(CommunityCleanupStatus.InProgress, ev.Status);
    }

    [Fact]
    public void SubmitVerification_FromInProgress_ShouldSetPendingVerificationAndSubmittedAt()
    {
        var ev = CreateTestEvent();
        ev.Start();

        ev.SubmitVerification();

        Assert.Equal(CommunityCleanupStatus.PendingVerification, ev.Status);
        Assert.NotNull(ev.SubmittedAt);
    }

    [Fact]
    public void Approve_FromPendingVerification_ShouldCompleteAndRecordLeo_BR_CMU_010()
    {
        var ev = CreateTestEvent();
        ev.Start();
        ev.SubmitVerification();
        var leoId = Guid.NewGuid();

        ev.Approve(leoId);

        Assert.Equal(CommunityCleanupStatus.Completed, ev.Status);
        Assert.Equal(leoId, ev.VerifiedByLeoId);
        Assert.NotNull(ev.VerifiedAt);
        Assert.False(ev.IsActive);
    }

    [Fact]
    public void Reject_FromPendingVerification_ShouldReturnToInProgress_BR_CMU_011()
    {
        var ev = CreateTestEvent();
        ev.Start();
        ev.SubmitVerification();

        ev.Reject("Ảnh after chưa rõ khu vực đã dọn, chụp lại giúp.");

        Assert.Equal(CommunityCleanupStatus.InProgress, ev.Status);
        Assert.Null(ev.SubmittedAt);
        Assert.NotNull(ev.RejectionReason);
    }

    [Fact]
    public void Cancel_FromAnyStatusExceptCompleted_ShouldTransitionToCancelled_BR_CMU_012()
    {
        var ev = CreateTestEvent();

        ev.Cancel("Thời tiết xấu, dời lịch tuần sau.");

        Assert.Equal(CommunityCleanupStatus.Cancelled, ev.Status);
        Assert.False(ev.IsActive);
    }

    [Fact]
    public void Cancel_WhenCompleted_ShouldThrow()
    {
        var ev = CreateTestEvent();
        ev.Start();
        ev.SubmitVerification();
        ev.Approve(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => ev.Cancel("Bất kỳ lý do gì đủ dài."));
    }

    [Fact]
    public void UpdateProgress_OutOfRange_ShouldThrow()
    {
        var ev = CreateTestEvent();
        ev.Start();

        Assert.Throws<ArgumentOutOfRangeException>(() => ev.UpdateProgress(150, null));
    }
}
