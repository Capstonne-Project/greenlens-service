using FluentValidation;

namespace Greenlens.Application.Features.Organization.CreateLocalOffice;

public sealed class CreateLocalOfficeCommandValidator : AbstractValidator<CreateLocalOfficeCommand>
{
    public CreateLocalOfficeCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên văn phòng không được để trống.")
            .MaximumLength(200).WithMessage("Tên văn phòng tối đa 200 ký tự.");

        RuleFor(x => x.DepartmentId)
            .NotEmpty().WithMessage("Mã đơn vị cấp tỉnh không được để trống.");

        RuleFor(x => x.WardCode)
            .NotEmpty().WithMessage("Mã xã/phường không được để trống.")
            .MaximumLength(5).WithMessage("Mã xã/phường tối đa 5 ký tự.");
    }
}
