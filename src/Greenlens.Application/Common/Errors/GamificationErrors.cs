using Greenlens.Domain.Common;

namespace Greenlens.Application.Common;

public static partial class Errors
{
    public static class Gamification
    {
        public static Error UserPointsNotFound => new(
            "GAMIFICATION_NOT_FOUND",
            "Không tìm thấy dữ liệu gamification cho người dùng này.",
            ErrorType.NotFound);

        public static Error GamificationLocked => new(
            "GAMIFICATION_LOCKED",
            "Tài khoản của bạn đang bị điều tra vì hoạt động bất thường.",
            ErrorType.BusinessRule);

        public static Error AlreadyLocked => new(
            "GAMIFICATION_ALREADY_LOCKED",
            "Gamification của người dùng này đã bị khóa.",
            ErrorType.Conflict);

        public static Error BadgeNotOwned => new(
            "BADGE_NOT_OWNED",
            "Bạn chưa đạt được huy hiệu này nên không thể chọn để hiển thị.",
            ErrorType.BusinessRule);
    }
}
