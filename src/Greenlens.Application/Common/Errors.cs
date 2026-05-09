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
}
