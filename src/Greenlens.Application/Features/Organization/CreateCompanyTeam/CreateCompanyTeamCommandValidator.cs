using FluentValidation;

namespace Greenlens.Application.Features.Organization.CreateCompanyTeam;

public sealed class CreateCompanyTeamCommandValidator : AbstractValidator<CreateCompanyTeamCommand>
{
    public CreateCompanyTeamCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên team là bắt buộc.")
            .MaximumLength(100);
    }
}
