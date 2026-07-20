using FluentValidation;

namespace Greenlens.Application.Features.Reports.UpdateCleanupProgress;

public sealed class UpdateCleanupProgressCommandValidator : AbstractValidator<UpdateCleanupProgressCommand>
{
    public UpdateCleanupProgressCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();
        RuleFor(x => x.TeamId).NotEmpty();
        RuleFor(x => x.Percent).InclusiveBetween(0, 100);
    }
}
