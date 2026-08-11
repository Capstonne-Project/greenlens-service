using FluentValidation;

namespace Greenlens.Application.Features.Reports.ReassignCompanyTeam;

public sealed class ReassignCompanyTeamCommandValidator : AbstractValidator<ReassignCompanyTeamCommand>
{
    public ReassignCompanyTeamCommandValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .MinimumLength(20)
            .WithMessage("Reason must be at least 20 characters.");
    }
}
