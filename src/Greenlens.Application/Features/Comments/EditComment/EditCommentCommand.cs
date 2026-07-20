using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Comments.EditComment;

public sealed record EditCommentCommand(Guid CommentId, string Content) : IRequest<Result<EditCommentResponse>>;

public sealed record EditCommentResponse(
    Guid Id,
    string Content,
    DateTime? UpdatedAt,
    bool CanEdit);
