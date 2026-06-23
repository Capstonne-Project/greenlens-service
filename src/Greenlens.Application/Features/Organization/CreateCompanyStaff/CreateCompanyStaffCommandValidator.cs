using FluentValidation;

namespace Greenlens.Application.Features.Organization.CreateCompanyStaff;

public sealed class CreateCompanyStaffCommandValidator : AbstractValidator<CreateCompanyStaffCommand>
{
    public CreateCompanyStaffCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Position).MaximumLength(100);
    }
}
