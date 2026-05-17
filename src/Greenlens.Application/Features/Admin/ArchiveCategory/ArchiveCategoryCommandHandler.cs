using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.ArchiveCategory;

public sealed class ArchiveCategoryCommandHandler(
    IPollutionCategoryRepository categories,
    IUnitOfWork uow) : IRequestHandler<ArchiveCategoryCommand, Result>
{
    public async Task<Result> Handle(ArchiveCategoryCommand request, CancellationToken ct)
    {
        var category = await categories.GetByIdAsync(request.Id, ct).ConfigureAwait(false);
        if (category is null)
            return Errors.Reports.CategoryNotFound;

        if (request.Archive)
            category.Deactivate();
        else
            category.Activate();

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Success();
    }
}
