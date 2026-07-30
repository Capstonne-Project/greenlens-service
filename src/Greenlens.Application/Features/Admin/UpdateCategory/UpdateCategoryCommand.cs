using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.UpdateCategory;

/// <remarks>Implements: BR-ADM-003, BR-ADM-010.</remarks>
public sealed record UpdateCategoryCommand(
    Guid Id, string NameVi, string NameEn, string? IconUrl) : IRequest<Result>;
