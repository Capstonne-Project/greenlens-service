using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Admin.ArchiveCategory;

public sealed class ArchiveCategoryCommandHandler(
    IPollutionCategoryRepository categories,
    IUnitOfWork uow,
    ILogger<ArchiveCategoryCommandHandler> logger) : IRequestHandler<ArchiveCategoryCommand, Result>
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

        logger.LogInformation("Category {CategoryId} {Action}",
            request.Id, request.Archive ? "archived" : "restored");

        return Result.Success();
    }
}
