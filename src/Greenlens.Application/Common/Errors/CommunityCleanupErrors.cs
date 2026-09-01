using Greenlens.Domain.Common;

namespace Greenlens.Application.Common;

/// <summary>
/// Draft BR-CMU-* errors — docs/community-cleanup-feature-spec.md. IDs pending official assignment.
/// </summary>
public static partial class Errors
{
    public static class CommunityCleanup
    {
        public static Error ReportNotVerified => new(
            "REPORT_NOT_VERIFIED",
            "Chỉ có thể mở chương trình dọn cộng đồng trên báo cáo đã xác minh (Verified).",
            ErrorType.BusinessRule);

        public static Error CommunityAlreadyActive => new(
            "COMMUNITY_ALREADY_ACTIVE",
            "Báo cáo đã có chương trình dọn cộng đồng đang hoạt động.",
            ErrorType.Conflict);

        public static Error ReportHasActiveAssignment => new(
            "REPORT_HAS_ACTIVE_ASSIGNMENT",
            "Báo cáo đã được phân công cho team/công ty. Không thể mở chương trình cộng đồng.",
            ErrorType.Conflict);

        public static Error LeaderNotCleaner => new(
            "LEADER_NOT_CLEANER",
            "Leader phải là người dùng có vai trò Cleaner.",
            ErrorType.Validation);

        public static Error LeaderNotInCleanupTeam => new(
            "LEADER_NOT_IN_CLEANUP_TEAM",
            "Leader phải thuộc một đội Cleanup đang hoạt động.",
            ErrorType.BusinessRule);

        public static Error EventNotFound => new(
            "COMMUNITY_EVENT_NOT_FOUND",
            "Không tìm thấy chương trình dọn cộng đồng.",
            ErrorType.NotFound);

        public static Error InvalidStatusTransition => new(
            "COMMUNITY_INVALID_STATUS_TRANSITION",
            "Không thể thực hiện thao tác này ở trạng thái hiện tại của chương trình.",
            ErrorType.BusinessRule);

        public static Error JoinClosed => new(
            "JOIN_CLOSED",
            "Chương trình đã đóng đăng ký.",
            ErrorType.BusinessRule);

        public static Error EventFull => new(
            "EVENT_FULL",
            "Chương trình đã đủ số lượng người tham gia.",
            ErrorType.BusinessRule);

        public static Error AlreadyJoined => new(
            "ALREADY_JOINED",
            "Bạn đã tham gia chương trình này rồi.",
            ErrorType.Conflict);

        public static Error NotCitizen => new(
            "NOT_CITIZEN",
            "Chỉ Citizen mới có thể tham gia chương trình dọn cộng đồng.",
            ErrorType.Forbidden);

        public static Error ParticipantNotFound => new(
            "COMMUNITY_PARTICIPANT_NOT_FOUND",
            "Bạn chưa tham gia chương trình này.",
            ErrorType.NotFound);

        public static Error CannotWithdraw => new(
            "CANNOT_WITHDRAW",
            "Không thể rút khỏi chương trình sau khi đã check-in hoặc chương trình đã bắt đầu.",
            ErrorType.BusinessRule);

        public static Error NotEventLeader => new(
            "NOT_EVENT_LEADER",
            "Chỉ Leader của chương trình mới được thực hiện hành động này.",
            ErrorType.Forbidden);

        public static Error NotAuthorized => new(
            "COMMUNITY_NOT_AUTHORIZED",
            "Bạn không có quyền xem thông tin này.",
            ErrorType.Forbidden);

        public static Error CheckInTooFar(double distanceMeters) => new(
            "COMMUNITY_CHECKIN_TOO_FAR",
            $"Vị trí của bạn cách điểm tập trung khoảng {GeoDistanceFormatting.Format(distanceMeters)}. " +
            "Ghi rõ lý do để vẫn xác nhận có mặt.",
            ErrorType.BusinessRule);

        public static Error ReasonTooShort => new(
            "COMMUNITY_REASON_TOO_SHORT",
            "Lý do phải có ít nhất 20 ký tự.",
            ErrorType.Validation);

        public static Error InsufficientVerificationEvidence => new(
            "INSUFFICIENT_VERIFICATION_EVIDENCE",
            "Cần ít nhất 1 ảnh before và 2 ảnh after trước khi nộp xác thực.",
            ErrorType.Validation);

        public static Error ProgressNotComplete => new(
            "PROGRESS_NOT_COMPLETE",
            "Tiến độ phải đạt 100% trước khi nộp xác thực.",
            ErrorType.BusinessRule);

        public static Error TooEarlyToStart => new(
            "COMMUNITY_TOO_EARLY_TO_START",
            "Chưa đến giờ bắt đầu dọn dẹp của chương trình này.",
            ErrorType.BusinessRule);

        public static Error ProgressCannotDecrease => new(
            "COMMUNITY_PROGRESS_CANNOT_DECREASE",
            "Tiến độ không thể giảm so với mức đã cập nhật trước đó.",
            ErrorType.BusinessRule);
    }
}
