using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Comments.EditComment;

/// <summary>BR-CMT-004: author edits comment within 15 minutes.</summary>
public sealed class EditCommentCommandHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IProfanityFilter profanityFilter,
    IUserRepository users,
    IUnitOfWork uow,
    ILogger<EditCommentCommandHandler> logger)
    : IRequestHandler<EditCommentCommand, Result<EditCommentResponse>>
{
    public async Task<Result<EditCommentResponse>> Handle(EditCommentCommand request, CancellationToken ct)
    {
        logger.LogInformation("Getting edit comment");

        if (!currentUser.IsAuthenticated)
        {
            logger.LogWarning("Edit comment attempt by unauthenticated user");
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

        if (comment.AuthorId != currentUser.UserId)
        {
            logger.LogWarning("Comment {CommentId} is not author by {UserId}", request.CommentId, currentUser.UserId);
            return Errors.Comments.NotCommentAuthor;
        }

        if (profanityFilter.ContainsProfanity(request.Content))
        {
            var user = await users.GetByIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
            if (user is not null)
            {
                user.RecordCommentViolation();
                await uow.SaveChangesAsync(ct).ConfigureAwait(false);
                logger.LogWarning("Inappropriate content detected for user {UserId}", currentUser.UserId);
            }

            return Errors.Comments.InappropriateContent;
        }

        try
        {
            comment.Edit(request.Content, currentUser.UserId);
        }
        catch (DomainException)
        {
            logger.LogWarning("Edit window expired for comment {CommentId}", request.CommentId);
            return Errors.Comments.EditWindowExpired;
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Comment {CommentId} edited by {UserId}", comment.Id, currentUser.UserId);

        return new EditCommentResponse(
            comment.Id, comment.Content, comment.UpdatedAt, comment.IsWithinEditWindow());
    }
}
