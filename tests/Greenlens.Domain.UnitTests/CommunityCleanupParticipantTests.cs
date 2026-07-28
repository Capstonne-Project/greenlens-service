using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;

namespace Greenlens.Domain.UnitTests;

public sealed class CommunityCleanupParticipantTests
{
    [Fact]
    public void Create_ShouldStartJoined_BR_CMU_005()
    {
        var p = CommunityCleanupParticipant.Create(Guid.NewGuid(), Guid.NewGuid(), CommunityCleanupParticipantRole.Member);

        Assert.Equal(CommunityCleanupParticipantStatus.Joined, p.Status);
        Assert.Equal(CommunityCleanupParticipantRole.Member, p.Role);
    }

    [Fact]
    public void CheckIn_FromJoined_ShouldTransitionToCheckedIn_BR_CMU_007()
    {
        var p = CommunityCleanupParticipant.Create(Guid.NewGuid(), Guid.NewGuid(), CommunityCleanupParticipantRole.Member);

        p.CheckIn(10.782m, 106.695m);

        Assert.Equal(CommunityCleanupParticipantStatus.CheckedIn, p.Status);
        Assert.NotNull(p.CheckedInAt);
    }

    [Fact]
    public void Withdraw_FromJoined_ShouldTransitionToWithdrawn_BR_CMU_006()
    {
        var p = CommunityCleanupParticipant.Create(Guid.NewGuid(), Guid.NewGuid(), CommunityCleanupParticipantRole.Member);

        p.Withdraw();

        Assert.Equal(CommunityCleanupParticipantStatus.Withdrawn, p.Status);
    }

    [Fact]
    public void Withdraw_AfterCheckIn_ShouldThrow_BR_CMU_006()
    {
        var p = CommunityCleanupParticipant.Create(Guid.NewGuid(), Guid.NewGuid(), CommunityCleanupParticipantRole.Member);
        p.CheckIn(10.782m, 106.695m);

        Assert.Throws<InvalidOperationException>(() => p.Withdraw());
    }

    [Fact]
    public void ForceWithdraw_FromCheckedIn_ShouldSucceed()
    {
        var p = CommunityCleanupParticipant.Create(Guid.NewGuid(), Guid.NewGuid(), CommunityCleanupParticipantRole.Member);
        p.CheckIn(10.782m, 106.695m);

        p.ForceWithdraw();

        Assert.Equal(CommunityCleanupParticipantStatus.Withdrawn, p.Status);
    }
}
