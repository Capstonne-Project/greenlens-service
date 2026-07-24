using FluentValidation;

namespace Greenlens.Application.Features.Auth.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Email) ^ !string.IsNullOrWhiteSpace(x.Phone))
            .WithMessage("Cung cấp email hoặc số điện thoại, không phải cả hai.");

        When(x => !string.IsNullOrWhiteSpace(x.Email), () =>
        {
            RuleFor(x => x.Email!).NotEmpty().EmailAddress();
        });

        When(x => !string.IsNullOrWhiteSpace(x.Phone), () =>
        {
            RuleFor(x => x.Phone!)
                .Matches(@"^(\+84|84|0)[0-9]{8,10}$")
                .WithMessage("Số điện thoại không hợp lệ.");
        });

        RuleFor(x => x.Password).NotEmpty();
    }
}
