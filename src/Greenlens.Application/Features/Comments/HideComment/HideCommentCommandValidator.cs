using FluentValidation;

namespace Greenlens.Application.Features.Comments.HideComment;

public sealed class HideCommentCommandValidator : AbstractValidator<HideCommentCommand>
{
    public HideCommentCommandValidator()
    {
        RuleFor(x => x.CommentId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MinimumLength(10).MaximumLength(500);
    }
}
