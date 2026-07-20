using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Comments.ToggleCommentLike;

public sealed class ToggleCommentLikeCommandHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IUnitOfWork uow)
    : IRequestHandler<ToggleCommentLikeCommand, Result<ToggleCommentLikeResponse>>
{
    public async Task<Result<ToggleCommentLikeResponse>> Handle(
        ToggleCommentLikeCommand request,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Errors.Comments.LoginRequired;

        var commentExists = await db.Set<Comment>()
            .AnyAsync(c => c.Id == request.CommentId, ct)
            .ConfigureAwait(false);

        if (!commentExists)
            return Errors.Comments.CommentNotFound;

        var existing = await db.Set<CommentLike>()
            .FirstOrDefaultAsync(
                l => l.CommentId == request.CommentId && l.UserId == currentUser.UserId,
                ct)
            .ConfigureAwait(false);

        bool liked;
        if (existing is null)
        {
            db.Set<CommentLike>().Add(CommentLike.Create(request.CommentId, currentUser.UserId));
            liked = true;
        }
        else
        {
            db.Set<CommentLike>().Remove(existing);
            liked = false;
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        var likeCount = await db.Set<CommentLike>()
            .CountAsync(l => l.CommentId == request.CommentId, ct)
            .ConfigureAwait(false);

        return new ToggleCommentLikeResponse(request.CommentId, liked, likeCount);
    }
}
