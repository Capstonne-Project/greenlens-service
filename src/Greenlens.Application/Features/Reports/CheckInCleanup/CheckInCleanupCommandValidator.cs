using FluentValidation;

namespace Greenlens.Application.Features.Reports.CheckInCleanup;

public sealed class CheckInCleanupCommandValidator : AbstractValidator<CheckInCleanupCommand>
{
    public CheckInCleanupCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();
        RuleFor(x => x.TeamId).NotEmpty();

        // BR-REP-003: Vietnam GPS bounds
        RuleFor(x => x.Latitude)
            .InclusiveBetween(8.0m, 24.0m)
            .WithMessage("Latitude phải trong khoảng 8.0–24.0 (Việt Nam).");
        RuleFor(x => x.Longitude)
            .InclusiveBetween(102.0m, 110.0m)
            .WithMessage("Longitude phải trong khoảng 102.0–110.0 (Việt Nam).");
    }
}
