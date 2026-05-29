using Greenlens.Domain.Common;

namespace Greenlens.Application.Common;

public static partial class Errors
{
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

    public static class Ai
    {
        // <summary>AI Service timeout or down. BR-AI-006.</summary>
        public static Error ServiceUnavailable => new(
            "AI_SERVICE_UNAVAILABLE",
            "Dịch vụ phân tích ảnh tạm thời không khả dụng. Vui lòng thử lại sau.",
            ErrorType.Unexpected);

        // <summary>temp_image_id not found or expired (> 15 min).</summary>
        public static Error TempImageNotFound => new(
            "TEMP_IMAGE_NOT_FOUND",
            "Phiên upload ảnh không tồn tại hoặc đã hết hạn (15 phút). Vui lòng upload lại.",
            ErrorType.Validation);

        // <summary>AI decided image is irrelevant/abusive — block submit. BR flow doc.</summary>
        public static Error ImageRejectedByAi => new(
            "IMAGE_REJECTED_BY_AI",
            "Ảnh không phù hợp hoặc bị nghi ngờ spam. Vui lòng dùng ảnh khác.",
            ErrorType.BusinessRule);
    }
}
