using FluentValidation;

namespace Greenlens.Application.Features.Organization.GetCompanyKpi;

public sealed class GetCompanyKpiQueryValidator : AbstractValidator<GetCompanyKpiQuery>
{
    public GetCompanyKpiQueryValidator()
    {
        When(q => q.From.HasValue && q.To.HasValue, () =>
        {
            RuleFor(q => q.To!.Value)
                .GreaterThan(q => q.From!.Value)
                .WithMessage("To phải sau From.");
        });
    }
}
