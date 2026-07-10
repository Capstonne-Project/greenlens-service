using FluentValidation;

namespace Greenlens.Application.Features.Organization.SuspendCompany;

public sealed class SuspendCompanyCommandValidator : AbstractValidator<SuspendCompanyCommand>
{
    public SuspendCompanyCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MinimumLength(20)
            .WithMessage("Lý do tạm ngưng cần ít nhất 20 ký tự.");
    }
}
