using FluentValidation;

namespace Greenlens.Application.Features.Organization.RenewContract;

public sealed class RenewContractCommandValidator : AbstractValidator<RenewContractCommand>
{
    public RenewContractCommandValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty();

        RuleFor(x => x.NewStartDate)
            .NotEmpty()
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date)
            .WithMessage("Ngày bắt đầu kỳ hợp đồng mới phải từ hôm nay trở đi.");

        RuleFor(x => x.NewEndDate)
            .NotEmpty()
            .GreaterThan(x => x.NewStartDate)
            .WithMessage("Ngày kết thúc phải sau ngày bắt đầu.");

        RuleFor(x => x.NewContractNumber)
            .NotEmpty()
            .MaximumLength(50)
            .WithMessage("Số hợp đồng tối đa 50 ký tự.");
    }
}
