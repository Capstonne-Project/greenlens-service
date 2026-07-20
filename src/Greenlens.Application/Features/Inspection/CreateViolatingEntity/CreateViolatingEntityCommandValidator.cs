using FluentValidation;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.Features.Inspection.CreateViolatingEntity;

public sealed class CreateViolatingEntityCommandValidator : AbstractValidator<CreateViolatingEntityCommand>
{
    public CreateViolatingEntityCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên đối tượng vi phạm không được trống.")
            .MaximumLength(200);

        RuleFor(x => x.Address)
            .MaximumLength(500);

        RuleFor(x => x.TaxCode)
            .MaximumLength(20)
            .When(x => x.TaxCode is not null);

        RuleFor(x => x.IdentityNumber)
            .MaximumLength(20)
            .When(x => x.IdentityNumber is not null);

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20)
            .When(x => x.PhoneNumber is not null);

        // Business phải có TaxCode; Individual nên có IdentityNumber (không bắt buộc)
        RuleFor(x => x.TaxCode)
            .NotEmpty().WithMessage("Doanh nghiệp phải có mã số thuế (MST/MSDN).")
            .When(x => x.Type == ViolatorType.Business);
    }
}
