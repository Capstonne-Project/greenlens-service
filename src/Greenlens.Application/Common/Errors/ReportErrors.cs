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

        /// <summary>BR-ORG-012: LEO cannot verify reports outside their assigned ward.</summary>
        public static Error OutsideJurisdiction => new(
            "OUTSIDE_JURISDICTION",
            "Bạn không có quyền tiếp nhận báo cáo ngoài khu vực.",
            ErrorType.Forbidden);

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

        public static Error NotReporter => new(
            "NOT_REPORT_OWNER",
            "Chỉ người gửi báo cáo mới được thực hiện hành động này.",
            ErrorType.Forbidden);

        public static Error ReopenLimitReached => new(
            "REOPEN_LIMIT_REACHED",
            "Đã hết số lần mở lại báo cáo (tối đa 2 lần).",
            ErrorType.BusinessRule);

        public static Error DeclineWindowExpired => new(
            "DECLINE_WINDOW_EXPIRED",
            "Đã hết thời gian từ chối task (24 giờ sau khi được gán).",
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

        public static Error CannotAssignCompanyTeamDirectly => new(
            "CANNOT_ASSIGN_COMPANY_TEAM_DIRECTLY",
            "Không thể phân công trực tiếp team của công ty. Hãy điều phối task đến công ty trước, sau đó CompanyManager sẽ phân công team.",
            ErrorType.BusinessRule);

        public static Error CompanyNotFound => new(
            "COMPANY_NOT_FOUND",
            "Không tìm thấy công ty dịch vụ môi trường.",
            ErrorType.NotFound);

        public static Error CompanyNotActive => new(
            "COMPANY_NOT_ACTIVE",
            "Công ty chưa kích hoạt hoặc hợp đồng đã hết hạn.",
            ErrorType.BusinessRule);

        public static Error ReportAlreadyDispatchedToCompany => new(
            "REPORT_ALREADY_DISPATCHED",
            "Báo cáo đã được điều phối đến công ty.",
            ErrorType.Conflict);

        public static Error ReportNotDispatchedToYourCompany => new(
            "REPORT_NOT_DISPATCHED_TO_YOUR_COMPANY",
            "Báo cáo không được điều phối đến công ty của bạn.",
            ErrorType.BusinessRule);

        public static Error CompanyDoesNotServeWard => new(
            "COMPANY_DOES_NOT_SERVE_WARD",
            "Công ty không phụ trách phường/xã của báo cáo này. Kiểm tra lại vùng phục vụ của công ty.",
            ErrorType.BusinessRule);

        /// <summary>BR-REP-012: Login required to submit report.</summary>
        public static Error LoginRequired => new(
            "LOGIN_REQUIRED",
            "Bạn cần đăng nhập để gửi báo cáo ô nhiễm.",
            ErrorType.Forbidden);

        /// <summary>BR-REP-017: Cannot delete verified/processed report.</summary>
        public static Error CannotDeleteReport => new(
            "CANNOT_DELETE_REPORT",
            "Báo cáo đã được xác nhận hoặc đang xử lý, không thể xóa.",
            ErrorType.BusinessRule);

        /// <summary>BR-REP-014: Missing before images on resolve.</summary>
        public static Error MissingBeforeImages => new(
            "MISSING_BEFORE_IMAGES",
            "Cần upload ít nhất 1 ảnh hiện trạng trước khi xử lý (before).",
            ErrorType.Validation);

        /// <summary>BR-REP-015: Reopen window (7 days) expired.</summary>
        public static Error ReopenWindowExpired => new(
            "REOPEN_WINDOW_EXPIRED",
            "Đã quá 7 ngày kể từ khi báo cáo được giải quyết. Không thể mở lại.",
            ErrorType.BusinessRule);

        /// <summary>BR-REP-018: Already submitted a rating for this report.</summary>
        public static Error AlreadyRated => new(
            "ALREADY_RATED",
            "Bạn đã đánh giá báo cáo này rồi.",
            ErrorType.Conflict);

        /// <summary>BR-REP-019: Draft limit reached (max 3).</summary>
        public static Error DraftLimitReached => new(
            "DRAFT_LIMIT_REACHED",
            "Bạn đã đạt giới hạn 3 bản nháp. Xóa bớt hoặc gửi đi.",
            ErrorType.BusinessRule);

        /// <summary>BR-REP-019: Draft not found or not owned by user.</summary>
        public static Error DraftNotFound => new(
            "DRAFT_NOT_FOUND",
            "Không tìm thấy bản nháp.",
            ErrorType.NotFound);

        /// <summary>BR-REP-032: Primary (target) report of a merge not found.</summary>
        public static Error PrimaryReportNotFound => new(
            "PRIMARY_REPORT_NOT_FOUND",
            "Không tìm thấy báo cáo gốc để gộp.",
            ErrorType.NotFound);

        /// <summary>BR-REP-031: Report is not flagged as a possible duplicate.</summary>
        public static Error NotPossibleDuplicate => new(
            "NOT_POSSIBLE_DUPLICATE",
            "Báo cáo không ở trạng thái nghi ngờ trùng lặp.",
            ErrorType.BusinessRule);

        /// <summary>BR-REP-032: A report cannot be marked as a duplicate of itself.</summary>
        public static Error CannotMergeIntoSelf => new(
            "CANNOT_MERGE_INTO_SELF",
            "Không thể gộp một báo cáo vào chính nó.",
            ErrorType.Validation);

        /// <summary>BR-REP-033: Users cannot flag their own report.</summary>
        public static Error CannotFlagOwnReport => new(
            "CANNOT_FLAG_OWN_REPORT",
            "Bạn không thể gắn cờ báo cáo của chính mình.",
            ErrorType.BusinessRule);

        /// <summary>BR-REP-033: The user already flagged this report with the same type.</summary>
        public static Error AlreadyFlagged => new(
            "ALREADY_FLAGGED",
            "Bạn đã gắn cờ báo cáo này rồi.",
            ErrorType.Conflict);

        /// <summary>BR-REP-004: Description shorter than 10 characters when provided.</summary>
        public static Error DescriptionTooShort => new(
            "DESCRIPTION_TOO_SHORT",
            "Mô tả phải từ 10–1000 ký tự.",
            ErrorType.Validation);

        /// <summary>BR-REP-004: Description contains blocked words.</summary>
        public static Error InappropriateDescription => new(
            "INAPPROPRIATE_CONTENT",
            "Mô tả chứa nội dung không phù hợp.",
            ErrorType.Validation);

        /// <summary>BR-REP-010: Citizen exceeded 5/h or 20/24h submit quota.</summary>
        public static Error RateLimitExceeded(int retryAfterMinutes) => new(
            "RATE_LIMIT_EXCEEDED",
            $"Bạn đã đạt giới hạn gửi báo cáo. Thử lại sau {retryAfterMinutes} phút.",
            ErrorType.RateLimited);
    }
}
