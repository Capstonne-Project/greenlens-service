using FluentValidation;

namespace Greenlens.Application.Features.CommunityCleanup.RejectCommunityVerification;

public sealed class RejectCommunityVerificationCommandValidator : AbstractValidator<RejectCommunityVerificationCommand>
{
    public RejectCommunityVerificationCommandValidator()
    {
        RuleFor(x => x.EventId).NotEmpty();
        RuleFor(x => x.Reason).MinimumLength(20).WithMessage("Lý do phải có ít nhất 20 ký tự.");
    }
}
