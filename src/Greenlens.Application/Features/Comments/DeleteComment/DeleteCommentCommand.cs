using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Comments.DeleteComment;

public sealed record DeleteCommentCommand(Guid CommentId) : IRequest<Result>;
