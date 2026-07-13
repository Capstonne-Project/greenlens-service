using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.DeleteCategory;

/// <summary>
/// Soft-delete a PollutionCategory. 
/// Reports referencing this category will still exist, but the category won't be listed for new reports.
/// </summary>
public sealed record DeleteCategoryCommand(Guid Id) : IRequest<Result>;
