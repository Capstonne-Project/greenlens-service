using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Comments.HideComment;

public sealed record HideCommentCommand(Guid CommentId, string Reason) : IRequest<Result>;
