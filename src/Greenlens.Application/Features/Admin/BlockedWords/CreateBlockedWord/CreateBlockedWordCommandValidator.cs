using FluentValidation;

namespace Greenlens.Application.Features.Admin.BlockedWords.CreateBlockedWord;

public sealed class CreateBlockedWordCommandValidator : AbstractValidator<CreateBlockedWordCommand>
{
    public CreateBlockedWordCommandValidator()
    {
        RuleFor(x => x.Word)
            .NotEmpty()
            .MinimumLength(2).WithMessage("Từ/cụm từ phải có ít nhất 2 ký tự.")
            .MaximumLength(100);

        RuleFor(x => x.Note).MaximumLength(500);
    }
}
