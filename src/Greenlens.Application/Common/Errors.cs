using Greenlens.Domain.Common;

namespace Greenlens.Application.Common;

public static class Errors
{
    public static class Auth
    {
        public static Error InvalidCredentials => new(
            "INVALID_CREDENTIALS",
            "Email hoặc mật khẩu không đúng.",
            ErrorType.Validation);

        public static Error AccountLocked => new(
            "ACCOUNT_LOCKED",
            "Tài khoản đã bị khóa do đăng nhập sai quá nhiều lần. Vui lòng thử lại sau 30 phút.",
            ErrorType.BusinessRule);

        public static Error EmailNotVerified => new(
            "EMAIL_NOT_VERIFIED",
            "Email chưa được xác thực. Vui lòng kiểm tra hộp thư.",
            ErrorType.BusinessRule);

        public static Error EmailTaken => new(
            "EMAIL_TAKEN",
            "Email đã được sử dụng.",
            ErrorType.Conflict);

        public static Error OtpExpired => new(
            "OTP_EXPIRED",
            "Mã OTP đã hết hạn. Vui lòng yêu cầu mã mới.",
            ErrorType.Validation);

        public static Error OtpInvalid => new(
            "OTP_INVALID",
            "Mã OTP không đúng.",
            ErrorType.Validation);

        public static Error OtpMaxAttempts => new(
            "OTP_MAX_ATTEMPTS",
            "Đã vượt quá số lần nhập OTP cho phép. Vui lòng yêu cầu mã mới.",
            ErrorType.BusinessRule);

        public static Error WeakPassword => new(
            "WEAK_PASSWORD",
            "Mật khẩu không đủ mạnh. Cần ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.",
            ErrorType.Validation);

        public static Error InvalidRefreshToken => new(
            "INVALID_REFRESH_TOKEN",
            "Refresh token không hợp lệ hoặc đã hết hạn.",
            ErrorType.Validation);

        public static Error TokenExpired => new(
            "TOKEN_EXPIRED",
            "Token đã hết hạn.",
            ErrorType.Validation);

        public static Error UserNotFound => new(
            "NOT_FOUND",
            "Không tìm thấy người dùng.",
            ErrorType.NotFound);

        public static Error GoogleAuthFailed => new(
            "GOOGLE_AUTH_FAILED",
            "Xác thực Google không thành công.",
            ErrorType.Validation);

        public static Error IncorrectCurrentPassword => new(
            "INCORRECT_CURRENT_PASSWORD",
            "Mật khẩu hiện tại không đúng.",
            ErrorType.Validation);
    }

    public static class Users
    {
        public static Error UserNotFound => new(
            "USER_NOT_FOUND",
            "Không tìm thấy người dùng.",
            ErrorType.NotFound);

        public static Error CannotDeleteSelf => new(
            "CANNOT_DELETE_SELF",
            "Không thể xóa chính tài khoản của bạn.",
            ErrorType.BusinessRule);

        public static Error UserAlreadyDeleted => new(
            "USER_ALREADY_DELETED",
            "Người dùng đã bị xóa trước đó.",
            ErrorType.Conflict);

        public static Error InvalidFileType => new(
            "INVALID_FILE_TYPE",
            "Loại file không hợp lệ. Chỉ chấp nhận jpg, png, webp.",
            ErrorType.Validation);

        public static Error FileTooLarge => new(
            "FILE_TOO_LARGE",
            "File quá lớn. Kích thước tối đa là 5MB.",
            ErrorType.Validation);

        public static Error StorageUploadFailed => new(
            "STORAGE_UPLOAD_FAILED",
            "Không thể tải file lên máy chủ lưu trữ. Vui lòng thử lại sau.",
            ErrorType.Unexpected);
    }

    public static class Media
    {
        public static Error InvalidImageType => new(
            "INVALID_IMAGE_TYPE",
            "Loại ảnh không hợp lệ. Chỉ chấp nhận jpg, png, webp, heic.",
            ErrorType.Validation);

