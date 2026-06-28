using Greenlens.Domain.Common;

namespace Greenlens.Application.Common;

public static partial class Errors
{
    public static class Notification
    {
        public static Error NotFound(Guid id) =>
            new($"NOTIFICATION_NOT_FOUND", $"Không tìm thấy thông báo {id}.", ErrorType.NotFound);

        public static readonly Error NotOwner =
            new("NOTIFICATION_NOT_OWNER", "Bạn không có quyền truy cập thông báo này.", ErrorType.Forbidden);
    }
}
