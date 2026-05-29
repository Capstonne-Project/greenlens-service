using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.GetWasteTags;

/// <summary>
/// Returns all active waste tags for UI dropdown/chip selection.
/// </summary>
public sealed record GetWasteTagsQuery() : IRequest<Result<GetWasteTagsResponse>>;

public sealed record GetWasteTagsResponse(IReadOnlyList<WasteTagItem> Tags);

public sealed record WasteTagItem(
    Guid Id,
    string Code,
    string NameVi,
    string NameEn,
    string? IconUrl,
    string? Description,
    int DisplayOrder);

public sealed class GetWasteTagsQueryHandler(IWasteTagRepository wasteTags)
    : IRequestHandler<GetWasteTagsQuery, Result<GetWasteTagsResponse>>
{
    public async Task<Result<GetWasteTagsResponse>> Handle(
        GetWasteTagsQuery request, CancellationToken ct)
    {
        var tags = await wasteTags.GetAllActiveAsync(ct).ConfigureAwait(false);

        var items = tags.Select(t => new WasteTagItem(
            t.Id, t.Code, t.NameVi, t.NameEn,
            t.IconUrl, t.Description, t.DisplayOrder)).ToList();

        return new GetWasteTagsResponse(items);
    }
}
