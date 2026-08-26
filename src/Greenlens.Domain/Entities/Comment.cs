using Greenlens.Domain.Common;
using Greenlens.Domain.Exceptions;

namespace Greenlens.Domain.Entities;

/// <summary>
/// Citizen / staff comment on a pollution report. Optional parent = TikTok-style reply.
/// </summary>
/// <remarks>Implements: BR-CMT-001..004.</remarks>
public sealed class Comment : SoftDeletableEntity
{
    private Comment() { }

    public Guid ReportId { get; private set; }
    public Guid AuthorId { get; private set; }
    public string Content { get; private set; } = default!;
    public bool IsHidden { get; private set; }
    public string? HiddenReason { get; private set; }
    public Guid? HiddenBy { get; private set; }
    public DateTime? HiddenAt { get; private set; }

    /// <summary>Null = top-level comment. Set = reply to another comment on the same report.</summary>
    public Guid? ParentCommentId { get; private set; }

    public Report Report { get; private set; } = default!;
    public User Author { get; private set; } = default!;
    public Comment? ParentComment { get; private set; }
    public ICollection<Comment> Replies { get; private set; } = [];
    public ICollection<CommentMedia> Media { get; private set; } = [];
    public ICollection<CommentLike> Likes { get; private set; } = [];

    public static Comment Create(
        Guid reportId,
        Guid authorId,
        string content,
        Guid? parentCommentId = null)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new DomainException("Comment content is required.");

        var trimmed = content.Trim();
        if (trimmed.Length is < 1 or > 500)
            throw new DomainException("Comment must be between 1 and 500 characters.");

        return new Comment
        {
            ReportId = reportId,
            AuthorId = authorId,
            Content = trimmed,
            ParentCommentId = parentCommentId,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>BR-CMT-004: author may edit within configured window.</summary>
    public void Edit(string content, Guid editorId, int editWindowMinutes = 15)
    {
        if (AuthorId != editorId)
            throw new DomainException("Only the author can edit this comment.");

        if (IsHidden)
            throw new DomainException("Hidden comments cannot be edited.");

        if (DateTime.UtcNow - CreatedAt > TimeSpan.FromMinutes(editWindowMinutes))
            throw new DomainException("Edit window has expired.");

        var trimmed = content.Trim();
        if (trimmed.Length is < 1 or > 500)
            throw new DomainException("Comment must be between 1 and 500 characters.");

        Content = trimmed;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>BR-CMT-004: author may soft-delete within configured window.</summary>
    public void DeleteByAuthor(Guid authorId, int editWindowMinutes = 15)
    {
        if (IsDeleted)
            throw new DomainException("Comment already deleted.");

        if (AuthorId != authorId)
            throw new DomainException("Only the author can delete this comment.");

        if (DateTime.UtcNow - CreatedAt > TimeSpan.FromMinutes(editWindowMinutes))
            throw new DomainException("Delete window has expired.");

        SoftDelete(authorId.ToString());
    }

    /// <summary>BR-CMT-004: LEO/Admin may hide a comment at any time.</summary>
    public void Hide(Guid officerId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 10)
            throw new DomainException("Hide reason must be at least 10 characters.");

        IsHidden = true;
        HiddenReason = reason.Trim();
        HiddenBy = officerId;
        HiddenAt = DateTime.UtcNow;
    }

    /// <summary>BR-REP-032: move comments to primary report when merging duplicates.</summary>
    public void ReassignToReport(Guid primaryReportId)
    {
        if (primaryReportId == Guid.Empty)
            throw new ArgumentException("Primary report id is required.", nameof(primaryReportId));

        ReportId = primaryReportId;
    }

    public bool IsWithinEditWindow(int editWindowMinutes = 15) =>
        DateTime.UtcNow - CreatedAt <= TimeSpan.FromMinutes(editWindowMinutes);
}
