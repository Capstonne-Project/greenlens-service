using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.Seeders.Administrator;

internal static class NotificationTemplateSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, ILogger logger)
    {
        var existingTypes = await db.Set<NotificationTemplate>()
            .Select(t => t.Type)
            .ToListAsync()
            .ConfigureAwait(false);

        var templates = GetAllTemplates()
            .Where(t => !existingTypes.Contains(t.Type))
            .ToList();

        if (templates.Count == 0)
        {
            logger.LogDebug("Notification templates up to date — no new types to seed.");
            return;
        }

        foreach (var template in templates)
            template.Publish();

        db.Set<NotificationTemplate>().AddRange(templates);
        await db.SaveChangesAsync().ConfigureAwait(false);

        logger.LogInformation("Seeded {Count} notification template(s).", templates.Count);
    }

    private static List<NotificationTemplate> GetAllTemplates() =>
    [
        Create(
            "report_status_changed",
            "Trạng thái báo cáo thay đổi",
            "Báo cáo {report_id} của bạn đã chuyển sang trạng thái: {status}.",
            "Report status changed",
            "Your {report_id} report has been changed to: {status}.",
            NotificationType.ReportStatusChanged),

        Create(
            "new_comment",
            "Bình luận mới",
            "Có một bình luận mới trên báo cáo {report_id} của bạn.",
            "New comment",
            "There is a new comment on your report {report_id}.",
            NotificationType.NewComment),

        Create(
            "badge_earned",
            "Chúc mừng! Bạn nhận được huy hiệu mới",
            "Bạn vừa nhận được huy hiệu {badge_name} vì những đóng góp tích cực.",
            "Congratulations! New badge earned",
            "You have earned the {badge_name} badge for your positive contributions.",
            NotificationType.BadgeEarned),

        Create(
            "level_up",
            "Lên cấp!",
            "Chúc mừng bạn đã đạt cấp độ {level}!",
            "Level up!",
            "Congratulations on reaching level {level}!",
            NotificationType.LevelUp),

        Create(
            "sla_breach_warning",
            "Cảnh báo vi phạm SLA",
            "Báo cáo {report_id} sắp vi phạm thời gian xử lý cam kết (SLA). Vui lòng kiểm tra ngay.",
            "SLA Breach Warning",
            "Report {report_id} is about to breach its Service Level Agreement (SLA). Please check immediately.",
            NotificationType.SlaBreachWarning),

        Create(
            "nearby_report",
            "Có báo cáo ô nhiễm gần bạn",
            "Một báo cáo ô nhiễm mới vừa được ghi nhận cách bạn không xa.",
            "Nearby pollution report",
            "A new pollution report has been recorded near your location.",
            NotificationType.NearbyReport),

        Create(
            "penalty_issued",
            "Thông báo xử phạt",
            "Có một quyết định xử phạt liên quan đến báo cáo của bạn.",
            "Penalty Issued",
            "A penalty decision has been issued related to your report.",
            NotificationType.PenaltyIssued),

        Create(
            "contract_expiry",
            "Hợp đồng sắp hết hạn",
            "Hợp đồng xử lý môi trường của bạn sắp hết hạn.",
            "Contract Expiry",
            "Your environmental service contract is about to expire.",
            NotificationType.ContractExpiry),

        Create(
            "report_overdue",
            "Báo cáo quá hạn",
            "Báo cáo {report_id} đã tồn tại quá 72h mà chưa được xử lý.",
            "Report Overdue",
            "Report {report_id} has been pending for over 72h without resolution.",
            NotificationType.ReportOverdue),

        Create(
            "report_unassigned",
            "Báo cáo chưa được phân công",
            "Báo cáo {report_id} đã được xác minh nhưng chưa có người xử lý trong vòng 24h.",
            "Report Unassigned",
            "Report {report_id} has been verified but remains unassigned for 24h.",
            NotificationType.ReportUnassigned),

        Create(
            "report_auto_closed",
            "Báo cáo tự động đóng",
            "Báo cáo {report_id} đã được hệ thống tự động đóng sau 7 ngày chờ xác nhận.",
            "Report Auto-Closed",
            "Report {report_id} has been automatically closed after 7 days pending confirmation.",
            NotificationType.ReportAutoClosed),

        Create(
            "duplicate_review_needed",
            "Báo cáo cần xem xét trùng lặp",
            "Báo cáo {report_code} đã nhận {flag_count} cờ ({flag_type}). Vui lòng xem xét trong queue nghi ngờ trùng.",
            "Duplicate review needed",
            "Report {report_code} received {flag_count} flag(s) ({flag_type}). Please review the duplicate queue.",
            NotificationType.DuplicateReviewNeeded),

        Create(
            "report_verification_needed",
            "Báo cáo mới cần xác minh",
            "Báo cáo {report_code} vừa được gửi và đang chờ bạn xác minh.",
            "New report awaiting verification",
            "Report {report_code} was just submitted and is waiting for your verification.",
            NotificationType.ReportVerificationNeeded)
    ];

    private static NotificationTemplate Create(
        string key, string titleVi, string bodyVi, string titleEn, string bodyEn, NotificationType type)
    {
        return NotificationTemplate.Create(
            templateKey: key,
            titleVi: titleVi,
            bodyVi: bodyVi,
            titleEn: titleEn,
            bodyEn: bodyEn,
            channel: NotificationChannel.Both,
            type: type);
    }
}
