using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.ArchiveCategory;

/// <summary>Toggle category active/inactive (soft archive).</summary>
public sealed record ArchiveCategoryCommand(Guid Id, bool Archive) : IRequest<Result>;
