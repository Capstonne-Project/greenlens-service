using Greenlens.Domain.Common;

namespace Greenlens.Application.Common;

public static partial class Errors
{
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
}
