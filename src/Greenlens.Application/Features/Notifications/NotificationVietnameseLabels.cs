using Greenlens.Domain.Enums;

namespace Greenlens.Application.Features.Notifications;

/// <summary>Human-readable Vietnamese labels for notification placeholders.</summary>
internal static class NotificationVietnameseLabels
{
    internal static string FormatReportStatus(ReportStatus status) => status switch
    {
        ReportStatus.Submitted => "Đã gửi",
        ReportStatus.Verified => "Đã xác minh",
        ReportStatus.InProgress => "Đang xử lý",
        ReportStatus.Reopened => "Đã mở lại",
        ReportStatus.Resolved => "Đã xử lý xong",
        ReportStatus.Closed => "Đã đóng",
        ReportStatus.Rejected => "Đã từ chối",
        ReportStatus.Duplicate => "Trùng lặp",
        _ => status.ToString()
    };

    internal static string FormatReportStatus(string status) => status switch
    {
        "Submitted" => FormatReportStatus(ReportStatus.Submitted),
        "Verified" => FormatReportStatus(ReportStatus.Verified),
        "InProgress" => FormatReportStatus(ReportStatus.InProgress),
        "Reopened" => FormatReportStatus(ReportStatus.Reopened),
        "Resolved" => FormatReportStatus(ReportStatus.Resolved),
        "Closed" => FormatReportStatus(ReportStatus.Closed),
        "Rejected" => FormatReportStatus(ReportStatus.Rejected),
        "Duplicate" => FormatReportStatus(ReportStatus.Duplicate),
        _ => status
    };

    internal static string FormatSeverity(Severity severity) => severity switch
    {
        Severity.Critical => "Nghiêm trọng",
        Severity.High => "Cao",
        Severity.Medium => "Trung bình",
        Severity.Low => "Thấp",
        _ => severity.ToString()
    };

    internal static string FormatSeverity(string severity) => severity switch
    {
        "Critical" => FormatSeverity(Severity.Critical),
        "High" => FormatSeverity(Severity.High),
        "Medium" => FormatSeverity(Severity.Medium),
        "Low" => FormatSeverity(Severity.Low),
        _ => severity
    };

    internal static string FormatFlagType(FlagType flagType) => flagType switch
    {
        FlagType.Duplicate => "trùng lặp",
        FlagType.Invalid => "không hợp lệ",
        FlagType.Spam => "spam",
        FlagType.Inappropriate => "không phù hợp",
        _ => flagType.ToString()
    };

    internal static string LeoOfficer(string? wardName) =>
        string.IsNullOrWhiteSpace(wardName) ? "Cán bộ phường" : $"Cán bộ phường {wardName}";

    internal static string DeoOfficer(string? provinceName) =>
        string.IsNullOrWhiteSpace(provinceName) ? "Cán bộ sở" : $"Cán bộ sở {provinceName}";

    internal static string DisplayWardName(string? wardName) =>
        string.IsNullOrWhiteSpace(wardName) ? "khu vực liên quan" : wardName;

    internal static string DisplayProvinceName(string? provinceName) =>
        string.IsNullOrWhiteSpace(provinceName) ? "tỉnh/thành phố" : provinceName;
}
