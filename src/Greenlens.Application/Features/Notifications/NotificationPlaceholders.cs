using System.Globalization;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.Features.Notifications;

internal static class NotificationPlaceholders
{
    internal static Dictionary<string, string> ForNearbyReport(string reportCode, string categoryName) =>
        new(StringComparer.Ordinal)
        {
            ["report_code"] = reportCode,
            ["category_name"] = categoryName
        };

    internal static Dictionary<string, string> ForPenaltyIssued(
        string reportCode,
        decimal penaltyAmount,
        string decisionNumber) =>
        new(StringComparer.Ordinal)
        {
            ["report_code"] = reportCode,
            ["penalty_amount"] = penaltyAmount.ToString("N0", CultureInfo.InvariantCulture),
            ["decision_number"] = decisionNumber
        };

    internal static Dictionary<string, string> ForReopenReview(string reportCode) =>
        new(StringComparer.Ordinal)
        {
            ["report_code"] = reportCode
        };

    internal static Dictionary<string, string> ForReopenDecided(
        string reportCode,
        bool approved,
        string? reason = null) =>
        new(StringComparer.Ordinal)
        {
            ["report_code"] = reportCode,
            ["decision"] = approved ? "chấp nhận" : "từ chối",
            ["reason"] = approved
                ? "Báo cáo sẽ được phân công dọn dẹp lại."
                : $"Lý do: {reason?.Trim() ?? "Không có lý do."}"
        };

    internal static Dictionary<string, string> ForDuplicateReview(string reportCode, string detectionSummary) =>
        new(StringComparer.Ordinal)
        {
            ["report_code"] = reportCode,
            ["detection_summary"] = detectionSummary
        };

    internal static Dictionary<string, string> ForDuplicateReviewFromFlags(
        string reportCode,
        FlagType flagType,
        int flagCount) =>
        ForDuplicateReview(
            reportCode,
            $"{flagCount} cờ {NotificationVietnameseLabels.FormatFlagType(flagType)} từ công dân");

    internal static Dictionary<string, string> ForDuplicateReviewFromAi(
        string reportCode,
        string primaryReportCode,
        decimal confidence) =>
        ForDuplicateReview(
            reportCode,
            $"hệ thống xác nhận trùng với {primaryReportCode}, độ tin cậy {confidence:P0}");

    internal static Dictionary<string, string> ForCleanupTaskAssigned(string reportCode, string teamName) =>
        new(StringComparer.Ordinal)
        {
            ["report_code"] = reportCode,
            ["team_name"] = teamName
        };

    internal static Dictionary<string, string> ForReportStatus(string reportCode, ReportStatus status) =>
        ForReportStatus(reportCode, NotificationVietnameseLabels.FormatReportStatus(status));

    internal static Dictionary<string, string> ForReportStatus(string reportCode, string status) =>
        new(StringComparer.Ordinal)
        {
            ["report_code"] = reportCode,
            ["status"] = NotificationVietnameseLabels.FormatReportStatus(status)
        };

    internal static Dictionary<string, string> ForCompanyReportDispatched(
        string reportCode,
        string companyName) =>
        new(StringComparer.Ordinal)
        {
            ["report_code"] = reportCode,
            ["company_name"] = companyName
        };
}
