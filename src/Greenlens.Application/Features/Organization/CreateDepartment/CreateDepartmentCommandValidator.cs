using FluentValidation;

namespace Greenlens.Application.Features.Organization.CreateDepartment;

public sealed class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên đơn vị không được để trống.")
            .MaximumLength(200).WithMessage("Tên đơn vị tối đa 200 ký tự.");

        RuleFor(x => x.ProvinceCode)
            .NotEmpty().WithMessage("Mã tỉnh/thành phố không được để trống.")
            .Length(2).WithMessage("Mã tỉnh/thành phố phải có 2 ký tự.");
    }
}
