using FluentValidation;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.Features.Organization.RecruitStaff;

public sealed class RecruitStaffCommandValidator : AbstractValidator<RecruitStaffCommand>
{
    public RecruitStaffCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống.")
            .EmailAddress().WithMessage("Email không hợp lệ.");

        RuleFor(x => x.TargetRole)
            .Must(r => r is UserRole.Cleaner or UserRole.Inspector)
            .WithMessage("Vai trò phải là Cleaner hoặc Inspector.");
    }
}
