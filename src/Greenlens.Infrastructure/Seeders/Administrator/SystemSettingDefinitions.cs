using Greenlens.Domain.Enums;

namespace Greenlens.Infrastructure.Seeders.Administrator;

internal sealed record SystemSettingSeedDefinition(
    SystemSettingModule Module,
    string Key,
    SystemSettingValueType ValueType,
    string DefaultValue,
    string Description,
    decimal? MinValue = null,
    decimal? MaxValue = null);

/// <summary>
/// Canonical defaults = current production hardcode (not BR doc overrides).
/// Hotspot (BR-MAP-010) excluded — feature removed.
/// </summary>
internal static class SystemSettingDefinitions
{
    public static IReadOnlyList<SystemSettingSeedDefinition> All { get; } =
    [
        // ── Reports: duplicate (Mục A) — radius 25m = current code ──
        Def(SystemSettingModule.Reports, "duplicate_radius_meters", SystemSettingValueType.Int, "25",
            "Bán kính phát hiện trùng lặp Tier-1 (mét)", 10, 500),
        Def(SystemSettingModule.Reports, "duplicate_time_window_hours", SystemSettingValueType.Int, "0",
            "Cửa sổ thời gian trùng lặp (giờ). 0 = không lọc theo thời gian", 0, 8760),
        Def(SystemSettingModule.Reports, "duplicate_max_candidates", SystemSettingValueType.Int, "20",
            "Số báo cáo candidate tối đa khi submit", 5, 100),
        Def(SystemSettingModule.Reports, "duplicate_merge_points_ratio", SystemSettingValueType.Decimal, "0.5",
            "Tỷ lệ điểm thưởng khi gộp báo cáo trùng", 0, 1),

        // ── Reports: recurrence (Mục B) ──
        Def(SystemSettingModule.Reports, "recurrence_radius_meters", SystemSettingValueType.Int, "25",
            "Bán kính nghi ngờ tái phạm (mét)", 10, 500),
        Def(SystemSettingModule.Reports, "recurrence_lookback_days", SystemSettingValueType.Int, "30",
            "Chỉ xét báo cáo Closed trong N ngày gần đây", 1, 365),
        Def(SystemSettingModule.Reports, "recurrence_min_days_after_close", SystemSettingValueType.Int, "0",
            "Số ngày tối thiểu sau khi Closed mới flag tái phạm (0 = ngay)", 0, 365),
        Def(SystemSettingModule.Reports, "recurrence_max_days_after_close", SystemSettingValueType.Int, "30",
            "Số ngày tối đa sau Closed để flag tái phạm", 1, 365),

        // ── Reports: lifecycle (Mục C) — auto_close 2d = current code ──
        Def(SystemSettingModule.Reports, "max_images_per_report", SystemSettingValueType.Int, "5",
            "Số ảnh tối đa mỗi báo cáo", 1, 10),
        Def(SystemSettingModule.Reports, "max_image_size_bytes", SystemSettingValueType.Int, "10485760",
            "Kích thước ảnh tối đa (bytes)", 1_048_576, 52_428_800),
        Def(SystemSettingModule.Reports, "max_drafts_per_user", SystemSettingValueType.Int, "3",
            "Số bản nháp tối đa mỗi user", 1, 20),
        Def(SystemSettingModule.Reports, "draft_retention_days", SystemSettingValueType.Int, "7",
            "Số ngày giữ bản nháp trước khi xóa", 1, 90),
        Def(SystemSettingModule.Reports, "auto_close_resolved_days", SystemSettingValueType.Int, "2",
            "Tự đóng báo cáo Resolved sau N ngày chờ xác nhận", 1, 30),
        Def(SystemSettingModule.Reports, "reopen_window_days", SystemSettingValueType.Int, "7",
            "Cửa sổ yêu cầu mở lại sau Resolved/Closed", 1, 90),
        Def(SystemSettingModule.Reports, "max_approved_reopens", SystemSettingValueType.Int, "1",
            "Số lần mở lại được duyệt tối đa", 0, 5),
        Def(SystemSettingModule.Reports, "flag_notify_threshold", SystemSettingValueType.Int, "3",
            "Số cờ công dân trước khi notify LEO", 1, 20),

        // ── SLA (Mục D) ──
        Def(SystemSettingModule.Sla, "sla_verify_hours", SystemSettingValueType.Int, "24",
            "SLA xác minh báo cáo Submitted (giờ)", 1, 168),
        Def(SystemSettingModule.Sla, "sla_resolve_days_critical", SystemSettingValueType.Int, "3",
            "SLA xử lý mức Critical (ngày)", 1, 30),
        Def(SystemSettingModule.Sla, "sla_resolve_days_high", SystemSettingValueType.Int, "5",
            "SLA xử lý mức High (ngày)", 1, 30),
        Def(SystemSettingModule.Sla, "sla_resolve_days_medium", SystemSettingValueType.Int, "7",
            "SLA xử lý mức Medium (ngày)", 1, 30),
        Def(SystemSettingModule.Sla, "sla_resolve_days_low", SystemSettingValueType.Int, "10",
            "SLA xử lý mức Low (ngày)", 1, 60),
        Def(SystemSettingModule.Sla, "overdue_pending_hours", SystemSettingValueType.Int, "72",
            "Quá hạn pending Submitted/Verified (giờ, BR-REP-008)", 1, 720),
        Def(SystemSettingModule.Sla, "unassigned_verified_hours", SystemSettingValueType.Int, "24",
            "Verified chưa phân công (giờ, BR-REP-009)", 1, 168),
        Def(SystemSettingModule.Sla, "sla_verify_breach_priority_boost", SystemSettingValueType.Int, "100",
            "Cộng priority khi breach SLA verify", 0, 500),

        // ── Geo (Mục E) ──
        Def(SystemSettingModule.Geo, "vietnam_min_latitude", SystemSettingValueType.Decimal, "8",
            "Vĩ độ tối thiểu Việt Nam", 0, 90),
        Def(SystemSettingModule.Geo, "vietnam_max_latitude", SystemSettingValueType.Decimal, "24",
            "Vĩ độ tối đa Việt Nam", 0, 90),
        Def(SystemSettingModule.Geo, "vietnam_min_longitude", SystemSettingValueType.Decimal, "102",
            "Kinh độ tối thiểu Việt Nam", 0, 180),
        Def(SystemSettingModule.Geo, "vietnam_max_longitude", SystemSettingValueType.Decimal, "110",
            "Kinh độ tối đa Việt Nam", 0, 180),
        Def(SystemSettingModule.Geo, "check_in_max_distance_meters", SystemSettingValueType.Int, "200",
            "Khoảng cách check-in tối đa (mét)", 50, 1000),
        Def(SystemSettingModule.Geo, "exif_gps_mismatch_meters", SystemSettingValueType.Int, "200",
            "Ngưỡng lệch EXIF vs GPS (mét)", 50, 2000),
        Def(SystemSettingModule.Geo, "inspection_soft_gps_meters", SystemSettingValueType.Int, "200",
            "GPS mềm khi xác nhận đến hiện trường thanh tra", 50, 2000),

        // ── Map public (Mục F) — no hotspot ──
        Def(SystemSettingModule.Map, "public_coordinate_decimal_places", SystemSettingValueType.Int, "4",
            "Số chữ số thập phân tọa độ public (~11m)", 2, 6),
        Def(SystemSettingModule.Map, "map_max_bounding_lat_span", SystemSettingValueType.Decimal, "6",
            "Span lat tối đa bbox", 1, 20),
        Def(SystemSettingModule.Map, "map_max_bounding_lng_span", SystemSettingValueType.Decimal, "8",
            "Span lng tối đa bbox", 1, 30),
        Def(SystemSettingModule.Map, "map_default_detail_limit", SystemSettingValueType.Int, "200",
            "Limit mặc định map detail", 50, 1000),
        Def(SystemSettingModule.Map, "map_max_detail_limit", SystemSettingValueType.Int, "500",
            "Limit tối đa map detail", 100, 2000),
        Def(SystemSettingModule.Map, "map_default_grid_level", SystemSettingValueType.Int, "3",
            "Grid level mặc định aggregate", 1, 10),
        Def(SystemSettingModule.Map, "map_viewport_default_days", SystemSettingValueType.Int, "30",
            "Viewport summary mặc định (ngày)", 1, 365),
        Def(SystemSettingModule.Map, "map_viewport_min_days", SystemSettingValueType.Int, "7",
            "Viewport summary min (ngày)", 1, 90),
        Def(SystemSettingModule.Map, "map_viewport_max_days", SystemSettingValueType.Int, "90",
            "Viewport summary max (ngày)", 7, 365),
        Def(SystemSettingModule.Map, "map_max_aggregate_rows", SystemSettingValueType.Int, "50000",
            "Cap an toàn số dòng aggregate", 1000, 200000),

        // ── Officer priority (Mục G) ──
        Def(SystemSettingModule.Officer, "priority_severity_weight", SystemSettingValueType.Int, "3",
            "Trọng số severity trong priority score", 0, 20),
        Def(SystemSettingModule.Officer, "priority_reporter_count_weight", SystemSettingValueType.Int, "2",
            "Trọng số reporter count", 0, 20),
        Def(SystemSettingModule.Officer, "priority_age_divisor_hours", SystemSettingValueType.Int, "24",
            "Chia tuổi báo cáo (giờ) trong công thức", 1, 168),
        Def(SystemSettingModule.Officer, "priority_sla_verify_breach_boost", SystemSettingValueType.Int, "100",
            "Boost priority khi SLA verify breach", 0, 500),

        // ── Cleanup (Mục H) ──
        Def(SystemSettingModule.Cleanup, "progress_stale_hours", SystemSettingValueType.Int, "24",
            "Nhắc tiến độ stale (giờ)", 1, 168),
        Def(SystemSettingModule.Cleanup, "progress_escalate_hours", SystemSettingValueType.Int, "48",
            "Escalate tiến độ stale (giờ)", 2, 336),
        Def(SystemSettingModule.Cleanup, "decline_window_hours", SystemSettingValueType.Int, "24",
            "Cửa sổ từ chối assignment (giờ)", 1, 168),
        Def(SystemSettingModule.Cleanup, "progress_update_interval_hours", SystemSettingValueType.Int, "24",
            "Khoảng cách cập nhật tiến độ khuyến nghị", 1, 168),

        // ── Notifications (Mục J) ──
        Def(SystemSettingModule.Notifications, "nearby_report_radius_meters", SystemSettingValueType.Int, "2000",
            "Bán kính thông báo báo cáo gần (mét)", 100, 10000),
        Def(SystemSettingModule.Notifications, "nearby_report_max_recipients", SystemSettingValueType.Int, "100",
            "Số citizen tối đa nhận nearby notify", 1, 500),
        Def(SystemSettingModule.Notifications, "max_notifications_per_type_per_day", SystemSettingValueType.Int, "20",
            "Anti-spam: tối đa notify/loại/ngày", 1, 100),

        // ── Auth (Mục L) ──
        Def(SystemSettingModule.Auth, "max_failed_login_attempts", SystemSettingValueType.Int, "5",
            "Số lần đăng nhập sai trước khóa", 3, 20),
        Def(SystemSettingModule.Auth, "lockout_minutes", SystemSettingValueType.Int, "30",
            "Thời gian khóa tài khoản (phút)", 5, 1440),
        Def(SystemSettingModule.Auth, "captcha_after_failed_attempts", SystemSettingValueType.Int, "3",
            "Yêu cầu CAPTCHA sau N lần sai", 1, 10),
        Def(SystemSettingModule.Auth, "otp_max_attempts", SystemSettingValueType.Int, "5",
            "Số lần nhập OTP sai tối đa", 3, 20),
        Def(SystemSettingModule.Auth, "account_soft_delete_retention_days", SystemSettingValueType.Int, "90",
            "Giữ soft-delete trước hard delete", 30, 365),

        // ── Comments (Mục M) ──
        Def(SystemSettingModule.Comments, "comment_edit_window_minutes", SystemSettingValueType.Int, "15",
            "Cửa sổ sửa bình luận (phút)", 1, 1440),
        Def(SystemSettingModule.Comments, "comment_ban_duration_days", SystemSettingValueType.Int, "7",
            "Thời gian ban bình luận (ngày)", 1, 90),

        // ── Organization (Mục N + workload) ──
        Def(SystemSettingModule.Organization, "staff_invitation_expiry_days", SystemSettingValueType.Int, "7",
            "Hết hạn lời mời nhân sự (ngày)", 1, 30),
        Def(SystemSettingModule.Organization, "invitation_response_days", SystemSettingValueType.Int, "7",
            "Thời hạn phản hồi lời mời (ngày, hiển thị template)", 1, 30),
        Def(SystemSettingModule.Organization, "max_tasks_per_team", SystemSettingValueType.Int, "6",
            "Số task đồng thời tối đa mỗi đội", 1, 30),
        Def(SystemSettingModule.Organization, "team_workload_warning_threshold", SystemSettingValueType.Int, "5",
            "Ngưỡng cảnh báo workload đội", 1, 30),
        Def(SystemSettingModule.Organization, "contract_warning_days", SystemSettingValueType.Json, "[30,7,1]",
            "Cảnh báo hết hạn hợp đồng (ngày trước expiry)", null, null),

        // ── Community cleanup (Mục O) ──
        Def(SystemSettingModule.CommunityCleanup, "community_before_images_max", SystemSettingValueType.Int, "5",
            "Số ảnh before tối đa community cleanup", 1, 20),
        Def(SystemSettingModule.CommunityCleanup, "check_in_reminder_minutes_before_start", SystemSettingValueType.Int, "15",
            "Nhắc check-in trước giờ bắt đầu (phút)", 5, 120),

        // ── Data retention (Mục P) ──
        Def(SystemSettingModule.DataRetention, "media_retention_years", SystemSettingValueType.Int, "2",
            "Giữ media (năm)", 1, 10),
        Def(SystemSettingModule.DataRetention, "audit_log_retention_months", SystemSettingValueType.Int, "12",
            "Giữ audit log (tháng)", 6, 60),
        Def(SystemSettingModule.DataRetention, "status_history_retention_months", SystemSettingValueType.Int, "12",
            "Giữ lịch sử trạng thái (tháng)", 6, 60),

        // ── Rate limits (Mục Q) ──
        Def(SystemSettingModule.RateLimits, "submit_max_per_hour", SystemSettingValueType.Int, "5",
            "Submit report tối đa/giờ", 1, 50),
        Def(SystemSettingModule.RateLimits, "submit_max_per_day", SystemSettingValueType.Int, "20",
            "Submit report tối đa/ngày", 1, 100),
        Def(SystemSettingModule.RateLimits, "submit_lock_seconds", SystemSettingValueType.Int, "3600",
            "Khóa submit khi vượt quota (giây)", 60, 86400),

        // ── Inspection (Mục I) ──
        Def(SystemSettingModule.Inspection, "inspection_sla_resolve_days_critical", SystemSettingValueType.Int, "3",
            "SLA thanh tra Critical (ngày)", 1, 30),
        Def(SystemSettingModule.Inspection, "inspection_sla_resolve_days_high", SystemSettingValueType.Int, "5",
            "SLA thanh tra High (ngày)", 1, 30),
        Def(SystemSettingModule.Inspection, "inspection_sla_resolve_days_medium", SystemSettingValueType.Int, "7",
            "SLA thanh tra Medium (ngày)", 1, 30),
        Def(SystemSettingModule.Inspection, "inspection_sla_resolve_days_low", SystemSettingValueType.Int, "10",
            "SLA thanh tra Low (ngày)", 1, 60),
        Def(SystemSettingModule.Inspection, "inspection_evidence_max_per_request", SystemSettingValueType.Int, "5",
            "Số evidence tối đa mỗi request", 1, 20),

        // ── AI (Mục R) ──
        Def(SystemSettingModule.Ai, "ai_timeout_seconds", SystemSettingValueType.Int, "5",
            "Timeout AI classify (giây)", 1, 60),
        Def(SystemSettingModule.Ai, "ai_compare_timeout_seconds", SystemSettingValueType.Int, "15",
            "Timeout AI compare (giây)", 1, 120),
        Def(SystemSettingModule.Ai, "ai_temp_image_ttl_seconds", SystemSettingValueType.Int, "900",
            "TTL ảnh tạm AI (giây)", 60, 3600),
        Def(SystemSettingModule.Ai, "presign_upload_ttl_minutes", SystemSettingValueType.Int, "15",
            "TTL presign upload (phút)", 1, 120),

        // ── Validation (Mục S) ──
        Def(SystemSettingModule.Validation, "reject_reason_min_length", SystemSettingValueType.Int, "20",
            "Độ dài tối thiểu lý do từ chối", 5, 500),
        Def(SystemSettingModule.Validation, "reopen_reason_min_length", SystemSettingValueType.Int, "20",
            "Độ dài tối thiểu lý do mở lại", 5, 500),
        Def(SystemSettingModule.Validation, "escalation_reason_min_length", SystemSettingValueType.Int, "50",
            "Độ dài tối thiểu lý do escalate", 10, 500),
    ];

    private static SystemSettingSeedDefinition Def(
        SystemSettingModule module,
        string key,
        SystemSettingValueType valueType,
        string defaultValue,
        string description,
        decimal? min,
        decimal? max)
        => new(module, key, valueType, defaultValue, description, min, max);
}
