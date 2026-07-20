using FluentValidation;

namespace Greenlens.Application.Features.Reports.EscalateCleanup;

public sealed class EscalateCleanupCommandValidator : AbstractValidator<EscalateCleanupCommand>
{
    public EscalateCleanupCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();
        RuleFor(x => x.TeamId).NotEmpty();
        RuleFor(x => x.Reason)
            .NotEmpty()
            .MinimumLength(20)
            .WithMessage("Lý do escalate phải có ít nhất 20 ký tự.");
    }
}
