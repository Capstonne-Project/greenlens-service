using FluentValidation;

namespace Greenlens.Application.Features.Users.GetAllUsersWithPaged;

public sealed class GetAllUsersWithPagedQueryValidator : AbstractValidator<GetAllUsersWithPagedQuery>
{
    public GetAllUsersWithPagedQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100.");
    }
}
