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
}
