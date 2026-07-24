using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Admin.CreateCategory;

public sealed class CreateCategoryCommandHandler(
    IPollutionCategoryRepository categories,
    IUnitOfWork uow,
    ILogger<CreateCategoryCommandHandler> logger)
    : IRequestHandler<CreateCategoryCommand, Result<CreateCategoryResponse>>
{
    public async Task<Result<CreateCategoryResponse>> Handle(
        CreateCategoryCommand request, CancellationToken ct)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        var exists = await categories.CodeExistsAsync(code, ct: ct).ConfigureAwait(false);
        if (exists)
            return Errors.Reports.CategoryCodeExists;

        var category = PollutionCategory.Create(
            code, request.NameVi, request.NameEn, request.IconUrl);

        categories.Add(category);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Category {Code} created with id {CategoryId}",
            category.Code, category.Id);

        return new CreateCategoryResponse(category.Id, category.Code, category.NameVi, category.NameEn);
    }
}
