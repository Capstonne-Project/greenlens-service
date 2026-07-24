using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.GetMyDrafts;

/// <summary>BR-REP-019: Return all drafts for current user, newest first.</summary>
public sealed class GetMyDraftsQueryHandler(
    IReportDraftRepository drafts,
    ICurrentUser currentUser,
    ILogger<GetMyDraftsQueryHandler> logger)
    : IRequestHandler<GetMyDraftsQuery, Result<GetMyDraftsResponse>>
{
    public async Task<Result<GetMyDraftsResponse>> Handle(
        GetMyDraftsQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting my drafts for user {UserId}", currentUser.UserId);

        var items = await drafts.QueryAsNoTracking()
            .Where(d => d.UserId == currentUser.UserId)
            .OrderByDescending(d => d.UpdatedAt)
            .Select(d => new DraftItemDto(d.Id, d.Payload, d.CreatedAt, d.UpdatedAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        logger.LogInformation("Found {Count} drafts for user {UserId}", items.Count, currentUser.UserId);

        return new GetMyDraftsResponse(items);
    }
}
