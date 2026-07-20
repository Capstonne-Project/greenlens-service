using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Admin.DeleteCategory;

public sealed class DeleteCategoryCommandHandler(
    IPollutionCategoryRepository categories,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    ILogger<DeleteCategoryCommandHandler> logger) : IRequestHandler<DeleteCategoryCommand, Result>
{
    public async Task<Result> Handle(DeleteCategoryCommand request, CancellationToken ct)
    {
        var category = await categories.GetByIdAsync(request.Id, ct).ConfigureAwait(false);
        if (category is null)
            return Errors.Reports.CategoryNotFound;

        category.SoftDelete(currentUser.UserId.ToString());
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("PollutionCategory {CategoryId} soft-deleted by {UserId}", request.Id, currentUser.UserId);

        return Result.Success();
    }
}
