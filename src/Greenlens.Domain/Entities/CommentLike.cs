using Greenlens.Domain.Common;

namespace Greenlens.Domain.Entities;

/// <summary>One like per user per comment (TikTok-style).</summary>
public sealed class CommentLike : BaseEntity
{
    private CommentLike() { }

    public Guid CommentId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Comment Comment { get; private set; } = default!;
    public User User { get; private set; } = default!;

    public static CommentLike Create(Guid commentId, Guid userId)
    {
        return new CommentLike
        {
            CommentId = commentId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };
    }
}
