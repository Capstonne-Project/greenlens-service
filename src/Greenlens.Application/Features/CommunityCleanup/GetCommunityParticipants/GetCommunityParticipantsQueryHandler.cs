using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.CommunityCleanup.Common;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.CommunityCleanup.GetCommunityParticipants;

/// <remarks>Draft rule BR-CMU-015: full participant list restricted to the event Leader or LEO/Admin.</remarks>
public sealed class GetCommunityParticipantsQueryHandler(
    ICommunityCleanupEventRepository events,
    ICommunityCleanupParticipantRepository participants,
    ICurrentUser currentUser,
    ILogger<GetCommunityParticipantsQueryHandler> logger)
    : IRequestHandler<GetCommunityParticipantsQuery, Result<CommunityCleanupParticipantsResponse>>
{
    public async Task<Result<CommunityCleanupParticipantsResponse>> Handle(GetCommunityParticipantsQuery request, CancellationToken ct)
    {
        var ev = await events.GetByIdAsync(request.EventId, ct).ConfigureAwait(false);
        if (ev is null)
            return Errors.CommunityCleanup.EventNotFound;

        var isPrivileged = string.Equals(currentUser.Role, "LEO", StringComparison.OrdinalIgnoreCase)
            || string.Equals(currentUser.Role, "Admin", StringComparison.OrdinalIgnoreCase);

        if (ev.LeaderUserId != currentUser.UserId && !isPrivileged)
            return Errors.CommunityCleanup.NotAuthorized;

        var query = participants.QueryAsNoTracking()
            .Where(p => p.EventId == request.EventId)
            .OrderBy(p => p.JoinedAt)
            .Select(p => new CommunityCleanupParticipantDto(
                p.UserId, p.User!.FullName, p.User!.AvatarUrl,
                p.Role, p.Status, p.JoinedAt, p.CheckedInAt));

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);
        var pagination = PaginationMeta.Create(request.Page, request.PageSize, totalCount);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        logger.LogInformation("Danh sách participant chương trình {EventId}. Số lượng: {Count}", request.EventId, items.Count);
        return new CommunityCleanupParticipantsResponse(items, pagination);
    }
}
