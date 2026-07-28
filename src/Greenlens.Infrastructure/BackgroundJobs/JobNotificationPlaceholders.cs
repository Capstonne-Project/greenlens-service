using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Features.Notifications;
using Greenlens.Domain.Enums;

namespace Greenlens.Infrastructure.BackgroundJobs;

/// <summary>Shared placeholder keys for notification templates in background jobs (BR-NTF-002).</summary>
internal static class JobNotificationPlaceholders
{
    public static Dictionary<string, string> ForReport(string reportCode)
        => new(StringComparer.Ordinal)
        {
            ["report_code"] = reportCode,
            // Legacy alias — some admin-edited templates may still use {report_id} for human-readable code.
            ["report_id"] = reportCode
        };

    public static Dictionary<string, string> ForReportWithSeverity(string reportCode, Severity severity)
    {
        var placeholders = ForReport(reportCode);
        placeholders["severity"] = NotificationVietnameseLabels.FormatSeverity(severity);
        return placeholders;
    }

    public static Dictionary<string, string> ForReportWithSeverity(string reportCode, string severity)
    {
        var placeholders = ForReport(reportCode);
        placeholders["severity"] = NotificationVietnameseLabels.FormatSeverity(severity);
        return placeholders;
    }

    public static Dictionary<string, string> ForCleanupStale(string reportCode, string teamName)
    {
        var placeholders = ForReport(reportCode);
        placeholders["team_name"] = teamName;
        return placeholders;
    }

    public static Dictionary<string, string> ForContractExpired(
        string companyName,
        string contractNumber)
        => new(StringComparer.Ordinal)
        {
            ["company_name"] = companyName,
            ["contract_number"] = contractNumber
        };

    public static Dictionary<string, string> ForContractExpiryWarning(
        string companyName,
        int daysLeft,
        string endDate)
        => new(StringComparer.Ordinal)
        {
            ["company_name"] = companyName,
            ["days_left"] = daysLeft.ToString(),
            ["end_date"] = endDate
        };

    public static Dictionary<string, string> ForDuplicateReviewFromAi(
        string reportCode,
        string primaryReportCode,
        decimal confidence)
        => new(StringComparer.Ordinal)
        {
            ["report_code"] = reportCode,
            ["detection_summary"] =
                $"hệ thống xác nhận trùng với {primaryReportCode}, độ tin cậy {confidence:P0}"
        };

    public static async Task<Dictionary<string, string>> EnrichFromWardCodeAsync(
        IApplicationDbContext db,
        Dictionary<string, string> placeholders,
        string? wardCode,
        CancellationToken ct = default)
        => await NotificationLocalityQueries
            .EnrichFromWardCodeAsync(db, placeholders, wardCode, ct)
            .ConfigureAwait(false);

    public static async Task<Dictionary<string, string>> EnrichFromReportIdAsync(
        IApplicationDbContext db,
        Dictionary<string, string> placeholders,
        Guid reportId,
        CancellationToken ct = default)
        => await NotificationLocalityQueries
            .EnrichFromReportIdAsync(db, placeholders, reportId, ct)
            .ConfigureAwait(false);
}
