using FluentValidation;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;

namespace Greenlens.Application.Features.Reports.EscalateCleanup;

public sealed class EscalateCleanupCommandValidator : AbstractValidator<EscalateCleanupCommand>
{
    public EscalateCleanupCommandValidator(ISystemSettingsProvider systemSettings)
    {
        var (_, _, escalationMin) = ModuleSystemSettings.ValidationReasonLengths(systemSettings);

        RuleFor(x => x.ReportId).NotEmpty();
        RuleFor(x => x.TeamId).NotEmpty();
        RuleFor(x => x.Reason)
            .NotEmpty()
            .MinimumLength(escalationMin)
            .WithMessage($"Lý do escalate phải có ít nhất {escalationMin} ký tự.");
    }
}
