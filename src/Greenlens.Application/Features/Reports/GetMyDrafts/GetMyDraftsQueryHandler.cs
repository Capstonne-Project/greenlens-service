using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Reports.GetMyDrafts;

/// <summary>BR-REP-019: Return all drafts for current user, newest first.</summary>
public sealed class GetMyDraftsQueryHandler(
    IReportDraftRepository drafts,
    ICurrentUser currentUser)
    : IRequestHandler<GetMyDraftsQuery, Result<GetMyDraftsResponse>>
{
    public async Task<Result<GetMyDraftsResponse>> Handle(
        GetMyDraftsQuery request,
        CancellationToken cancellationToken)
    {
        var items = await drafts.QueryAsNoTracking()
            .Where(d => d.UserId == currentUser.UserId)
            .OrderByDescending(d => d.UpdatedAt)
            .Select(d => new DraftItemDto(d.Id, d.Payload, d.CreatedAt, d.UpdatedAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new GetMyDraftsResponse(items);
    }
}
