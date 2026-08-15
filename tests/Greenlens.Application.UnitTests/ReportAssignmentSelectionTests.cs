using FluentAssertions;
using Greenlens.Application.Features.Reports.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.UnitTests;

public sealed class ReportAssignmentSelectionTests
{
    [Fact]
    public void SelectLatestForTeam_PicksNewestRow_BR_REP_015()
    {
        var teamId = Guid.NewGuid();
        var older = ReportAssignment.Create(Guid.NewGuid(), teamId, Guid.NewGuid());
        SetAssignedAt(older, DateTime.UtcNow.AddDays(-2));

        var newer = ReportAssignment.Create(Guid.NewGuid(), teamId, Guid.NewGuid());
        SetAssignedAt(newer, DateTime.UtcNow.AddDays(-1));

        var selected = ReportAssignmentSelection.SelectLatestForTeam([older, newer], teamId);

        selected.Should().BeSameAs(newer);
    }

    [Fact]
    public void HasOpenAssignmentForTeam_IgnoresCompletedHistory_BR_REP_015()
    {
        var teamId = Guid.NewGuid();
        var completed = ReportAssignment.Create(Guid.NewGuid(), teamId, Guid.NewGuid());
        completed.Accept();
        completed.Complete();

        ReportAssignmentSelection.HasOpenAssignmentForTeam([completed], teamId).Should().BeFalse();
    }

    [Fact]
    public void HasOpenAssignmentForTeam_DetectsAssignedRow_BR_REP_015()
    {
        var teamId = Guid.NewGuid();
        var completed = ReportAssignment.Create(Guid.NewGuid(), teamId, Guid.NewGuid());
        completed.Accept();
        completed.Complete();

        var assigned = ReportAssignment.Create(Guid.NewGuid(), teamId, Guid.NewGuid());
        SetAssignedAt(completed, DateTime.UtcNow.AddDays(-2));
        SetAssignedAt(assigned, DateTime.UtcNow);

        ReportAssignmentSelection.HasOpenAssignmentForTeam([completed, assigned], teamId).Should().BeTrue();
    }

    [Fact]
    public void AllNonDeclinedCompleted_CountsHistoricalCompletedRows_BR_REP_015()
    {
        var teamId = Guid.NewGuid();
        var oldCompleted = ReportAssignment.Create(Guid.NewGuid(), teamId, Guid.NewGuid());
        oldCompleted.Accept();
        oldCompleted.Complete();

        var newAssigned = ReportAssignment.Create(Guid.NewGuid(), teamId, Guid.NewGuid());

        ReportAssignmentSelection.AllNonDeclinedCompleted([oldCompleted, newAssigned]).Should().BeFalse();

        newAssigned.Accept();
        newAssigned.Complete();

        ReportAssignmentSelection.AllNonDeclinedCompleted([oldCompleted, newAssigned]).Should().BeTrue();
    }

    [Fact]
    public void AllCurrentCycleNonDeclinedCompleted_IgnoresStaleTeamAfterReopen_BR_REP_015()
    {
        var teamA = Guid.NewGuid();
        var teamB = Guid.NewGuid();

        var teamAOld = ReportAssignment.Create(Guid.NewGuid(), teamA, Guid.NewGuid());
        teamAOld.Accept();
        teamAOld.Complete();
        SetAssignedAt(teamAOld, DateTime.UtcNow.AddDays(-10));

        var teamBOld = ReportAssignment.Create(Guid.NewGuid(), teamB, Guid.NewGuid());
        teamBOld.Accept();
        teamBOld.Complete();
        SetAssignedAt(teamBOld, DateTime.UtcNow.AddDays(-10));

        var teamANew = ReportAssignment.Create(Guid.NewGuid(), teamA, Guid.NewGuid());
        SetAssignedAt(teamANew, DateTime.UtcNow);

        ReportAssignmentSelection.AllCurrentCycleNonDeclinedCompleted(
                [teamAOld, teamBOld, teamANew], ReportStatus.InProgress)
            .Should().BeFalse();

        teamANew.Accept();
        teamANew.Complete();

        ReportAssignmentSelection.AllCurrentCycleNonDeclinedCompleted(
                [teamAOld, teamBOld, teamANew], ReportStatus.InProgress)
            .Should().BeTrue();
    }

