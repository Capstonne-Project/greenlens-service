using Greenlens.Domain.Common;

namespace Greenlens.Application.Common;

public static partial class Errors
{
    public static class Meta
    {
        public static Error ShareImageRequired => new(
            "META_PAGE_SHARE_IMAGE_REQUIRED",
            "Không có ảnh báo cáo để đăng lên Facebook Page. Báo cáo cần ít nhất một ảnh.",
            ErrorType.BusinessRule);

        public static Error FeatureDisabled => new(
            "META_PAGE_SHARE_DISABLED",
            "Chia sẻ lên Facebook Page chưa được bật trên hệ thống.",
            ErrorType.BusinessRule);

        public static Error NotConfigured => new(
            "META_PAGE_NOT_CONFIGURED",
            "Facebook Page chưa được cấu hình (PageId hoặc PageAccessToken).",
            ErrorType.Unexpected);

        public static Error PublishFailed(string reason) => new(
            "META_PAGE_PUBLISH_FAILED",
            reason,
            ErrorType.Unexpected);
    }
}
