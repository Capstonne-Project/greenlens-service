using FluentValidation;

namespace Greenlens.Application.Features.Reports.EscalateCleanup;

public sealed class EscalateCleanupCommandValidator : AbstractValidator<EscalateCleanupCommand>
{
    private const int MinReasonLength = 20;

    public EscalateCleanupCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();
        RuleFor(x => x.TeamId).NotEmpty();
        RuleFor(x => x.Reason)
            .NotEmpty()
            .MinimumLength(MinReasonLength)
            .WithMessage($"Lý do escalate phải có ít nhất {MinReasonLength} ký tự.");
    }
}
