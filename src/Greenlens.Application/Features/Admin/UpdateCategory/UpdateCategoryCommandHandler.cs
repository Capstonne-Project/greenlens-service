using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.UpdateCategory;

public sealed class UpdateCategoryCommandHandler(
    IPollutionCategoryRepository categories,
    IUnitOfWork uow) : IRequestHandler<UpdateCategoryCommand, Result>
{
    public async Task<Result> Handle(UpdateCategoryCommand request, CancellationToken ct)
    {
        var category = await categories.GetByIdAsync(request.Id, ct).ConfigureAwait(false);
        if (category is null)
            return Errors.Reports.CategoryNotFound;

        category.Update(request.NameVi, request.NameEn, request.IconUrl);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Success();
    }
}
