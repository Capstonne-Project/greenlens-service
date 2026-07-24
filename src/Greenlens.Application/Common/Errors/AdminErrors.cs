using Greenlens.Domain.Common;

namespace Greenlens.Application.Common;

public static partial class Errors
{
    public static class Admin
    {
        // ── Notification Templates (BR-ADM-004) ──

        public static Error NotificationTemplateNotFound => new(
            "NOTIFICATION_TEMPLATE_NOT_FOUND",
            "Mẫu thông báo không tồn tại.",
            ErrorType.NotFound);

        public static Error NotificationTemplateDuplicate(string templateKey, string channel) => new(
            "NOTIFICATION_TEMPLATE_DUPLICATE",
            $"Mẫu thông báo '{templateKey}' đã tồn tại cho kênh {channel}.",
            ErrorType.Conflict);

        // ── Penalty Frameworks (BR-ADM-008) ──

        public static Error PenaltyFrameworkNotFound => new(
            "PENALTY_FRAMEWORK_NOT_FOUND",
            "Không tìm thấy khung xử phạt.",
            ErrorType.NotFound);

        public static Error PenaltyFrameworkCategoryNotFound => new(
            "PENALTY_FRAMEWORK_CATEGORY_NOT_FOUND",
            "Danh mục ô nhiễm không tồn tại.",
            ErrorType.NotFound);

        public static Error PenaltyFrameworkDuplicate(string violationLevel) => new(
            "PENALTY_FRAMEWORK_DUPLICATE",
            $"Đã tồn tại khung xử phạt đang hoạt động cho danh mục và mức vi phạm '{violationLevel}' này.",
            ErrorType.Conflict);

        // ── Gamification Config (BR-ADM-005) ──

        public static Error GamificationConfigNotFound => new(
            "GAMIFICATION_CONFIG_NOT_FOUND",
            "Cấu hình điểm thưởng không tồn tại.",
            ErrorType.NotFound);

        // ── Content Moderation (BR-ADM-006) ──

        public static Error ReportAlreadyHidden => new(
            "REPORT_ALREADY_HIDDEN",
            "Báo cáo đã bị ẩn trước đó.",
            ErrorType.Conflict);

        public static Error ReportNotHidden => new(
            "REPORT_NOT_HIDDEN",
            "Báo cáo không bị ẩn, không thể bỏ ẩn.",
            ErrorType.Conflict);

        // ── Audit Log (BR-ADM-010) ──

        public static Error AuditLogNotFound => new(
            "AUDIT_LOG_NOT_FOUND",
            "Không tìm thấy bản ghi nhật ký.",
            ErrorType.NotFound);
    }
}
