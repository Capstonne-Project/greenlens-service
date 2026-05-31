using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Admin.UpdateCategory;

public sealed class UpdateCategoryCommandHandler(
    IPollutionCategoryRepository categories,
    IUnitOfWork uow,
    ILogger<UpdateCategoryCommandHandler> logger) : IRequestHandler<UpdateCategoryCommand, Result>
{
    public async Task<Result> Handle(UpdateCategoryCommand request, CancellationToken ct)
    {
        var category = await categories.GetByIdAsync(request.Id, ct).ConfigureAwait(false);
        if (category is null)
            return Errors.Reports.CategoryNotFound;

        // Update category details
        category.Update(request.NameVi, request.NameEn, request.IconUrl);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Category {CategoryId} updated", request.Id);

        return Result.Success();
    }
}
