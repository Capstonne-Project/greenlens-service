using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.UpdateWasteTag;

/// <summary>Admin updates an existing waste tag.</summary>
public sealed record UpdateWasteTagCommand(
    Guid Id,
    string NameVi,
    string NameEn,
    string? IconUrl,
    string? Description,
    int DisplayOrder) : IRequest<Result>;
