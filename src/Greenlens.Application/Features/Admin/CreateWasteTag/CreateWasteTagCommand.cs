using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.CreateWasteTag;

/// <summary>Admin creates a new waste tag.</summary>
public sealed record CreateWasteTagCommand(
    string Code,
    string NameVi,
    string NameEn,
    string? IconUrl,
    string? Description,
    int DisplayOrder) : IRequest<Result<CreateWasteTagResponse>>;

public sealed record CreateWasteTagResponse(Guid Id, string Code);
