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
}
