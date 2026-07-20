using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.CreateCategory;

public sealed record CreateCategoryCommand(
    string Code, string NameVi, string NameEn, string? IconUrl)
    : IRequest<Result<CreateCategoryResponse>>;

public sealed record CreateCategoryResponse(Guid Id, string Code, string NameVi, string NameEn);
