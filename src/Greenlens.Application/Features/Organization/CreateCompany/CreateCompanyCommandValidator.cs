using FluentValidation;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.Features.Organization.CreateCompany;

public sealed class CreateCompanyCommandValidator : AbstractValidator<CreateCompanyCommand>
{
    public CreateCompanyCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.ContractNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ContractStartDate).NotEmpty();
        RuleFor(x => x.ContractType).IsInEnum();

        RuleFor(x => x.ContractEndDate)
            .GreaterThan(x => x.ContractStartDate)
            .When(x => x.ContractEndDate.HasValue)
            .WithMessage("Ngày kết thúc hợp đồng phải sau ngày bắt đầu.");

        // Bidding requires EndDate; Subsidiary = null (vô thời hạn)
        RuleFor(x => x.ContractEndDate)
            .NotNull()
            .When(x => x.ContractType == ContractType.Bidding)
            .WithMessage("Hợp đồng đấu thầu (Bidding) bắt buộc có ngày kết thúc.");

        RuleFor(x => x.TaxCode).MaximumLength(20);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Email).MaximumLength(200).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
    }
}
