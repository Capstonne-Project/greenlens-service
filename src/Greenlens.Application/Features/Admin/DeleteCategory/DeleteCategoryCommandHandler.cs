using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Admin.DeleteCategory;

/// <summary>Admin soft-deletes a pollution category.</summary>
/// <remarks>Implements: BR-ADM-003 (catalog management), BR-DAT-002 (retain referential integrity).</remarks>
public sealed class DeleteCategoryCommandHandler(
    IPollutionCategoryRepository categories,
    IReportRepository reports,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    ILogger<DeleteCategoryCommandHandler> logger) : IRequestHandler<DeleteCategoryCommand, Result>
{
    public async Task<Result> Handle(DeleteCategoryCommand request, CancellationToken ct)
    {
        var category = await categories.GetByIdIncludingDeletedAsync(request.Id, ct).ConfigureAwait(false);
        if (category is null)
        {
            logger.LogWarning("Category not found: {Id}", request.Id);
            return Errors.Reports.CategoryNotFound;
        }
        if (category.IsDeleted)
        {
            logger.LogWarning("Category already deleted: {Id}", request.Id);
            return Errors.Reports.CategoryAlreadyDeleted;
        }

        var inUse = await reports.ExistsByCategoryIdAsync(request.Id, ct).ConfigureAwait(false);
        if (inUse)
        {
            logger.LogWarning("Category is in use: {Id}", request.Id);
            return Errors.Reports.CategoryInUse;
        }

        category.SoftDelete(currentUser.UserId.ToString());
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("PollutionCategory {CategoryId} soft-deleted by {UserId}", request.Id, currentUser.UserId);

        return Result.Success();
    }
}
