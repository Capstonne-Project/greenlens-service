using Greenlens.Domain.Common;

namespace Greenlens.Application.Common;

public static partial class Errors
{
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

    public static class Ai
    {
        /// <summary>AI Service timeout or down. BR-AI-006.</summary>
        public static Error ServiceUnavailable => new(
            "AI_SERVICE_UNAVAILABLE",
            "Dịch vụ phân tích ảnh tạm thời không khả dụng. Vui lòng thử lại sau.",
            ErrorType.Unexpected);

        /// <summary>temp_image_id not found or expired (> 15 min).</summary>
        public static Error TempImageNotFound => new(
            "TEMP_IMAGE_NOT_FOUND",
            "Phiên upload ảnh không tồn tại hoặc đã hết hạn (15 phút). Vui lòng upload lại.",
            ErrorType.Validation);

        /// <summary>AI decided image is irrelevant/abusive — block submit. BR flow doc.</summary>
        public static Error ImageRejectedByAi => new(
            "IMAGE_REJECTED_BY_AI",
            "Ảnh không phù hợp hoặc bị nghi ngờ spam. Vui lòng dùng ảnh khác.",
            ErrorType.BusinessRule);
    }
}
