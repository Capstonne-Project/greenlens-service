using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.UpdateCategory;

public sealed record UpdateCategoryCommand(
    Guid Id, string NameVi, string NameEn, string? IconUrl) : IRequest<Result>;
