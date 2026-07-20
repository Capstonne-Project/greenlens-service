using FluentValidation;

namespace Greenlens.Application.Features.Comments.EditComment;

public sealed class EditCommentCommandValidator : AbstractValidator<EditCommentCommand>
{
    public EditCommentCommandValidator()
    {
        RuleFor(x => x.CommentId).NotEmpty();
        RuleFor(x => x.Content).NotEmpty().MinimumLength(1).MaximumLength(500);
    }
}
