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

        // ── Manager account (optional — both must be provided together or neither) ──
        RuleFor(x => x.ManagerEmail)
            .NotEmpty().EmailAddress().MaximumLength(200)
            .When(x => x.ManagerEmail is not null);

        RuleFor(x => x.ManagerFullName)
            .NotEmpty().MaximumLength(200)
            .When(x => x.ManagerFullName is not null);

        // If either field is provided, the other must be too
        RuleFor(x => x.ManagerFullName)
            .NotEmpty()
            .When(x => !string.IsNullOrEmpty(x.ManagerEmail))
            .WithMessage("ManagerFullName bắt buộc khi cung cấp ManagerEmail.");

        RuleFor(x => x.ManagerEmail)
            .NotEmpty()
            .When(x => !string.IsNullOrEmpty(x.ManagerFullName))
            .WithMessage("ManagerEmail bắt buộc khi cung cấp ManagerFullName.");

        // ── WardCodes (optional) ──
        RuleForEach(x => x.WardCodes)
            .NotEmpty()
            .MaximumLength(20)
            .When(x => x.WardCodes is { Count: > 0 });

        RuleFor(x => x.TaxCode).MaximumLength(20);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Email).MaximumLength(200).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
    }
}