        public static Error ImageTooLarge => new(
            "IMAGE_TOO_LARGE",
            "Ảnh quá lớn. Kích thước tối đa là 10MB.",
            ErrorType.Validation);
    }

    public static class Catalog
    {
        public static Error ProvinceNotFound => new(
            "PROVINCE_NOT_FOUND",
            "Mã tỉnh/thành không hợp lệ hoặc không tồn tại.",
            ErrorType.NotFound);
    }

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
            "Team đã đạt giới hạn 10 báo cáo In-Progress. Vui lòng chọn team khác.",
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
    }

    public static class Map
    {
        public static Error InvalidBoundingBox => new(
            "INVALID_BOUNDING_BOX",
            "Khung nhìn map không hợp lệ (min/max hoặc nằm ngoài phạm vi Việt Nam).",
            ErrorType.Validation);

        public static Error BoundingBoxTooLarge => new(
            "BOUNDING_BOX_TOO_LARGE",
            "Khung nhìn quá rộng. Vui lòng zoom gần hơn.",
            ErrorType.Validation);

        public static Error InvalidMapMode => new(
            "INVALID_MAP_MODE",
            "mode phải là detail hoặc aggregate.",
            ErrorType.Validation);
    }

    public static class Phone
    {
        public static Error FirebaseTokenInvalid => new(
            "FIREBASE_TOKEN_INVALID",
            "Firebase token không hợp lệ hoặc đã hết hạn.",
            ErrorType.Validation);

        public static Error FirebasePhoneMissing => new(
            "FIREBASE_PHONE_MISSING",
            "Token Firebase không chứa thông tin số điện thoại.",
            ErrorType.Validation);

        public static Error PhoneAlreadyUsed => new(
            "PHONE_ALREADY_USED",
            "Số điện thoại này đã được sử dụng bởi tài khoản khác.",
            ErrorType.Conflict);
    }

    public static class Organization
    {
        public static Error DepartmentNotFound => new(
            "DEPARTMENT_NOT_FOUND",
            "Không tìm thấy đơn vị quản lý cấp tỉnh/thành phố.",
            ErrorType.NotFound);

        public static Error DepartmentAlreadyExists => new(
            "DEPARTMENT_ALREADY_EXISTS",
            "Tỉnh/thành phố này đã có đơn vị quản lý.",
            ErrorType.Conflict);

        public static Error LocalOfficeNotFound => new(
            "LOCAL_OFFICE_NOT_FOUND",
            "Không tìm thấy văn phòng cấp xã/phường.",
            ErrorType.NotFound);

        public static Error LocalOfficeAlreadyExists => new(
            "LOCAL_OFFICE_ALREADY_EXISTS",
            "Xã/phường này đã có văn phòng môi trường.",
            ErrorType.Conflict);

        public static Error TeamNotFound => new(
            "TEAM_NOT_FOUND",
            "Không tìm thấy đội môi trường.",
            ErrorType.NotFound);

        public static Error MemberAlreadyInTeam => new(
            "MEMBER_ALREADY_IN_TEAM",
            "Người dùng đã là thành viên của đội này.",
            ErrorType.Conflict);

        public static Error MemberNotInTeam => new(
            "MEMBER_NOT_IN_TEAM",
            "Người dùng không phải thành viên của đội này.",
            ErrorType.NotFound);

        public static Error InvalidRoleForOfficer => new(
            "INVALID_ROLE_FOR_OFFICER",
            "Người dùng phải có vai trò LEO để được gán cho văn phòng.",
            ErrorType.BusinessRule);

        public static Error InvalidRoleForTeamMember => new(
            "INVALID_ROLE_FOR_TEAM_MEMBER",
            "Người dùng phải có vai trò Cleanup hoặc Inspector để tham gia đội.",
            ErrorType.BusinessRule);

        public static Error WardNotFound => new(
            "WARD_NOT_FOUND",
            "Mã xã/phường không tồn tại.",
            ErrorType.NotFound);

        public static Error ProvinceNotFound => new(
            "PROVINCE_NOT_FOUND",
            "Mã tỉnh/thành phố không tồn tại.",
            ErrorType.NotFound);
    }
}
