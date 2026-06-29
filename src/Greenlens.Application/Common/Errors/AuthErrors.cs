using Greenlens.Domain.Common;

namespace Greenlens.Application.Common;

public static partial class Errors
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

        /// <summary>BR-AUTH-015: User bị ban bởi Admin.</summary>
        public static Error AccountBanned => new(
            "ACCOUNT_BANNED",
            "Tài khoản của bạn đã bị cấm. Liên hệ Admin.",
            ErrorType.Forbidden);

        /// <summary>BR-AUTH-015: User đã soft-deleted.</summary>
        public static Error AccountDeactivated => new(
            "ACCOUNT_DEACTIVATED",
            "Tài khoản của bạn đã bị vô hiệu hóa.",
            ErrorType.Forbidden);

        /// <summary>BR-AUTH-015: Company hết hạn hợp đồng.</summary>
        public static Error CompanyExpired => new(
            "COMPANY_EXPIRED",
            "Công ty của bạn đã hết hạn hợp đồng. Liên hệ DEO.",
            ErrorType.Forbidden);

        /// <summary>BR-AUTH-020: Mật khẩu mới trùng 3 MK gần nhất.</summary>
        public static Error PasswordRecentlyUsed => new(
            "PASSWORD_RECENTLY_USED",
            "Mật khẩu mới không được trùng với 3 mật khẩu đã sử dụng gần nhất.",
            ErrorType.Validation);

        /// <summary>BR-AUTH-009: Không được phép gán role này.</summary>
        public static Error RoleAssignmentNotAllowed => new(
            "ROLE_ASSIGNMENT_NOT_ALLOWED",
            "Bạn không có quyền gán vai trò này.",
            ErrorType.Forbidden);
    }
}
