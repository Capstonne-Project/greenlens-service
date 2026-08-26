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
    ISystemSettingsProvider systemSettings,
    ILogger<DeleteCommentCommandHandler> logger)
    : IRequestHandler<DeleteCommentCommand, Result>
{
    public async Task<Result> Handle(DeleteCommentCommand request, CancellationToken ct)
    {
        var editWindowMinutes = ModuleSystemSettings.CommentEditWindowMinutes(systemSettings);

        logger.LogInformation("Getting delete comment");

        if (!currentUser.IsAuthenticated)
        {
            logger.LogWarning("Delete comment attempt by unauthenticated user");
            return Errors.Comments.LoginRequired;
        }

        var comment = await db.Set<Comment>()
            .FirstOrDefaultAsync(c => c.Id == request.CommentId, ct)
            .ConfigureAwait(false);
        if (comment is null)
        {
            logger.LogWarning("Comment not found for comment {CommentId}", request.CommentId);
            return Errors.Comments.CommentNotFound;
        }

        if (comment.IsDeleted)
        {
            logger.LogWarning("Comment {CommentId} already deleted", request.CommentId);
            return Errors.Comments.CommentAlreadyDeleted;
        }

        try
        {
            comment.DeleteByAuthor(currentUser.UserId, editWindowMinutes);
        }
        catch (DomainException ex) when (ex.Message.Contains("author", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Comment {CommentId} is not author by {UserId}", request.CommentId, currentUser.UserId);
            return Errors.Comments.NotCommentAuthor;
        }
        catch (DomainException ex) when (ex.Message.Contains("already deleted", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Comment {CommentId} already deleted", request.CommentId);
            return Errors.Comments.CommentAlreadyDeleted;
        }
        catch (DomainException)
        {
            logger.LogWarning("Edit window expired for comment {CommentId}", request.CommentId);
            return Errors.Comments.EditWindowExpired;
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Comment {CommentId} deleted by author {UserId}", comment.Id, currentUser.UserId);
        return Result.Success();
    }
}
