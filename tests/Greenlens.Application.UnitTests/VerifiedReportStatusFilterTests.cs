using Greenlens.Application.Features.Gamification;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.UnitTests;

public sealed class VerifiedReportStatusFilterTests
{
    [Theory]
    [InlineData(ReportStatus.Verified, true)]
    [InlineData(ReportStatus.InProgress, true)]
    [InlineData(ReportStatus.Resolved, true)]
    [InlineData(ReportStatus.Reopened, true)]
    [InlineData(ReportStatus.Closed, true)]
    [InlineData(ReportStatus.Submitted, false)]
    [InlineData(ReportStatus.Rejected, false)]
    [InlineData(ReportStatus.Duplicate, false)]
    public void IsVerifiedForBadge_MatchesMilestoneDefinition_BR_GAM_004(
        ReportStatus status, bool expected)
    {
        Assert.Equal(expected, VerifiedReportStatusFilter.IsVerifiedForBadge(status));
    }
}
