using Greenlens.Domain.Common;

namespace Greenlens.Application.Common;

public static partial class Errors
{
    public static class Cleanup
    {
        public static Error TooFarFromSite(double distanceMeters) => new(
            "CLEANUP_TOO_FAR",
            $"Vị trí của bạn cách hiện trường khoảng {GeoDistanceFormatting.Format(distanceMeters)}. " +
            "Vui lòng di chuyển gần hơn (BR-CLN-002).",
            ErrorType.BusinessRule);

        public static Error CheckInRequired => new(
            "CLEANUP_CHECKIN_REQUIRED",
            "Phải check-in hiện trường trước khi bắt đầu dọn dẹp (BR-CLN-003).",
            ErrorType.BusinessRule);

        public static Error EscalateReasonRequired => new(
            "CLEANUP_ESCALATE_REASON_SHORT",
            "Lý do escalate phải có ít nhất 20 ký tự (BR-CLN-006).",
            ErrorType.Validation);

        public static Error ProgressUpdateRequired => new(
            "CLEANUP_PROGRESS_STALE",
            "Phải cập nhật tiến độ ít nhất 1 lần/ngày (BR-CLN-004).",
            ErrorType.BusinessRule);

        public static Error AssignmentNotInProgress => new(
            "CLEANUP_NOT_IN_PROGRESS",
            "Assignment không ở trạng thái InProgress.",
            ErrorType.BusinessRule);

        /// <summary>BR-CLN-004: progress can only stay the same (correction) or increase, never decrease.</summary>
        public static Error ProgressCannotDecrease(int currentPercent) => new(
            "CLEANUP_PROGRESS_CANNOT_DECREASE",
            $"Không thể cập nhật tiến độ thấp hơn {currentPercent}% đã lưu trước đó.",
            ErrorType.Validation);
    }
}
