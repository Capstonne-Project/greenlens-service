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

    private static void SetAssignedAt(ReportAssignment assignment, DateTime assignedAt)
    {
        typeof(ReportAssignment)
            .GetProperty(nameof(ReportAssignment.AssignedAt))!
            .SetValue(assignment, assignedAt);
    }
}
