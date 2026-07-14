using Greenlens.Domain.Common;

namespace Greenlens.Application.Common;

public static partial class Errors
{
    public static class BlockedWords
    {
        public static Error NotFound => new(
            "BLOCKED_WORD_NOT_FOUND",
            "Không tìm thấy từ bị chặn.",
            ErrorType.NotFound);

        public static Error Duplicate => new(
            "BLOCKED_WORD_DUPLICATE",
            "Từ hoặc cụm từ này đã có trong danh sách.",
            ErrorType.Conflict);
    }
}