    [Fact]
    public void AllCurrentCycleEscalatedOrCompleted_IgnoresPriorCycleCompleted_BR_REP_015()
    {
        var teamA = Guid.NewGuid();
        var teamB = Guid.NewGuid();

        var teamBOld = ReportAssignment.Create(Guid.NewGuid(), teamB, Guid.NewGuid());
        teamBOld.Accept();
        teamBOld.Complete();
        SetAssignedAt(teamBOld, DateTime.UtcNow.AddDays(-10));

        var teamANew = ReportAssignment.Create(Guid.NewGuid(), teamA, Guid.NewGuid());
        teamANew.Accept();
        SetAssignedAt(teamANew, DateTime.UtcNow);
        teamANew.Escalate("Need heavy equipment support beyond team capability.");

        ReportAssignmentSelection.AllCurrentCycleEscalatedOrCompleted(
                [teamBOld, teamANew], ReportStatus.InProgress)
            .Should().BeTrue();
    }

    [Fact]
    public void ResolveCurrentAssignment_AfterReopenBeforeReassign_ReturnsNull_NotOldCompleted_BR_REP_015()
    {
        var teamId = Guid.NewGuid();
        var oldCompleted = ReportAssignment.Create(Guid.NewGuid(), teamId, Guid.NewGuid());
        oldCompleted.Accept();
        oldCompleted.Complete();
        SetAssignedAt(oldCompleted, DateTime.UtcNow.AddDays(-2));

        ReportAssignmentSelection.ResolveCurrentAssignment(
                [oldCompleted], ReportStatus.Reopened)
            .Should().BeNull();
    }

    [Fact]
    public void ResolveCurrentAssignment_AfterReopenWithNewTeam_ReturnsNewAssignment_BR_REP_015()
    {
        var oldTeamId = Guid.NewGuid();
        var newTeamId = Guid.NewGuid();

        var oldCompleted = ReportAssignment.Create(Guid.NewGuid(), oldTeamId, Guid.NewGuid());
        oldCompleted.Accept();
        oldCompleted.Complete();
        SetAssignedAt(oldCompleted, DateTime.UtcNow.AddDays(-5));

        var newAssigned = ReportAssignment.Create(Guid.NewGuid(), newTeamId, Guid.NewGuid());
        SetAssignedAt(newAssigned, DateTime.UtcNow);

        var selected = ReportAssignmentSelection.ResolveCurrentAssignment(
            [oldCompleted, newAssigned], ReportStatus.InProgress);

        selected.Should().BeSameAs(newAssigned);
    }

    [Fact]
    public void ResolveCurrentAssignment_AfterReopenCompanyDispatchOnly_ReturnsNull_BR_REP_015()
    {
        var oldCompleted = ReportAssignment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        oldCompleted.Accept();
        oldCompleted.Complete();

        ReportAssignmentSelection.ResolveCurrentAssignment(
                [oldCompleted], ReportStatus.InProgress)
            .Should().BeNull();
    }

    [Fact]
    public void ResolveCurrentAssignment_WhenResolved_ReturnsLastCompletedForDisplay()
    {
        var teamId = Guid.NewGuid();
        var completed = ReportAssignment.Create(Guid.NewGuid(), teamId, Guid.NewGuid());
        completed.Accept();
        completed.Complete();

        ReportAssignmentSelection.ResolveCurrentAssignment(
                [completed], ReportStatus.Resolved)
            .Should().BeSameAs(completed);
    }

