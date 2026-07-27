using FluentValidation;

namespace Greenlens.Application.Features.Reports.RejectReopenRequest;

public sealed class RejectReopenRequestCommandValidator : AbstractValidator<RejectReopenRequestCommand>
{
    public RejectReopenRequestCommandValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .MinimumLength(20)
            .MaximumLength(2000);
    }
}
