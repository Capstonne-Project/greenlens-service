using Greenlens.Domain.Common;

namespace Greenlens.Application.Common;

public static partial class Errors
{
    public static class Cleanup
    {
        public static Error TooFarFromSite => new(
            "CLEANUP_TOO_FAR",
            "Vị trí check-in cách hiện trường hơn 200m. Vui lòng di chuyển gần hơn (BR-CLN-002).",
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
    }
}
