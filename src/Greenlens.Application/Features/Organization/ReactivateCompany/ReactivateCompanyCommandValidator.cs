using FluentValidation;

namespace Greenlens.Application.Features.Organization.ReactivateCompany;

public sealed class ReactivateCompanyCommandValidator : AbstractValidator<ReactivateCompanyCommand>
{
    public ReactivateCompanyCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
    }
}
