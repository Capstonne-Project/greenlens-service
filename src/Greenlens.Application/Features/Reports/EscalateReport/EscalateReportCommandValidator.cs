using FluentValidation;

namespace Greenlens.Application.Features.Reports.EscalateReport;

public sealed class EscalateReportCommandValidator : AbstractValidator<EscalateReportCommand>
{
    public EscalateReportCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Lý do escalate không được để trống.")
            .MinimumLength(10).WithMessage("Lý do escalate phải ít nhất 10 ký tự.");
    }
}
