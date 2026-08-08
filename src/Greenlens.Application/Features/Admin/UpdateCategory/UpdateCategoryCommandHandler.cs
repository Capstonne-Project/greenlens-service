using System.Text.Json;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Admin.UpdateCategory;

/// <remarks>Implements: BR-ADM-003, BR-ADM-010.</remarks>
public sealed class UpdateCategoryCommandHandler(
    IPollutionCategoryRepository categories,
    IUnitOfWork uow,
    IAuditLogger auditLogger,
    ILogger<UpdateCategoryCommandHandler> logger) : IRequestHandler<UpdateCategoryCommand, Result>
{
    public async Task<Result> Handle(UpdateCategoryCommand request, CancellationToken ct)
    {
        var category = await categories.GetByIdAsync(request.Id, ct).ConfigureAwait(false);
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

        var oldSnapshot = JsonSerializer.Serialize(new
        {
            category.NameVi,
            category.NameEn,
            category.IconUrl
        });

        category.Update(request.NameVi, request.NameEn, request.IconUrl);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        await auditLogger.LogAsync(
            "UpdateCategory",
            "PollutionCategory",
            category.Id.ToString(),
            oldValues: oldSnapshot,
            newValues: JsonSerializer.Serialize(new
            {
                category.NameVi,
                category.NameEn,
                category.IconUrl
            }),
            ct).ConfigureAwait(false);

        logger.LogInformation("Category {CategoryId} updated", request.Id);

        return Result.Success();
    }
}
