using FluentValidation;

namespace Greenlens.Application.Features.Auth.Register;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"\d").WithMessage("Password must contain at least one digit.")
            .Matches(@"[\W_]").WithMessage("Password must contain at least one special character.");

        /// BR-AUTH-011: Họ tên 2–50 ký tự, chỉ chữ cái (kể cả dấu tiếng Việt) và khoảng trắng.
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ tên là bắt buộc.")
            .Length(2, 50).WithMessage("Họ tên từ 2-50 ký tự.")
            .Matches(@"^[\p{L}\s]+$").WithMessage("Họ tên không hợp lệ (không chứa ký tự đặc biệt).");

        /// BR-AUTH-012: Phải đồng ý điều khoản sử dụng trước khi đăng ký.
        RuleFor(x => x.AcceptTerms)
            .Equal(true).WithMessage("Bạn phải đồng ý với điều khoản để đăng ký.");
    }
}
