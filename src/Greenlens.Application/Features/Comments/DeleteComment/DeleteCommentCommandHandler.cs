using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Comments.DeleteComment;

/// <summary>BR-CMT-004: author soft-deletes comment within 15 minutes.</summary>
public sealed class DeleteCommentCommandHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<DeleteCommentCommandHandler> logger)
    : IRequestHandler<DeleteCommentCommand, Result>
{
    public async Task<Result> Handle(DeleteCommentCommand request, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Errors.Comments.LoginRequired;

        var comment = await db.Set<Comment>()
            .FirstOrDefaultAsync(c => c.Id == request.CommentId, ct)
            .ConfigureAwait(false);
        if (comment is null)
            return Errors.Comments.CommentNotFound;

        try
        {
            comment.DeleteByAuthor(currentUser.UserId);
        }
        catch (DomainException ex) when (ex.Message.Contains("author", StringComparison.OrdinalIgnoreCase))
        {
            return Errors.Comments.NotCommentAuthor;
        }
        catch (DomainException)
        {
            return Errors.Comments.EditWindowExpired;
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Comment {CommentId} deleted by author {UserId}", comment.Id, currentUser.UserId);
        return Result.Success();
    }
}
