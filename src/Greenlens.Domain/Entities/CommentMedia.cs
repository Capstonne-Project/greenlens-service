using Greenlens.Domain.Common;

namespace Greenlens.Domain.Entities;

/// <summary>Image attached to a report comment. Max 2 per comment (BR-CMT-002).</summary>
public sealed class CommentMedia : AuditableEntity
{
    private CommentMedia() { }

    public Guid CommentId { get; private set; }
    public string Url { get; private set; } = default!;
    public string MimeType { get; private set; } = default!;
    public long SizeBytes { get; private set; }

    public Comment Comment { get; private set; } = default!;

    public static CommentMedia Create(Guid commentId, string url, string mimeType, long sizeBytes)
    {
        return new CommentMedia
        {
            CommentId = commentId,
            Url = url,
            MimeType = mimeType,
            SizeBytes = sizeBytes,
            CreatedAt = DateTime.UtcNow
        };
    }
}
