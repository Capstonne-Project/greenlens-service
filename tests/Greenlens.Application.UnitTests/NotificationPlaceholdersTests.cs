using Greenlens.Application.Features.Notifications;
using Greenlens.Domain.Enums;
using FluentAssertions;

namespace Greenlens.Application.UnitTests;

public sealed class NotificationPlaceholdersTests
{
    [Fact]
    public void ForNearbyReport_MapsReportCodeAndCategory_BR_NTF_002()
    {
        var placeholders = NotificationPlaceholders.ForNearbyReport("RPT-100", "Rác thải sinh hoạt");

        placeholders["report_code"].Should().Be("RPT-100");
        placeholders["category_name"].Should().Be("Rác thải sinh hoạt");
    }

    [Fact]
    public void ForPenaltyIssued_FormatsAmountAndFields_BR_NTF_002()
    {
        var placeholders = NotificationPlaceholders.ForPenaltyIssued(
            "RPT-200",
            1_500_000m,
            "QĐ-2026-99");

        placeholders["report_code"].Should().Be("RPT-200");
        placeholders["decision_number"].Should().Be("QĐ-2026-99");
        placeholders["penalty_amount"].Should().Be("1,500,000");
    }

    [Fact]
    public void ForReopenDecided_Approved_SetsDecisionAndReason_BR_REP_015()
    {
        var placeholders = NotificationPlaceholders.ForReopenDecided("RPT-300", approved: true);

        placeholders["report_code"].Should().Be("RPT-300");
        placeholders["decision"].Should().Be("chấp nhận");
        placeholders["reason"].Should().Contain("phân công");
    }

    [Fact]
    public void ForReopenDecided_Rejected_IncludesReason_BR_REP_015()
    {
        var placeholders = NotificationPlaceholders.ForReopenDecided("RPT-301", approved: false, "Chưa đủ bằng chứng");

        placeholders["decision"].Should().Be("từ chối");
        placeholders["reason"].Should().Contain("Chưa đủ bằng chứng");
    }

    [Fact]
    public void ForDuplicateReviewFromFlags_BuildsSummary_BR_REP_033()
    {
        var placeholders = NotificationPlaceholders.ForDuplicateReviewFromFlags(
            "RPT-400",
            FlagType.Duplicate,
            3);

        placeholders["report_code"].Should().Be("RPT-400");
        placeholders["detection_summary"].Should().Contain("3 cờ");
        placeholders["detection_summary"].Should().Contain("Duplicate");
    }

    [Fact]
    public void ForDuplicateReviewFromAi_BuildsSummary_BR_REP_032()
    {
        var placeholders = NotificationPlaceholders.ForDuplicateReviewFromAi(
            "RPT-401",
            "RPT-100",
            0.92m);

        placeholders["detection_summary"].Should().Contain("RPT-100");
        placeholders["detection_summary"].Should().Contain("92");
    }

    [Fact]
    public void ForCleanupTaskAssigned_MapsTeamAndReport_BR_CLN_001()
    {
        var placeholders = NotificationPlaceholders.ForCleanupTaskAssigned("RPT-500", "Đội Xanh");

        placeholders["report_code"].Should().Be("RPT-500");
        placeholders["team_name"].Should().Be("Đội Xanh");
    }

    [Fact]
    public void ForReportStatus_MapsInProgress_BR_NTF_002()
    {
        var placeholders = NotificationPlaceholders.ForReportStatus("RPT-600", "InProgress");

        placeholders["report_code"].Should().Be("RPT-600");
        placeholders["status"].Should().Be("InProgress");
    }

    [Fact]
    public void ForCompanyReportDispatched_MapsReportAndCompany_BR_CMP_005()
    {
        var placeholders = NotificationPlaceholders.ForCompanyReportDispatched("RPT-610", "Green Clean Co.");

        placeholders["report_code"].Should().Be("RPT-610");
        placeholders["company_name"].Should().Be("Green Clean Co.");
    }
}
