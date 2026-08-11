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
            await SyncTemplateBodiesAsync(db, logger).ConfigureAwait(false);
            return;
        }

        foreach (var template in templates)
            template.Publish();

        db.Set<NotificationTemplate>().AddRange(templates);
        await db.SaveChangesAsync().ConfigureAwait(false);

        logger.LogInformation("Seeded {Count} notification template(s).", templates.Count);

        await SyncTemplateBodiesAsync(db, logger).ConfigureAwait(false);
    }

    /// <summary>Updates canonical body/title for templates that existed before placeholder refresh.</summary>
    private static async Task SyncTemplateBodiesAsync(ApplicationDbContext db, ILogger logger)
    {
        var updates = GetAllTemplates()
            .Select(t => (t.TemplateKey, t.TitleVi, t.BodyVi, t.TitleEn, t.BodyEn))
            .ToArray();

        var synced = 0;
        foreach (var (key, titleVi, bodyVi, titleEn, bodyEn) in updates)
        {
            var template = await db.Set<NotificationTemplate>()
                .FirstOrDefaultAsync(t => t.TemplateKey == key)
                .ConfigureAwait(false);

            if (template is null)
                continue;

            if (template.TitleVi == titleVi && template.BodyVi == bodyVi)
                continue;

            template.Update(titleVi, bodyVi, titleEn, bodyEn);
            template.Publish();
            synced++;
        }

        if (synced == 0)
            return;

        await db.SaveChangesAsync().ConfigureAwait(false);
        logger.LogInformation("Synced {Count} notification template body/title(s).", synced);
    }

    private static List<NotificationTemplate> GetAllTemplates() =>
    [
        Create(
            "report_status_changed",
            "Trạng thái báo cáo thay đổi",
            "Báo cáo {report_code} của bạn đã chuyển sang trạng thái: {status}.",
            "Report status changed",
            "Your {report_code} report has been changed to: {status}.",
            NotificationType.ReportStatusChanged),

        Create(
            "new_comment",
            "Bình luận mới",
            "Có một bình luận mới trên báo cáo {report_code} của bạn.",
            "New comment",
            "There is a new comment on your report {report_code}.",
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
            "Cảnh báo sắp quá hạn xử lý",
            "Báo cáo {report_code} tại {ward_name} sắp vượt thời hạn xử lý cam kết. Vui lòng kiểm tra ngay.",
            "SLA Breach Warning",
            "Report {report_code} is about to breach its Service Level Agreement (SLA). Please check immediately.",
            NotificationType.SlaBreachWarning),

        Create(
            "sla_verification_breached_leo",
            "Quá hạn xác minh",
            "Báo cáo {report_code} tại {ward_name} đã quá 24 giờ chưa được xác minh. Vui lòng xử lý ưu tiên.",
            "Verification SLA overdue",
            "Report {report_code} at {ward_name} has not been verified within 24 hours. Please prioritize.",
            NotificationType.SlaVerificationBreachedLeo),

        Create(
            "sla_verification_escalated_deo",
            "Tiếp nhận báo cáo chuyển cấp",
            "Báo cáo {report_code} tại {ward_name} ({province_name}) đã vượt thời hạn xác minh — chuyển vào hàng đợi của bạn.",
            "Escalated report received",
            "Report {report_code} exceeded verification SLA and is now in your queue.",
            NotificationType.SlaVerificationEscalatedDeo),

        Create(
            "sla_resolution_breached",
            "Quá hạn xử lý",
            "Báo cáo {report_code} tại {ward_name} (mức {severity}) đã vượt thời hạn xử lý. Vui lòng kiểm tra.",
            "Resolution SLA breached",
            "Report {report_code} ({severity}) exceeded the resolution SLA. Please review.",
            NotificationType.SlaResolutionBreached),

        Create(
            "sla_inspection_breached",
            "Quá hạn điều tra xử phạt",
            "Hồ sơ xử phạt liên quan báo cáo {report_code} tại {ward_name} đã vượt thời hạn điều tra. Vui lòng kiểm tra và xử lý.",
            "Inspection SLA breached",
            "The penalty dossier for report {report_code} exceeded the investigation SLA. Please review.",
            NotificationType.SlaInspectionBreached),

        Create(
            "inspection_task_assigned",
            "Nhiệm vụ thanh tra mới",
            "Báo cáo {report_code} tại {ward_name} vừa được giao cho đội {team_name}. Vui lòng kiểm tra hàng đợi nhiệm vụ thanh tra.",
            "New inspection task assigned",
            "Report {report_code} was assigned to team {team_name}. Please check your inspection task queue.",
            NotificationType.InspectionTaskAssigned),

        Create(
            "inspection_task_declined",
            "Đội thanh tra từ chối nhiệm vụ",
            "Đội {team_name} đã từ chối hồ sơ xử phạt liên quan báo cáo {report_code} tại {ward_name}. Lý do: {decline_reason}. Vui lòng gán lại đội khác.",
            "Inspection team declined task",
            "Team {team_name} declined the penalty dossier for report {report_code}. Reason: {decline_reason}. Please re-assign another team.",
            NotificationType.InspectionTaskDeclined),

        Create(
            "inspection_task_accepted",
            "Đội thanh tra chấp nhận nhiệm vụ",
            "Đội {team_name} đã chấp nhận nhiệm vụ thanh tra báo cáo {report_code} tại {ward_name}.",
            "Inspection team accepted task",
            "Team {team_name} accepted the inspection task for report {report_code}.",
            NotificationType.InspectionTaskAccepted),

        Create(
            "inspection_progress_updated",
            "Đội cập nhật tiến độ thanh tra",
            "Đội {team_name} {activity_summary} cho hồ sơ xử phạt liên quan báo cáo {report_code} tại {ward_name}.",
            "Inspection progress updated",
            "Team {team_name} {activity_summary} for the penalty dossier linked to report {report_code}.",
            NotificationType.InspectionProgressUpdated),

        Create(
            "inspection_task_completed",
            "Đội hoàn thành nhiệm vụ thanh tra",
            "Đội {team_name} đã hoàn thành nhiệm vụ thanh tra cho báo cáo {report_code} tại {ward_name}: {outcome_summary}.",
            "Inspection team completed task",
            "Team {team_name} completed the inspection task for report {report_code}: {outcome_summary}.",
            NotificationType.InspectionTaskCompleted),

        Create(
            "inspection_closed_no_violation",
            "Kết luận không phát hiện vi phạm",
            "Hồ sơ xử phạt liên quan báo cáo {report_code} đã được kết luận không đủ căn cứ xử phạt. Lý do: {reason}",
            "Inspection closed — no violation",
            "The penalty dossier for report {report_code} was closed with no violation found. Reason: {reason}",
            NotificationType.InspectionClosedNoViolation),

        Create(
            "penalty_payment_overdue",
            "Quá hạn nộp phạt",
            "Hồ sơ xử phạt liên quan báo cáo {report_code} tại {ward_name} đã quá hạn nộp phạt (quyết định số {decision_number}). Vui lòng phối hợp xử lý.",
            "Penalty payment overdue",
            "The penalty dossier for report {report_code} is overdue for payment (decision {decision_number}). Please coordinate follow-up.",
            NotificationType.PenaltyPaymentOverdue),

        Create(
            "inspection_penalty_paid_and_closed",
            "Đã nộp phạt đủ — hồ sơ đã đóng",
            "LEO đã ghi nhận báo cáo {report_code} nộp phạt đủ {paid_amount}đ và đóng hồ sơ xử phạt.",
            "Penalty paid in full — dossier closed",
            "LEO recorded a full payment of {paid_amount} for report {report_code} and closed the penalty dossier.",
            NotificationType.InspectionPenaltyPaidAndClosed),

        Create(
            "cleanup_progress_stale",
            "Đội chưa cập nhật tiến độ dọn dẹp",
            "Đội {team_name} chưa cập nhật tiến độ dọn dẹp hơn 48 giờ cho báo cáo {report_code} tại {ward_name}.",
            "Cleanup progress stale >48h",
            "Team {team_name} has not updated progress for report {report_code} in over 48 hours.",
            NotificationType.CleanupProgressStale),

        Create(
            "nearby_report",
            "Có báo cáo ô nhiễm gần bạn",
            "Báo cáo {report_code} ({category_name}) vừa được ghi nhận tại {ward_name}, trong bán kính 2 km quanh khu vực bạn từng báo cáo.",
            "Nearby pollution report",
            "Report {report_code} ({category_name}) was recorded within 2km of an area you previously reported.",
            NotificationType.NearbyReport),

        Create(
            "penalty_issued",
            "Thông báo xử phạt",
            "Quyết định {decision_number} đã ban hành cho báo cáo {report_code} với mức phạt {penalty_amount} VND.",
            "Penalty issued",
            "Decision {decision_number} was issued for report {report_code} with a penalty of {penalty_amount} VND.",
            NotificationType.PenaltyIssued),

        Create(
            "contract_expiry",
            "Hợp đồng sắp hết hạn",
            "Hợp đồng xử lý môi trường của bạn sắp hết hạn.",
            "Contract Expiry",
            "Your environmental service contract is about to expire.",
            NotificationType.ContractExpiry),

        Create(
            "contract_expired",
            "Hợp đồng đã hết hạn",
            "Hợp đồng công ty {company_name} ({contract_number}) đã hết hạn. Tài khoản công ty đã bị vô hiệu hóa.",
            "Contract expired",
            "Company {company_name} contract ({contract_number}) has expired. The company account was deactivated.",
            NotificationType.ContractExpired),

        Create(
            "contract_expiry_warning",
            "Hợp đồng sắp hết hạn ({days_left} ngày)",
            "Hợp đồng công ty {company_name} sẽ hết hạn trong {days_left} ngày ({end_date}).",
            "Contract expiring ({days_left} days)",
            "Company {company_name} contract expires in {days_left} days ({end_date}).",
            NotificationType.ContractExpiryWarning),

        Create(
            "company_report_dispatched",
            "Báo cáo mới trong hàng đợi công ty",
            "Báo cáo {report_code} tại {ward_name} đã được cán bộ phường điều phối đến công ty {company_name}. Vui lòng phân công đội xử lý.",
            "New report in company queue",
            "Report {report_code} was dispatched to company {company_name}. Please assign a team from the company queue.",
            NotificationType.CompanyReportDispatched),

        Create(
            "company_team_assigned",
            "Công ty đã phân công đội xử lý",
            "Công ty {company_name} đã phân công đội {team_names} xử lý báo cáo {report_code} tại {ward_name}.",
            "Company assigned cleanup team",
            "Company {company_name} assigned team(s) {team_names} to report {report_code}.",
            NotificationType.CompanyTeamAssigned),

        Create(
            "report_overdue",
            "Báo cáo quá hạn",
            "Báo cáo {report_code} tại {ward_name} đã tồn tại quá 72 giờ mà chưa được xử lý.",
            "Report Overdue",
            "Report {report_code} has been pending for over 72h without resolution.",
            NotificationType.ReportOverdue),

        Create(
            "report_unassigned",
            "Báo cáo chưa được phân công",
            "Báo cáo {report_code} tại {ward_name} đã được xác minh nhưng chưa có đội xử lý trong 24 giờ.",
            "Report Unassigned",
            "Report {report_code} has been verified but remains unassigned for 24h.",
            NotificationType.ReportUnassigned),

        Create(
            "report_auto_closed",
            "Báo cáo tự động đóng",
            "Báo cáo {report_code} đã được hệ thống tự động đóng sau 2 ngày chờ xác nhận.",
            "Report Auto-Closed",
            "Report {report_code} has been automatically closed after 2 days pending confirmation.",
            NotificationType.ReportAutoClosed),

        Create(
            "duplicate_review_needed",
            "Báo cáo cần xem xét trùng lặp",
            "Báo cáo {report_code} tại {ward_name} cần xem xét trùng lặp: {detection_summary}. Vui lòng kiểm tra hàng đợi nghi ngờ trùng.",
            "Duplicate review needed",
            "Report {report_code} needs duplicate review: {detection_summary}. Please check the duplicate queue.",
            NotificationType.DuplicateReviewNeeded),

        Create(
            "violation_recurrence_review_needed",
            "Nghi ngờ vi phạm tái phát",
            "Báo cáo {report_code} tại {ward_name} gần điểm đã xử lý ({prior_report_code}). Vui lòng so sánh và quyết định có mở hồ sơ thanh tra hay không.",
            "Suspected violation recurrence",
            "Report {report_code} is near a recently closed case ({prior_report_code}). Please compare and decide whether to open an inspection dossier.",
            NotificationType.ViolationRecurrenceReviewNeeded),

        Create(
            "reopen_review_needed",
            "Yêu cầu mở lại báo cáo",
            "Công dân yêu cầu mở lại báo cáo {report_code} tại {ward_name}. Vui lòng xem lý do và ảnh minh chứng.",
            "Reopen request received",
            "A citizen requested to reopen report {report_code}. Please review the reason and evidence.",
            NotificationType.ReopenReviewNeeded),

        Create(
            "reopen_request_decided",
            "Quyết định yêu cầu mở lại",
            "Cán bộ phường {ward_name} đã {decision} yêu cầu mở lại báo cáo {report_code}. {reason}",
            "Reopen request decided",
            "LEO {decision} your reopen request for report {report_code}. {reason}",
            NotificationType.ReopenRequestDecided),

        Create(
            "cleanup_task_assigned",
            "Nhiệm vụ dọn dẹp mới",
            "Báo cáo {report_code} tại {ward_name} vừa được giao cho đội {team_name}. Vui lòng kiểm tra hàng đợi nhiệm vụ.",
            "New cleanup task assigned",
            "Report {report_code} was assigned to team {team_name}. Please check your task queue.",
            NotificationType.CleanupTaskAssigned),

        Create(
            "cleanup_task_accepted",
            "Đội đã chấp nhận nhiệm vụ dọn dẹp",
            "Đội {team_name} đã chấp nhận nhiệm vụ dọn dẹp báo cáo {report_code} tại {ward_name}.",
            "Cleanup team accepted task",
            "Team {team_name} accepted the cleanup task for report {report_code}.",
            NotificationType.CleanupTaskAccepted),

        Create(
            "cleanup_task_declined",
            "Đội từ chối nhiệm vụ dọn dẹp",
            "Đội {team_name} đã từ chối nhiệm vụ dọn dẹp báo cáo {report_code} tại {ward_name}. Lý do: {decline_reason}. Vui lòng phân công lại.",
            "Cleanup team declined task",
            "Team {team_name} declined the cleanup task for report {report_code}. Reason: {decline_reason}. Please re-assign.",
            NotificationType.CleanupTaskDeclined),

        Create(
            "cleanup_progress_updated",
            "Đội cập nhật tiến độ dọn dẹp",
            "Đội {team_name} cập nhật tiến độ {progress_percent}% cho báo cáo {report_code} tại {ward_name}.",
            "Cleanup progress updated",
            "Team {team_name} updated progress to {progress_percent}% for report {report_code}.",
            NotificationType.CleanupProgressUpdated),

        Create(
            "cleanup_task_completed",
            "Đội hoàn thành nhiệm vụ dọn dẹp",
            "Đội {team_name} đã hoàn thành nhiệm vụ dọn dẹp báo cáo {report_code} tại {ward_name}.{resolution_note}",
            "Cleanup team completed task",
            "Team {team_name} completed the cleanup task for report {report_code}.{resolution_note}",
            NotificationType.CleanupTaskCompleted),

        Create(
            "report_verification_needed",
            "Báo cáo mới cần xác minh",
            "Báo cáo {report_code} tại {ward_name} ({province_name}) vừa được gửi và đang chờ bạn xác minh.",
            "New report awaiting verification",
            "Report {report_code} was just submitted and is waiting for your verification.",
            NotificationType.ReportVerificationNeeded),

        Create(
            "staff_invitation_received",
            "Lời mời tham gia đội môi trường",
            "{inviter_name} đã mời bạn tham gia vai trò {target_role} tại phường/xã {ward_name}{team_clause}. Vui lòng xem và phản hồi trong 7 ngày.",
            "Staff invitation received",
            "{inviter_name} invited you to join as {target_role} at {office_name}{team_clause}. Please respond within 7 days.",
            NotificationType.StaffInvitationReceived),

        Create(
            "staff_invitation_accepted",
            "Thành viên đã chấp nhận lời mời",
            "{member_name} đã chấp nhận lời mời tham gia vai trò {target_role} tại phường/xã {ward_name}{team_clause}.",
            "Staff invitation accepted",
            "{member_name} accepted your invitation to join as {target_role} at {office_name}{team_clause}.",
            NotificationType.StaffInvitationAccepted),

        Create(
            "staff_invitation_declined",
            "Thành viên đã từ chối lời mời",
            "{member_name} đã từ chối lời mời tham gia vai trò {target_role} tại phường/xã {ward_name}.",
            "Staff invitation declined",
            "{member_name} declined your invitation to join as {target_role} at {office_name}.",
            NotificationType.StaffInvitationDeclined),

        Create(
            "community_cleanup_opened",
            "Chương trình dọn cộng đồng vừa mở",
            "Chương trình \"{title}\" vừa mở đăng ký. Tham gia ngay để cùng dọn dẹp!",
            "Community cleanup opened",
            "The \"{title}\" community cleanup program is now open for joining!",
            NotificationType.CommunityCleanupOpened),

        Create(
            "community_cleanup_leader_assigned",
            "Bạn được chỉ định làm Leader",
            "Bạn được chỉ định làm Leader cho chương trình dọn cộng đồng \"{title}\". Hãy vào mục Việc của tôi để bắt đầu.",
            "You were appointed Leader",
            "You were appointed Leader for the community cleanup program \"{title}\". Check My Tasks to get started.",
            NotificationType.CommunityCleanupLeaderAssigned),

        Create(
            "community_cleanup_started",
            "Chương trình dọn cộng đồng đã bắt đầu",
            "Leader đã có mặt và chương trình \"{title}\" đã bắt đầu. Hãy check-in nếu bạn đã đến điểm hẹn!",
            "Community cleanup started",
            "The Leader has arrived and \"{title}\" has started. Check in if you're at the meeting point!",
            NotificationType.CommunityCleanupStarted),

        Create(
            "community_cleanup_progress_updated",
            "Cập nhật tiến độ dọn dẹp",
            "Chương trình \"{title}\" đã hoàn thành {percent}% tiến độ.",
            "Cleanup progress updated",
            "\"{title}\" has reached {percent}% progress.",
            NotificationType.CommunityCleanupProgressUpdated),

        Create(
            "community_cleanup_verification_submitted",
            "Cần duyệt hoàn thành chương trình",
            "Leader đã nộp minh chứng hoàn thành cho chương trình \"{title}\". Hãy vào duyệt.",
            "Cleanup evidence needs review",
            "The Leader submitted completion evidence for \"{title}\". Please review it.",
            NotificationType.CommunityCleanupVerificationSubmitted),

        Create(
            "community_cleanup_verification_rejected",
            "Minh chứng bị từ chối",
            "Minh chứng hoàn thành cho chương trình \"{title}\" đã bị từ chối. Lý do: {reason}",
            "Evidence rejected",
            "Your completion evidence for \"{title}\" was rejected. Reason: {reason}",
            NotificationType.CommunityCleanupVerificationRejected),

        Create(
            "community_cleanup_verified",
            "Chương trình dọn cộng đồng đã hoàn thành",
            "Chương trình \"{title}\" đã được xác nhận hoàn thành. Cảm ơn bạn đã tham gia!",
            "Community cleanup completed",
            "\"{title}\" has been verified as complete. Thanks for participating!",
            NotificationType.CommunityCleanupVerified),

        Create(
            "community_cleanup_checkin_reminder",
            "Sắp đến giờ dọn dẹp",
            "Chương trình \"{title}\" sẽ bắt đầu sau 15 phút. Đừng quên check-in khi đến điểm hẹn!",
            "Cleanup starting soon",
            "\"{title}\" starts in 15 minutes. Don't forget to check in when you arrive!",
            NotificationType.CommunityCleanupCheckInReminder),

        Create(
            "badge_progress_near",
            "Sắp đạt huy hiệu mới",
            "Bạn đã đạt {current}/{target} cho huy hiệu \"{badge_name}\". Cố gắng thêm chút nữa để nhận huy hiệu!",
            "Almost there for a new badge",
            "You've reached {current}/{target} toward the \"{badge_name}\" badge. Keep going to earn it!",
            NotificationType.BadgeProgressNear)
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
