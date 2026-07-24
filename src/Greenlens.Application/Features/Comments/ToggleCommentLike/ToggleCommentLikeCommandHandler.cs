using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Comments.ToggleCommentLike;

public sealed class ToggleCommentLikeCommandHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<ToggleCommentLikeCommandHandler> logger)
    : IRequestHandler<ToggleCommentLikeCommand, Result<ToggleCommentLikeResponse>>
{
    public async Task<Result<ToggleCommentLikeResponse>> Handle(
        ToggleCommentLikeCommand request,
        CancellationToken ct)
    {
        logger.LogInformation("Getting toggle comment like");

        if (!currentUser.IsAuthenticated)
        {
            logger.LogWarning("Toggle comment like attempt by unauthenticated user");
            return Errors.Comments.LoginRequired;
        }

        var commentExists = await db.Set<Comment>()
            .AnyAsync(c => c.Id == request.CommentId, ct)
            .ConfigureAwait(false);

        if (!commentExists)
        {
            logger.LogWarning("Comment not found for comment {CommentId}", request.CommentId);
            return Errors.Comments.CommentNotFound;
        }
        var existing = await db.Set<CommentLike>()
            .FirstOrDefaultAsync(
                l => l.CommentId == request.CommentId && l.UserId == currentUser.UserId,
                ct)
            .ConfigureAwait(false);

        bool liked;
        if (existing is null)
        {
            logger.LogWarning("Comment like not found for comment {CommentId} and user {UserId}", request.CommentId, currentUser.UserId);
            db.Set<CommentLike>().Add(CommentLike.Create(request.CommentId, currentUser.UserId));
            liked = true;
        }
        else
        {
            logger.LogWarning("Comment like found for comment {CommentId} and user {UserId}", request.CommentId, currentUser.UserId);
            db.Set<CommentLike>().Remove(existing);
            liked = false;
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        var likeCount = await db.Set<CommentLike>()
            .CountAsync(l => l.CommentId == request.CommentId, ct)
            .ConfigureAwait(false);

        logger.LogInformation("Comment {CommentId} liked by {UserId}: {Liked}, like count: {LikeCount}", request.CommentId, currentUser.UserId, liked, likeCount);

        return new ToggleCommentLikeResponse(request.CommentId, liked, likeCount);
    }
}
