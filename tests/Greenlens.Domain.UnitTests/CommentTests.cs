using Greenlens.Domain.Entities;
using Greenlens.Domain.Exceptions;

namespace Greenlens.Domain.UnitTests;

public sealed class CommentTests
{
    private static Comment CreateComment() =>
        Comment.Create(Guid.NewGuid(), Guid.NewGuid(), "Bình luận hợp lệ");

    [Fact]
    public void Create_ValidContent_Succeeds_BR_CMT_002()
    {
        var comment = CreateComment();

        Assert.Equal("Bình luận hợp lệ", comment.Content);
        Assert.False(comment.IsHidden);
        Assert.True(comment.IsWithinEditWindow());
    }

    [Fact]
    public void Create_EmptyContent_Throws_BR_CMT_002()
    {
        Assert.Throws<DomainException>(() =>
            Comment.Create(Guid.NewGuid(), Guid.NewGuid(), "   "));
    }

    [Fact]
    public void Edit_WithinWindow_UpdatesContent_BR_CMT_004()
    {
        var authorId = Guid.NewGuid();
        var comment = Comment.Create(Guid.NewGuid(), authorId, "Cũ");

        comment.Edit("Mới", authorId);

        Assert.Equal("Mới", comment.Content);
        Assert.NotNull(comment.UpdatedAt);
    }

    [Fact]
    public void Edit_AfterWindow_Throws_BR_CMT_004()
    {
        var authorId = Guid.NewGuid();
        var comment = Comment.Create(Guid.NewGuid(), authorId, "Cũ");
        typeof(Comment).GetProperty(nameof(Comment.CreatedAt))!
            .SetValue(comment, DateTime.UtcNow.AddMinutes(-16));

        Assert.Throws<DomainException>(() => comment.Edit("Mới", authorId));
    }

    [Fact]
    public void DeleteByAuthor_WithinWindow_SoftDeletes_BR_CMT_004()
    {
        var authorId = Guid.NewGuid();
        var comment = Comment.Create(Guid.NewGuid(), authorId, "Xóa tôi");

        comment.DeleteByAuthor(authorId);

        Assert.NotNull(comment.DeletedAt);
    }

    [Fact]
    public void Hide_WithReason_SetsHidden_BR_CMT_004()
    {
        var comment = CreateComment();
        var officerId = Guid.NewGuid();

        comment.Hide(officerId, "Nội dung spam hoặc xúc phạm");

        Assert.True(comment.IsHidden);
        Assert.Equal(officerId, comment.HiddenBy);
        Assert.NotNull(comment.HiddenAt);
    }

    [Fact]
    public void ReassignToReport_ChangesReportId_BR_REP_032()
    {
        var comment = CreateComment();
        var primaryId = Guid.NewGuid();

        comment.ReassignToReport(primaryId);

        Assert.Equal(primaryId, comment.ReportId);
    }
}
