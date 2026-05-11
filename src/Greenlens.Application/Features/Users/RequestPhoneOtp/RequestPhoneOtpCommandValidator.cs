using FluentValidation;

namespace Greenlens.Application.Features.Users.RequestPhoneOtp;

public sealed class RequestPhoneOtpCommandValidator : AbstractValidator<RequestPhoneOtpCommand>
{
    public RequestPhoneOtpCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Số điện thoại không được để trống.")
            .Matches(@"^(0|\+84|84)(3|5|7|8|9)\d{8}$")
            .WithMessage("Số điện thoại không hợp lệ. Vui lòng nhập số điện thoại Việt Nam.");
    }
}
