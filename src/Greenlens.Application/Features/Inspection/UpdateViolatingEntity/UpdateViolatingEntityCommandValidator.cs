using FluentValidation;

namespace Greenlens.Application.Features.Inspection.UpdateViolatingEntity;

public sealed class UpdateViolatingEntityCommandValidator : AbstractValidator<UpdateViolatingEntityCommand>
{
    public UpdateViolatingEntityCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        When(x => x.Name is not null, () =>
        {
            RuleFor(x => x.Name!).NotEmpty().MaximumLength(200);
        });

        When(x => x.Address is not null, () =>
        {
            RuleFor(x => x.Address!).MaximumLength(500);
        });

        When(x => x.TaxCode is not null, () =>
        {
            RuleFor(x => x.TaxCode!).NotEmpty().MaximumLength(20);
        });

        When(x => x.IdentityNumber is not null, () =>
        {
            RuleFor(x => x.IdentityNumber!).NotEmpty().MaximumLength(20);
        });

        When(x => x.PhoneNumber is not null, () =>
        {
            RuleFor(x => x.PhoneNumber!).MaximumLength(20);
        });
    }
}
