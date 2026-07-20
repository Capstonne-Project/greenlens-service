using FluentValidation;

namespace Greenlens.Application.Features.Organization.TerminateCompany;

public sealed class TerminateCompanyCommandValidator : AbstractValidator<TerminateCompanyCommand>
{
    public TerminateCompanyCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MinimumLength(20)
            .WithMessage("Lý do chấm dứt cần ít nhất 20 ký tự.");
    }
}
