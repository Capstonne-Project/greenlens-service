using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Xunit;

namespace Greenlens.Domain.UnitTests;

public sealed class InspectionReportWorkflowTests
{
    [Fact]
    public void AcceptTask_FromDraft_MovesToInProgress_BR_INS_033()
    {
        var inspection = InspectionReport.Create(
            Guid.NewGuid(), Guid.NewGuid(), Severity.Medium, Guid.NewGuid());

        var result = inspection.AcceptTask(Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Equal(InspectionStatus.InProgress, inspection.Status);
        Assert.NotNull(inspection.AcceptedAt);
    }

    [Fact]
    public void SubmitFieldInvestigation_RequiresInProgress_BR_INS_033()
    {
        var inspection = InspectionReport.Create(
            Guid.NewGuid(), Guid.NewGuid(), Severity.Medium, Guid.NewGuid());

        var result = inspection.SubmitFieldInvestigation(Guid.NewGuid());

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void IssuePenalty_RequiresFieldReportSubmitted_BR_INS_033()
    {
        var inspection = InspectionReport.Create(
            Guid.NewGuid(), Guid.NewGuid(), Severity.Medium, Guid.NewGuid());
        inspection.AcceptTask(Guid.NewGuid());

        var result = inspection.IssuePenalty(
            Guid.NewGuid(),
            ViolationLevel.Minor,
            1_000_000m,
            "QD-001",
            DateTime.UtcNow.AddDays(10));

        Assert.True(result.IsFailure);
    }
}
