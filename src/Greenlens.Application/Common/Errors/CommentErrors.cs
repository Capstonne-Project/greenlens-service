using Greenlens.Domain.Common;

namespace Greenlens.Application.Common;

public static partial class Errors
{
    public static class Comments
    {
        public static Error LoginRequired => new(
            "LOGIN_REQUIRED",
            "Vui lòng đăng nhập để bình luận.",
            ErrorType.Forbidden);

        public static Error CommentNotFound => new(
            "COMMENT_NOT_FOUND",
            "Không tìm thấy bình luận.",
            ErrorType.NotFound);

        public static Error CommentNotAllowed => new(
            "COMMENT_NOT_ALLOWED",
            "Báo cáo ẩn danh chỉ cho phép đội xử lý, LEO/DEO/Admin và người gửi gốc bình luận.",
            ErrorType.Forbidden);

        public static Error CommentBanned => new(
            "COMMENT_BANNED",
            "Tài khoản tạm khóa bình luận do vi phạm nội dung. Vui lòng thử lại sau.",
            ErrorType.BusinessRule);

        public static Error InappropriateContent => new(
            "INAPPROPRIATE_CONTENT",
            "Bình luận chứa nội dung không phù hợp.",
            ErrorType.BusinessRule);

        public static Error EditWindowExpired => new(
            "EDIT_WINDOW_EXPIRED",
            "Đã quá thời gian chỉnh sửa (15 phút).",
            ErrorType.BusinessRule);

        public static Error NotCommentAuthor => new(
            "NOT_COMMENT_AUTHOR",
            "Chỉ người viết bình luận mới được thực hiện hành động này.",
            ErrorType.Forbidden);

        public static Error TooManyImages => new(
            "TOO_MANY_IMAGES",
            "Bình luận chỉ được đính kèm tối đa 2 ảnh.",
            ErrorType.Validation);

        public static Error CommentImageTooLarge => new(
            "COMMENT_IMAGE_TOO_LARGE",
            "Ảnh đính kèm quá lớn. Kích thước tối đa là 5MB/ảnh.",
            ErrorType.Validation);

        public static Error AlreadyHidden => new(
            "ALREADY_HIDDEN",
            "Bình luận đã bị ẩn.",
            ErrorType.BusinessRule);
    }
}