    [Fact]
    public void MatchesCurrentAssignmentStatusFilter_IgnoresHistoricalCompletedAfterReopen_BR_REP_015()
    {
        var teamId = Guid.NewGuid();
        var oldCompleted = ReportAssignment.Create(Guid.NewGuid(), teamId, Guid.NewGuid());
        oldCompleted.Accept();
        oldCompleted.Complete();
        SetAssignedAt(oldCompleted, DateTime.UtcNow.AddDays(-5));

        var newAssigned = ReportAssignment.Create(Guid.NewGuid(), teamId, Guid.NewGuid());
        SetAssignedAt(newAssigned, DateTime.UtcNow);

        ReportAssignmentSelection.MatchesCurrentAssignmentStatusFilter(
                [oldCompleted, newAssigned], ReportStatus.InProgress, AssignmentStatus.Completed)
            .Should().BeFalse();

        ReportAssignmentSelection.MatchesCurrentAssignmentStatusFilter(
                [oldCompleted, newAssigned], ReportStatus.InProgress, AssignmentStatus.Assigned)
            .Should().BeTrue();
    }

    [Fact]
    public void ResolveProgressAssignment_DeclinedOnly_ReturnsDeclinedWithReason_BR_CLN_007()
    {
        var teamId = Guid.NewGuid();
        var declined = ReportAssignment.Create(Guid.NewGuid(), teamId, Guid.NewGuid());
        declined.Decline("Không đủ nhân sự trong tuần này");

        var selected = ReportAssignmentSelection.ResolveProgressAssignment(
            [declined], ReportStatus.InProgress);

        selected.Should().BeSameAs(declined);
        selected!.Status.Should().Be(AssignmentStatus.Declined);
        selected.DeclineReason.Should().Be("Không đủ nhân sự trong tuần này");
    }

    [Fact]
    public void ResolveProgressAssignment_AfterDeclineReassign_PicksNewTeam_BR_OFF_012()
    {
        var teamA = Guid.NewGuid();
        var teamB = Guid.NewGuid();

        var declined = ReportAssignment.Create(Guid.NewGuid(), teamA, Guid.NewGuid());
        declined.Decline("Team A busy");
        SetAssignedAt(declined, DateTime.UtcNow.AddHours(-2));

        var reassigned = ReportAssignment.Create(Guid.NewGuid(), teamB, Guid.NewGuid());
        SetAssignedAt(reassigned, DateTime.UtcNow);

        var selected = ReportAssignmentSelection.ResolveProgressAssignment(
            [declined, reassigned], ReportStatus.InProgress);

        selected.Should().BeSameAs(reassigned);
    }

    [Fact]
    public void ResolveProgressAssignment_DeclinedThenCompleted_ShowsCompletedNotDeclined()
    {
        var teamA = Guid.NewGuid();
        var teamB = Guid.NewGuid();

        var declined = ReportAssignment.Create(Guid.NewGuid(), teamA, Guid.NewGuid());
        declined.Decline("Team A unavailable");
        SetAssignedAt(declined, DateTime.UtcNow.AddDays(-1));

        var completed = ReportAssignment.Create(Guid.NewGuid(), teamB, Guid.NewGuid());
        completed.Accept();
        completed.Complete();
        SetAssignedAt(completed, DateTime.UtcNow.AddHours(-1));

        var selected = ReportAssignmentSelection.ResolveProgressAssignment(
            [declined, completed], ReportStatus.InProgress);

        selected.Should().BeSameAs(completed);
    }

    [Fact]
    public void ResolveProgressAssignment_FirstCycleAllCompleted_ReturnsLatestCompleted()
    {
        var teamA = Guid.NewGuid();
        var teamB = Guid.NewGuid();

        var completedA = ReportAssignment.Create(Guid.NewGuid(), teamA, Guid.NewGuid());
        completedA.Accept();
        completedA.Complete();
        SetAssignedAt(completedA, DateTime.UtcNow.AddDays(-2));
        SetCompletedAt(completedA, DateTime.UtcNow.AddDays(-1));

        var completedB = ReportAssignment.Create(Guid.NewGuid(), teamB, Guid.NewGuid());
        completedB.Accept();
        completedB.Complete();
        SetAssignedAt(completedB, DateTime.UtcNow.AddDays(-1));
        SetCompletedAt(completedB, DateTime.UtcNow);

        var selected = ReportAssignmentSelection.ResolveProgressAssignment(
            [completedA, completedB], ReportStatus.InProgress);

        selected.Should().BeSameAs(completedB);
    }

