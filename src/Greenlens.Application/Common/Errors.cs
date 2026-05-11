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
    }
}
