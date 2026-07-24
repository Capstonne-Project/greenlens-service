using Greenlens.Domain.Common;

namespace Greenlens.Application.Common;

public static partial class Errors
{
    public static class Inspections
    {
        public static Error InspectionNotFound => new(
            "INSPECTION_NOT_FOUND",
            "Không tìm thấy hồ sơ xử phạt.",
            ErrorType.NotFound);

        public static Error InvalidStatusTransition => new(
            "INSPECTION_INVALID_STATUS",
            "Không thể thực hiện hành động từ trạng thái hiện tại.",
            ErrorType.BusinessRule);

        public static Error ReportNotVerified => new(
            "REPORT_NOT_VERIFIED",
            "Báo cáo chưa được xác minh. Chỉ có thể lập hồ sơ xử phạt cho báo cáo đã Verified.",
            ErrorType.BusinessRule);

        public static Error TeamNotFound => new(
            "INSPECTION_TEAM_NOT_FOUND",
            "Không tìm thấy Inspection Team.",
            ErrorType.NotFound);

        public static Error TeamNotInspectionType => new(
            "TEAM_NOT_INSPECTION_TYPE",
            "Team phải thuộc loại Inspection.",
            ErrorType.BusinessRule);

        public static Error NotTeamLeader => new(
            "NOT_INSPECTION_TEAM_LEADER",
            "Chỉ Team Leader của Inspection Team được thực hiện hành động này.",
            ErrorType.BusinessRule);

        public static Error PenaltyAmountInvalid => new(
            "PENALTY_AMOUNT_INVALID",
            "Số tiền phạt phải lớn hơn 0.",
            ErrorType.Validation);

        public static Error PaymentAmountInvalid => new(
            "PAYMENT_AMOUNT_INVALID",
            "Số tiền nộp phạt phải lớn hơn 0.",
            ErrorType.Validation);

        public static Error ReasonTooShort => new(
            "CLOSE_REASON_TOO_SHORT",
            "Lý do đóng hồ sơ phải có ít nhất 50 ký tự (BR-INS-013).",
            ErrorType.Validation);

        public static Error RepeatOffenderDetected => new(
            "REPEAT_OFFENDER",
            "Cơ sở vi phạm ≥ 2 lần trong 12 tháng — cờ tái phạm (BR-INS-022). Mức phạt tối thiểu nâng 1 bậc.",
            ErrorType.BusinessRule);

        public static Error NotAssignedToYourTeam => new(
            "NOT_ASSIGNED_TO_YOUR_TEAM",
            "Hồ sơ xử phạt không được gán cho team của bạn.",
            ErrorType.Forbidden);

        public static Error InspectionAlreadyExistsForReport => new(
            "INSPECTION_ALREADY_EXISTS",
            "Báo cáo này đã có hồ sơ xử phạt đang hoạt động.",
            ErrorType.Conflict);

        public static Error TooFarFromSite => new(
            "INSPECTION_TOO_FAR",
            "Vị trí check-in cách hiện trường hơn 200m (BR-INS-004).",
            ErrorType.BusinessRule);

        public static Error CheckInRequired => new(
            "INSPECTION_CHECKIN_REQUIRED",
            "Phải check-in hiện trường trước khi thao tác (BR-INS-004).",
            ErrorType.BusinessRule);

        public static Error DeclineWindowExpired => new(
            "INSPECTION_DECLINE_EXPIRED",
            "Đã quá thời hạn 24 giờ để từ chối task (BR-INS-003).",
            ErrorType.BusinessRule);

        public static Error NoTeamAssigned => new(
            "INSPECTION_NO_TEAM",
            "Hồ sơ chưa được gán Inspection Team.",
            ErrorType.BusinessRule);

        public static Error ProgressUpdateRequired => new(
            "INSPECTION_PROGRESS_STALE",
            "Phải cập nhật tiến độ ít nhất 1 lần/ngày (BR-INS-031).",
            ErrorType.BusinessRule);

        public static Error ViolatingEntityNotFound => new(
            "VIOLATING_ENTITY_NOT_FOUND",
            "Không tìm thấy đối tượng vi phạm.",
            ErrorType.NotFound);

        public static Error ViolatingEntityDuplicateTaxCode => new(
            "VIOLATING_ENTITY_DUPLICATE_TAX_CODE",
            "Mã số thuế đã tồn tại trong hệ thống.",
            ErrorType.Conflict);

        public static Error ViolatingEntityDuplicateIdentityNumber => new(
            "VIOLATING_ENTITY_DUPLICATE_IDENTITY",
            "Số CMND/CCCD đã tồn tại trong hệ thống.",
            ErrorType.Conflict);

        public static Error ViolatingEntityAlreadyDeleted => new(
            "VIOLATING_ENTITY_ALREADY_DELETED",
            "Đối tượng vi phạm đã được xóa trước đó.",
            ErrorType.Conflict);

        public static Error ViolatingEntityInUse => new(
            "VIOLATING_ENTITY_IN_USE",
            "Không thể xóa đối tượng vi phạm đang có biên bản kiểm tra liên quan.",
            ErrorType.BusinessRule);

        public static Error EvidenceImagesRequired => new(
            "EVIDENCE_IMAGES_REQUIRED",
            "Vui lòng upload ít nhất 1 ảnh hiện trường.",
            ErrorType.Validation);

        /// <summary>BR-INS-010: ≥ 2 ảnh hiện trường required before issuing penalty.</summary>
        public static Error InsufficientEvidenceImages => new(
            "INSUFFICIENT_EVIDENCE_IMAGES",
            "Biên bản cần ít nhất 2 ảnh hiện trường trước khi ra quyết định xử phạt (BR-INS-010).",
            ErrorType.BusinessRule);

        public static Error PaymentNotFound => new(
            "PAYMENT_NOT_FOUND",
            "Không tìm thấy khoản thanh toán.",
            ErrorType.NotFound);

        public static Error PaymentAlreadyDeleted => new(
            "PAYMENT_ALREADY_DELETED",
            "Khoản thanh toán đã được xóa trước đó.",
            ErrorType.Conflict);
    }
}