    [Fact]
    public void ResolveProgressAssignment_ReopenedAwaitingAssign_ReturnsNull_BR_REP_015()
    {
        var teamId = Guid.NewGuid();
        var reopenTime = DateTime.UtcNow.AddDays(-1);

        var oldCompleted = ReportAssignment.Create(Guid.NewGuid(), teamId, Guid.NewGuid());
        oldCompleted.Accept();
        oldCompleted.Complete();
        SetAssignedAt(oldCompleted, DateTime.UtcNow.AddDays(-10));

        var history = new[]
        {
            CreateStatusHistory(ReportStatus.Resolved, ReportStatus.Reopened, reopenTime)
        };

        ReportAssignmentSelection.ResolveProgressAssignment(
                [oldCompleted],
                ReportStatus.Reopened,
                reopenedCount: 1,
                history)
            .Should().BeNull();
    }

    [Fact]
    public void SelectCurrentCycleAssignments_ResolvedAfterReopen_ExcludesPriorCycle_BR_REP_015()
    {
        var reopenTime = DateTime.UtcNow.AddDays(-2);
        var teamA = Guid.NewGuid();
        var teamB = Guid.NewGuid();
        var teamC = Guid.NewGuid();

        var teamAOld = ReportAssignment.Create(Guid.NewGuid(), teamA, Guid.NewGuid());
        teamAOld.Accept();
        teamAOld.Complete();
        SetAssignedAt(teamAOld, DateTime.UtcNow.AddDays(-10));

        var teamBOld = ReportAssignment.Create(Guid.NewGuid(), teamB, Guid.NewGuid());
        teamBOld.Accept();
        teamBOld.Complete();
        SetAssignedAt(teamBOld, DateTime.UtcNow.AddDays(-10));

        var teamCNew = ReportAssignment.Create(Guid.NewGuid(), teamC, Guid.NewGuid());
        teamCNew.Accept();
        teamCNew.Complete();
        SetAssignedAt(teamCNew, DateTime.UtcNow.AddDays(-1));

        var history = new[]
        {
            CreateStatusHistory(ReportStatus.Resolved, ReportStatus.Reopened, reopenTime)
        };

        var cycle = ReportAssignmentSelection.SelectCurrentCycleAssignments(
            [teamAOld, teamBOld, teamCNew],
            ReportStatus.InProgress,
            reopenedCount: 1,
            history);

        cycle.Should().ContainSingle().Which.Should().BeSameAs(teamCNew);
    }

    [Fact]
    public void ResolveProgressAssignment_ReopenNewTeam_ReturnsNewTeam_BR_REP_015()
    {
        var reopenTime = DateTime.UtcNow.AddDays(-2);
        var oldTeamId = Guid.NewGuid();
        var newTeamId = Guid.NewGuid();

        var oldCompleted = ReportAssignment.Create(Guid.NewGuid(), oldTeamId, Guid.NewGuid());
        oldCompleted.Accept();
        oldCompleted.Complete();
        SetAssignedAt(oldCompleted, DateTime.UtcNow.AddDays(-10));

        var newAssigned = ReportAssignment.Create(Guid.NewGuid(), newTeamId, Guid.NewGuid());
        SetAssignedAt(newAssigned, DateTime.UtcNow.AddDays(-1));

        var history = new[]
        {
            CreateStatusHistory(ReportStatus.Resolved, ReportStatus.Reopened, reopenTime)
        };

        var selected = ReportAssignmentSelection.ResolveProgressAssignment(
            [oldCompleted, newAssigned],
            ReportStatus.InProgress,
            reopenedCount: 1,
            history);

        selected.Should().BeSameAs(newAssigned);
    }

