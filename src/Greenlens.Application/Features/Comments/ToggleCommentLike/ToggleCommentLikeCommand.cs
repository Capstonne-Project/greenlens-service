using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Comments.ToggleCommentLike;

public sealed record ToggleCommentLikeCommand(Guid CommentId)
    : IRequest<Result<ToggleCommentLikeResponse>>;

public sealed record ToggleCommentLikeResponse(
    Guid CommentId,
    bool Liked,
    int LikeCount);
