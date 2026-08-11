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
            "Ảnh vượt quá giới hạn kích thước của mục đích upload.",
            ErrorType.Validation);

        // ── Video errors (BR-REP-002) ──

        public static Error InvalidVideoType => new(
            "INVALID_VIDEO_TYPE",
            "Loại video không hợp lệ. Chỉ chấp nhận mp4, mov.",
            ErrorType.Validation);

        public static Error VideoTooLarge => new(
            "VIDEO_TOO_LARGE",
            "Video quá lớn. Kích thước tối đa là 100MB.",
            ErrorType.Validation);

        public static Error VideoDurationExceeded => new(
            "VIDEO_DURATION_EXCEEDED",
            "Video quá dài. Thời lượng tối đa là 60 giây.",
            ErrorType.Validation);

        public static Error VideoTranscodeFailed => new(
            "VIDEO_TRANSCODE_FAILED",
            "Không thể xử lý video. Vui lòng thử lại với file khác.",
            ErrorType.Unexpected);

        public static Error InvalidUploadPurpose => new(
            "INVALID_UPLOAD_PURPOSE",
            "Mục đích upload không hợp lệ.",
            ErrorType.Validation);

        public static Error UploadPurposeForbidden => new(
            "UPLOAD_PURPOSE_FORBIDDEN",
            "Vai trò hiện tại không được phép upload loại media này.",
            ErrorType.Forbidden);

        public static Error InvalidStorageUrl => new(
            "INVALID_STORAGE_URL",
            "URL ảnh không thuộc kho lưu trữ của hệ thống.",
            ErrorType.Validation);

        public static Error InvalidFileName => new(
            "INVALID_FILE_NAME",
            "Tên file không hợp lệ.",
            ErrorType.Validation);

        public static Error TooManyImages => new(
            "TOO_MANY_IMAGES",
            "Số lượng ảnh vượt quá giới hạn cho phép.",
            ErrorType.Validation);

        public static Error UploadNotFound => new(
            "UPLOAD_NOT_FOUND",
            "Không tìm thấy file đã upload trên kho lưu trữ.",
            ErrorType.NotFound);

        public static Error UploadMetadataMismatch => new(
            "UPLOAD_METADATA_MISMATCH",
            "Metadata file không khớp với object đã upload.",
            ErrorType.Validation);
    }

    public static class Catalog
    {
        public static Error ProvinceNotFound => new(
            "PROVINCE_NOT_FOUND",
            "Mã tỉnh/thành không hợp lệ hoặc không tồn tại.",
            ErrorType.NotFound);

        public static Error WardNotFound => new(
            "WARD_NOT_FOUND",
            "Mã phường/xã không hợp lệ hoặc không tồn tại.",
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
