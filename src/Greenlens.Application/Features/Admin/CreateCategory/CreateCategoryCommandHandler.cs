using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;

namespace Greenlens.Application.Features.Admin.CreateCategory;

public sealed class CreateCategoryCommandHandler(
    IPollutionCategoryRepository categories,
    IUnitOfWork uow)
    : IRequestHandler<CreateCategoryCommand, Result<CreateCategoryResponse>>
{
    public async Task<Result<CreateCategoryResponse>> Handle(
        CreateCategoryCommand request, CancellationToken ct)
    {
        var category = PollutionCategory.Create(
            request.Code, request.NameVi, request.NameEn, request.IconUrl);

        categories.Add(category);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        return new CreateCategoryResponse(category.Id, category.Code, category.NameVi, category.NameEn);
    }
}
