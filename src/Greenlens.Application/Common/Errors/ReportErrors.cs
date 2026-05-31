using Greenlens.Domain.Common;

namespace Greenlens.Application.Common;

public static partial class Errors
{
    public static class Reports
    {
        public static Error CategoryNotFound => new(
            "CATEGORY_NOT_FOUND",
            "Danh mục ô nhiễm không tồn tại hoặc đã ngưng hoạt động.",
            ErrorType.NotFound);

        public static Error AuthenticationRequired => new(
            "AUTHENTICATION_REQUIRED",
            "Báo cáo không ẩn danh yêu cầu đăng nhập.",
            ErrorType.Validation);

        public static Error InvalidWardProvincePair => new(
            "INVALID_WARD_PROVINCE",
            "Mã phường/xã không khớp với tỉnh/thành hoặc không tồn tại trong danh mục.",
            ErrorType.Validation);

        public static Error ReportNotFound => new(
            "REPORT_NOT_FOUND",
            "Không tìm thấy báo cáo.",
            ErrorType.NotFound);

        public static Error InvalidStatusTransition => new(
            "INVALID_STATUS_TRANSITION",
            "Không thể chuyển trạng thái từ trạng thái hiện tại.",
            ErrorType.BusinessRule);

        public static Error ReportAlreadyAssigned => new(
            "REPORT_ALREADY_ASSIGNED",
            "Báo cáo đã được phân công cho team và đang trong quá trình xử lý.",
            ErrorType.Conflict);

        public static Error ConflictOfInterest => new(
            "CONFLICT_OF_INTEREST",
            "Không thể xử lý báo cáo do bạn tạo.",
            ErrorType.BusinessRule);

        public static Error TeamTypeMismatch => new(
            "TEAM_TYPE_MISMATCH",
            "Loại Team không phù hợp với loại ô nhiễm.",
            ErrorType.BusinessRule);

        public static Error TeamWorkloadExceeded => new(
            "TEAM_WORKLOAD_EXCEEDED",
            "Team đang thực hiện task khác. Chỉ có thể giao task mới khi team hoàn thành task hiện tại.",
            ErrorType.BusinessRule);

        public static Error AtLeastOneTeam => new(
            "AT_LEAST_ONE_TEAM",
            "Phải phân công ít nhất 1 team.",
            ErrorType.Validation);

        public static Error ReasonTooShort => new(
            "REASON_TOO_SHORT",
            "Lý do phải có ít nhất 20 ký tự.",
            ErrorType.Validation);

        public static Error ReasonTooShort50 => new(
            "REASON_TOO_SHORT_50",
            "Lý do phải có ít nhất 50 ký tự.",
            ErrorType.Validation);

        public static Error ReopenLimitReached => new(
            "REOPEN_LIMIT_REACHED",
            "Đã hết số lần mở lại báo cáo (tối đa 2 lần).",
            ErrorType.BusinessRule);

        public static Error DeclineWindowExpired => new(
            "DECLINE_WINDOW_EXPIRED",
            "Đã hết thời gian từ chối task (2 giờ sau khi được gán).",
            ErrorType.BusinessRule);

        public static Error AssignmentNotFound => new(
            "ASSIGNMENT_NOT_FOUND",
            "Không tìm thấy phân công cho team này.",
            ErrorType.NotFound);

        public static Error NotTeamMember => new(
            "NOT_TEAM_MEMBER",
            "Bạn không phải thành viên của team được gán.",
            ErrorType.BusinessRule);

        public static Error NotTeamLeader => new(
            "NOT_TEAM_LEADER",
            "Chỉ Team Leader được thực hiện hành động này.",
            ErrorType.BusinessRule);

        public static Error ReassignSameTeamType => new(
            "REASSIGN_SAME_TEAM_TYPE",
            "Chỉ có thể chuyển giao giữa các team cùng loại.",
            ErrorType.BusinessRule);

        public static Error InsufficientAfterImages => new(
            "INSUFFICIENT_AFTER_IMAGES",
            "Cần upload ít nhất 2 ảnh after từ các góc khác nhau.",
            ErrorType.Validation);

        public static Error AssignmentNotInProgress => new(
            "ASSIGNMENT_NOT_IN_PROGRESS",
            "Chỉ có thể cập nhật tiến độ khi task đang InProgress.",
            ErrorType.BusinessRule);

        public static Error InvalidProgressPercent => new(
            "INVALID_PROGRESS_PERCENT",
            "Phần trăm tiến độ phải trong khoảng 0–100.",
            ErrorType.Validation);

        public static Error ReportNotAssigned => new(
            "REPORT_NOT_ASSIGNED",
            "Báo cáo chưa được phân công cho team nào.",
            ErrorType.BusinessRule);

        public static Error WasteTagNotFound => new(
            "WASTE_TAG_NOT_FOUND",
            "Một hoặc nhiều tag loại rác không tồn tại.",
            ErrorType.NotFound);

        public static Error WasteTagInactive => new(
            "WASTE_TAG_INACTIVE",
            "Một hoặc nhiều tag loại rác đã bị vô hiệu hóa.",
            ErrorType.BusinessRule);

        public static Error WasteTagCodeExists => new(
            "WASTE_TAG_CODE_EXISTS",
            "Mã tag loại rác đã tồn tại.",
            ErrorType.Conflict);

        public static Error DispatchOutsideProvince => new(
            "DISPATCH_OUTSIDE_PROVINCE",
            "Chỉ có thể điều phối task đến xã/phường trong phạm vi tỉnh của bạn.",
            ErrorType.BusinessRule);
    }
}
