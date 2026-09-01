using Greenlens.Domain.Common;

namespace Greenlens.Application.Common;

public static partial class Errors
{
    /// <summary>Shared errors for progress-update flows (cleanup assignment, inspection, community cleanup).</summary>
    public static class Progress
    {
        /// <summary>GPS (body or photo EXIF) is farther than the allowed progress-update distance from the site.</summary>
        public static Error TooFarFromSite(double distanceMeters) => new(
            "PROGRESS_TOO_FAR",
            $"Vị trí chụp ảnh cách vị trí điểm rác khoảng {GeoDistanceFormatting.Format(distanceMeters)}. " +
            "Vui lòng chụp lại ảnh tại đúng vị trí để nộp tiến độ.",
            ErrorType.BusinessRule);

        /// <summary>Progress photo attached but no GPS in request body or image EXIF.</summary>
        public static Error LocationRequired => new(
            "PROGRESS_LOCATION_REQUIRED",
            "Không xác định được vị trí ảnh tiến độ. Vui lòng bật GPS khi chụp hoặc dùng ảnh có thông tin vị trí.",
            ErrorType.Validation);
    }
}
