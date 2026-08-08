using FluentValidation;

namespace Greenlens.Application.Features.CommunityCleanup.CheckInCommunityCleanup;

public sealed class CheckInCommunityCleanupCommandValidator : AbstractValidator<CheckInCommunityCleanupCommand>
{
    public CheckInCommunityCleanupCommandValidator()
    {
        RuleFor(x => x.EventId).NotEmpty();

        // BR-REP-003: Vietnam GPS bounds
        RuleFor(x => x.Latitude)
            .InclusiveBetween(8.0m, 24.0m)
            .WithMessage("Latitude phải trong khoảng 8.0–24.0 (Việt Nam).");
        RuleFor(x => x.Longitude)
            .InclusiveBetween(102.0m, 110.0m)
            .WithMessage("Longitude phải trong khoảng 102.0–110.0 (Việt Nam).");

        // Draft: out-of-range check-in override reason, when supplied, must be meaningful.
        RuleFor(x => x.Reason)
            .MinimumLength(20)
            .WithMessage("Lý do phải có ít nhất 20 ký tự.")
            .When(x => x.Reason is not null);
    }
}
