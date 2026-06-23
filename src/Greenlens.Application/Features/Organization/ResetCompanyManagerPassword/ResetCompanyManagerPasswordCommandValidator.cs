using FluentValidation;

namespace Greenlens.Application.Features.Organization.ResetCompanyManagerPassword;

public sealed class ResetCompanyManagerPasswordCommandValidator
    : AbstractValidator<ResetCompanyManagerPasswordCommand>
{
    public ResetCompanyManagerPasswordCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.ManagerUserId).NotEmpty();
    }
}
