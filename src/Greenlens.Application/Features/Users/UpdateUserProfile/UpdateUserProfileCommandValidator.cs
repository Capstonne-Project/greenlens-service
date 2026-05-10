using FluentValidation;

namespace Greenlens.Application.Features.Users.UpdateUserProfile;

public sealed class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileCommandValidator()
    {
        RuleFor(x => x.FullName)
            .MaximumLength(200).WithMessage("Full name must not exceed 200 characters.")
            .When(x => x.FullName is not null);

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20).WithMessage("Phone number must not exceed 20 characters.")
            .Matches(@"^\+?[\d\s\-()]+$").WithMessage("Invalid phone number format.")
            .When(x => x.PhoneNumber is not null);
    }
}
