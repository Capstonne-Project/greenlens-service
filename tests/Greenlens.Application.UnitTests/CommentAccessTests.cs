using FluentAssertions;
using Greenlens.Application.Common;

namespace Greenlens.Application.UnitTests;

public sealed class CommentAccessTests
{
    [Fact]
    public void CanCommentOnReport_PublicReport_AllowsAnyone_BR_CMT_001()
    {
        var citizenId = Guid.NewGuid();

        CommentAccess.CanCommentOnReport(false, "Citizen", citizenId, Guid.NewGuid())
            .Should().BeTrue();
    }

    [Fact]
    public void CanCommentOnReport_HiddenReport_BlocksOtherCitizens_BR_CMT_001()
    {
        var reporterId = Guid.NewGuid();
        var otherCitizen = Guid.NewGuid();

        CommentAccess.CanCommentOnReport(true, "Citizen", otherCitizen, reporterId)
            .Should().BeFalse();
    }

    [Fact]
    public void CanCommentOnReport_HiddenReport_AllowsReporter_BR_CMT_001()
    {
        var reporterId = Guid.NewGuid();

        CommentAccess.CanCommentOnReport(true, "Citizen", reporterId, reporterId)
            .Should().BeTrue();
    }

    [Theory]
    [InlineData("LEO")]
    [InlineData("DEO")]
    [InlineData("Admin")]
    public void CanCommentOnReport_HiddenReport_AllowsPrivilegedRoles_BR_CMT_001(string role)
    {
        CommentAccess.CanCommentOnReport(true, role, Guid.NewGuid(), Guid.NewGuid())
            .Should().BeTrue();
    }

    [Theory]
    [InlineData("CompanyStaff")]
    [InlineData("Cleaner")]
    public void CanCommentOnReport_HiddenReport_AllowsCleanupTeam_BR_CMT_001(string role)
    {
        CommentAccess.CanCommentOnReport(true, role, Guid.NewGuid(), Guid.NewGuid())
            .Should().BeTrue();
    }

    [Fact]
    public void ResolveAuthorDisplayName_CleanupTeam_MasksIdentity()
    {
        CommentAccess.ResolveAuthorDisplayName("CompanyStaff", "Nguyễn Văn A")
            .Should().Be(CommentAccess.CleanupTeamDisplayName);

        CommentAccess.ResolveAuthorDisplayName("Cleaner", "Trần Thị B")
            .Should().Be(CommentAccess.CleanupTeamDisplayName);
    }

    [Fact]
    public void ResolveAuthorDisplayName_Citizen_KeepsFullName()
    {
        CommentAccess.ResolveAuthorDisplayName("Citizen", "Lê Văn C")
            .Should().Be("Lê Văn C");
    }
}
