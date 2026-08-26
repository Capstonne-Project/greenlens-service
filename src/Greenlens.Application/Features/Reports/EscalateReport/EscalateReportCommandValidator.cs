using FluentValidation;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;

namespace Greenlens.Application.Features.Reports.EscalateReport;

public sealed class EscalateReportCommandValidator : AbstractValidator<EscalateReportCommand>
{
    public EscalateReportCommandValidator(ISystemSettingsProvider systemSettings)
    {
        var (_, _, escalationMin) = ModuleSystemSettings.ValidationReasonLengths(systemSettings);

        RuleFor(x => x.ReportId).NotEmpty();
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Lý do escalate không được để trống.")
            .MinimumLength(escalationMin).WithMessage($"Lý do escalate phải ít nhất {escalationMin} ký tự.");
    }
}
