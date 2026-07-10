using FluentValidation;

namespace Greenlens.Application.Features.Reports.SaveDraft;

/// <summary>BR-REP-019: Payload required, max 50KB.</summary>
public sealed class SaveDraftCommandValidator : AbstractValidator<SaveDraftCommand>
{
    public SaveDraftCommandValidator()
    {
        RuleFor(x => x.Payload)
            .NotEmpty().WithMessage("Nội dung nháp không được để trống.")
            .MaximumLength(51_200).WithMessage("Nội dung nháp vượt quá 50KB.");
    }
}