    [Fact]
    public void IsCompanyDispatchInCurrentCycle_ReopenBeforeRedispatch_ReturnsFalse_BR_REP_015()
    {
        var cycleStart = DateTime.UtcNow.AddDays(-1);
        var staleDispatch = DateTime.UtcNow.AddDays(-5);

        ReportAssignmentSelection.IsCompanyDispatchInCurrentCycle(
                reopenedCount: 1,
                staleDispatch,
                cycleStart)
            .Should().BeFalse();
    }

    [Fact]
    public void IsCompanyDispatchInCurrentCycle_AfterRedispatch_ReturnsTrue_BR_CMP_005()
    {
        var cycleStart = DateTime.UtcNow.AddDays(-2);
        var newDispatch = DateTime.UtcNow.AddDays(-1);

        ReportAssignmentSelection.IsCompanyDispatchInCurrentCycle(
                reopenedCount: 1,
                newDispatch,
                cycleStart)
            .Should().BeTrue();
    }

    [Fact]
    public void AllCurrentCycleNonDeclinedCompleted_ReopenedWithoutHistory_FalseAfterComplete_BR_REP_015()
    {
        var teamId = Guid.NewGuid();
        var assignedAt = DateTime.UtcNow.AddHours(-2);
        var completedAt = DateTime.UtcNow;

        var assignment = ReportAssignment.Create(Guid.NewGuid(), teamId, Guid.NewGuid());
        assignment.Accept();
        assignment.Complete();
        SetAssignedAt(assignment, assignedAt);
        SetCompletedAt(assignment, completedAt);

        ReportAssignmentSelection.AllCurrentCycleNonDeclinedCompleted(
                [assignment],
                ReportStatus.InProgress,
                reopenedCount: 1,
                statusHistory: [])
            .Should().BeFalse(
                "empty StatusHistory makes cycle boundary fall back to CompletedAt and excludes the assignment");
    }

    [Fact]
    public void AllCurrentCycleNonDeclinedCompleted_ReopenedWithHistory_TrueAfterComplete_BR_REP_015()
    {
        var teamId = Guid.NewGuid();
        var reopenAt = DateTime.UtcNow.AddHours(-3);
        var assignedAt = DateTime.UtcNow.AddHours(-2);
        var completedAt = DateTime.UtcNow;

        var assignment = ReportAssignment.Create(Guid.NewGuid(), teamId, Guid.NewGuid());
        assignment.Accept();
        assignment.Complete();
        SetAssignedAt(assignment, assignedAt);
        SetCompletedAt(assignment, completedAt);

        var history = CreateStatusHistory(ReportStatus.Resolved, ReportStatus.Reopened, reopenAt);

        ReportAssignmentSelection.AllCurrentCycleNonDeclinedCompleted(
                [assignment],
                ReportStatus.InProgress,
                reopenedCount: 1,
                statusHistory: [history])
            .Should().BeTrue();
    }

    [Fact]
    public void IsCompanyDispatchInCurrentCycle_FirstCycle_AlwaysTrue()
    {
        ReportAssignmentSelection.IsCompanyDispatchInCurrentCycle(
                0,
                DateTime.UtcNow.AddDays(-30),
                null)
            .Should().BeTrue();
    }

    private static ReportStatusHistory CreateStatusHistory(
        ReportStatus from,
        ReportStatus to,
        DateTime createdAt)
    {
        var history = ReportStatusHistory.Create(
            Guid.NewGuid(),
            from,
            to,
            Guid.NewGuid());

        typeof(ReportStatusHistory)
            .GetProperty(nameof(ReportStatusHistory.CreatedAt))!
            .SetValue(history, createdAt);

        return history;
    }

    private static void SetCompletedAt(ReportAssignment assignment, DateTime completedAt)
    {
        typeof(ReportAssignment)
            .GetProperty(nameof(ReportAssignment.CompletedAt))!
            .SetValue(assignment, completedAt);
    }

    private static void SetAssignedAt(ReportAssignment assignment, DateTime assignedAt)
    {
        typeof(ReportAssignment)
            .GetProperty(nameof(ReportAssignment.AssignedAt))!
            .SetValue(assignment, assignedAt);
    }
}
