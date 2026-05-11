using FluentValidation;

namespace Greenlens.Application.Features.Users.VerifyPhoneFirebase;

public sealed class VerifyPhoneFirebaseCommandValidator : AbstractValidator<VerifyPhoneFirebaseCommand>
{
    public VerifyPhoneFirebaseCommandValidator()
    {
        RuleFor(x => x.FirebaseIdToken)
            .NotEmpty().WithMessage("Firebase ID token không được để trống.");
    }
}
