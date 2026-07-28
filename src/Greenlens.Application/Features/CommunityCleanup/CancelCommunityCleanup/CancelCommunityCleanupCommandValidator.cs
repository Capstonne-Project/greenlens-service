using FluentValidation;

namespace Greenlens.Application.Features.CommunityCleanup.CancelCommunityCleanup;

public sealed class CancelCommunityCleanupCommandValidator : AbstractValidator<CancelCommunityCleanupCommand>
{
    public CancelCommunityCleanupCommandValidator()
    {
        RuleFor(x => x.EventId).NotEmpty();
        RuleFor(x => x.Reason).MinimumLength(20).WithMessage("Lý do phải có ít nhất 20 ký tự.");
    }
}
