using FluentValidation;

namespace Greenlens.Application.Features.Inspection.DeleteViolatingEntity;

public sealed class DeleteViolatingEntityCommandValidator : AbstractValidator<DeleteViolatingEntityCommand>
{
    public DeleteViolatingEntityCommandValidator()
    {
        RuleFor(x => x.EntityId).NotEmpty();
    }
}
