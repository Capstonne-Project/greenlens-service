using FluentValidation;

namespace Greenlens.Application.Features.Admin.BlockedWords.UpdateBlockedWord;

public sealed class UpdateBlockedWordCommandValidator : AbstractValidator<UpdateBlockedWordCommand>
{
    public UpdateBlockedWordCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Word)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(100);
        RuleFor(x => x.Note).MaximumLength(500);
    }
}
