using Greenlens.Domain.Enums;

namespace Greenlens.Application.Features.Admin.SystemSettings;

/// <summary>Static catalog for admin UI module sidebar (BR-ADM-010).</summary>
public static class SystemSettingModuleCatalog
{
    public static IReadOnlyList<SystemSettingModuleInfo> All { get; } =
    [
        new(SystemSettingModule.Reports, "reports", "Báo cáo", "Trùng lặp, tái phạm, vòng đời báo cáo"),
        new(SystemSettingModule.Sla, "sla", "SLA", "Hạn xác minh, xử lý, quá hạn"),
        new(SystemSettingModule.Geo, "geo", "Địa lý", "GPS Việt Nam, check-in, EXIF"),
        new(SystemSettingModule.Map, "map", "Bản đồ công khai", "Giới hạn viewport, làm tròn tọa độ"),
        new(SystemSettingModule.Officer, "officer", "Cán bộ", "Điểm ưu tiên hàng đợi"),
        new(SystemSettingModule.Cleanup, "cleanup", "Dọn dẹp", "Tiến độ, SLA assignment"),
        new(SystemSettingModule.Notifications, "notifications", "Thông báo", "Bán kính nearby, anti-spam"),
        new(SystemSettingModule.Gamification, "gamification", "Gamification", "Ngưỡng badge (bổ sung)"),
        new(SystemSettingModule.Auth, "auth", "Xác thực", "Khóa tài khoản, OTP"),
        new(SystemSettingModule.Comments, "comments", "Bình luận", "Sửa/xóa, ban"),
        new(SystemSettingModule.Organization, "organization", "Tổ chức", "Mời nhân sự, workload đội"),
        new(SystemSettingModule.CommunityCleanup, "community_cleanup", "Dọn cộng đồng", "Ảnh, nhắc check-in"),
        new(SystemSettingModule.DataRetention, "data_retention", "Lưu trữ", "Retention media, audit"),
        new(SystemSettingModule.RateLimits, "rate_limits", "Giới hạn tần suất", "Submit report quota"),
        new(SystemSettingModule.Inspection, "inspection", "Thanh tra", "SLA, evidence"),
        new(SystemSettingModule.Ai, "ai", "AI", "Timeout, TTL upload"),
        new(SystemSettingModule.Validation, "validation", "Validation", "Độ dài lý do tối thiểu")
    ];

    public static bool TryParseModule(string module, out SystemSettingModule parsed)
    {
        parsed = default;
        if (string.IsNullOrWhiteSpace(module))
            return false;

        foreach (var entry in All)
        {
            if (string.Equals(entry.RouteSlug, module, StringComparison.OrdinalIgnoreCase)
                || string.Equals(entry.Module.ToString(), module, StringComparison.OrdinalIgnoreCase))
            {
                parsed = entry.Module;
                return true;
            }
        }

        return Enum.TryParse(module, ignoreCase: true, out parsed);
    }
}

public sealed record SystemSettingModuleInfo(
    SystemSettingModule Module,
    string RouteSlug,
    string DisplayNameVi,
    string DescriptionVi);
