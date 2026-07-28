using Greenlens.Application.Features.CommunityCleanup.Common;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.CommunityCleanup.GetCommunityParticipants;

/// <summary>Full participant list — Leader-of-event or LEO/Admin only (draft BR-CMU-015).</summary>
public sealed record GetCommunityParticipantsQuery(
    Guid EventId,
    int Page,
    int PageSize) : IRequest<Result<CommunityCleanupParticipantsResponse>>;
