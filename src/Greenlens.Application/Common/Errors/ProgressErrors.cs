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
    }
}
