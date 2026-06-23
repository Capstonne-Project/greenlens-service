using FluentValidation;

namespace Greenlens.Application.Features.Organization.CreateCompanyManager;

public sealed class CreateCompanyManagerCommandValidator : AbstractValidator<CreateCompanyManagerCommand>
{
    public CreateCompanyManagerCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.ManagerEmail).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.ManagerFullName).NotEmpty().MaximumLength(200);
    }
}
