using FluentValidation;

namespace Greenlens.Application.Features.Users.VerifyPhoneOtp;

public sealed class VerifyPhoneOtpCommandValidator : AbstractValidator<VerifyPhoneOtpCommand>
{
    public VerifyPhoneOtpCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Số điện thoại không được để trống.")
            .Matches(@"^(0|\+84|84)(3|5|7|8|9)\d{8}$")
            .WithMessage("Số điện thoại không hợp lệ.");

        RuleFor(x => x.OtpCode)
            .NotEmpty().WithMessage("Mã OTP không được để trống.")
            .Length(6).WithMessage("Mã OTP phải có 6 chữ số.")
            .Matches(@"^\d{6}$").WithMessage("Mã OTP chỉ chứa chữ số.");
    }
}
