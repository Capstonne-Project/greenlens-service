using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Comments.HideComment;

/// <summary>BR-CMT-004: LEO/DEO/Admin hides a comment with reason.</summary>
public sealed class HideCommentCommandHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<HideCommentCommandHandler> logger)
    : IRequestHandler<HideCommentCommand, Result>
{
    public async Task<Result> Handle(HideCommentCommand request, CancellationToken ct)
    {
        logger.LogInformation("Getting hide comment");

        var comment = await db.Set<Comment>()
            .FirstOrDefaultAsync(c => c.Id == request.CommentId, ct)
            .ConfigureAwait(false);
        if (comment is null)
        {
            logger.LogWarning("Comment not found for comment {CommentId}", request.CommentId);
            return Errors.Comments.CommentNotFound;
        }

        if (comment.IsHidden)
        {
            logger.LogWarning("Comment {CommentId} already hidden", request.CommentId);
            return Errors.Comments.AlreadyHidden;
        }

        try
        {
            comment.Hide(currentUser.UserId, request.Reason);
        }
        catch (DomainException ex)
        {
            logger.LogWarning("Domain validation error for comment {CommentId}: {Message}", request.CommentId, ex.Message);
            return Errors.Comments.DomainValidation(ex.Message);
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Comment {CommentId} hidden by {UserId}", comment.Id, currentUser.UserId);
        return Result.Success();
    }
}
