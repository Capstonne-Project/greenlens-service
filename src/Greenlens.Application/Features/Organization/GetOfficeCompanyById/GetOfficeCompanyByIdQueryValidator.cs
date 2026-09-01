using FluentValidation;

namespace Greenlens.Application.Features.Organization.GetOfficeCompanyById;

public sealed class GetOfficeCompanyByIdQueryValidator : AbstractValidator<GetOfficeCompanyByIdQuery>
{
    public GetOfficeCompanyByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
