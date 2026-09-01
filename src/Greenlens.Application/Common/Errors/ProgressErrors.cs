using Greenlens.Domain.Common;

namespace Greenlens.Application.Common;

public static partial class Errors
{
    /// <summary>Shared errors for progress-update flows (cleanup assignment, inspection, community cleanup).</summary>
    public static class Progress
    {
        /// <summary>Submitted GPS location is farther than the allowed progress-update distance from the site.</summary>
        public static Error TooFarFromSite(double distanceMeters) => new(
            "PROGRESS_TOO_FAR",
            $"Vị trí của bạn cách vị trí yêu cầu {distanceMeters:F0}m, quá xa để cập nhật tiến độ.",
            ErrorType.BusinessRule);

        /// <summary>EXIF GPS in a progress photo is farther than the allowed distance from the report site.</summary>
        public static Error PhotoTooFarFromSite(double distanceMeters) => new(
            "PROGRESS_PHOTO_TOO_FAR",
            $"Vị trí chụp ảnh cách vị trí điểm rác khoảng {distanceMeters:F0}m. " +
            "Vui lòng chụp lại ảnh tại đúng vị trí để nộp tiến độ.",
            ErrorType.BusinessRule);
    }
}
