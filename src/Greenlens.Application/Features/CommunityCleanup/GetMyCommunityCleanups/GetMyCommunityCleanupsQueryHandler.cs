using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.CommunityCleanup.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.CommunityCleanup.GetMyCommunityCleanups;

public sealed class GetMyCommunityCleanupsQueryHandler(
    ICommunityCleanupParticipantRepository participants,
    ICurrentUser currentUser,
    ILogger<GetMyCommunityCleanupsQueryHandler> logger)
    : IRequestHandler<GetMyCommunityCleanupsQuery, Result<CommunityCleanupListResponse>>
{
    private sealed record Row(
        CommunityCleanupEvent Event,
        CommunityCleanupParticipant MyParticipation,
        string ReportCode,
        decimal ReportLat,
        decimal ReportLng,
        string LeaderFullName,
        int ParticipantCount,
        string? ThumbnailUrl);

    public async Task<Result<CommunityCleanupListResponse>> Handle(GetMyCommunityCleanupsQuery request, CancellationToken ct)
    {
        var query = participants.QueryAsNoTracking()
            .Where(p => p.UserId == currentUser.UserId)
            .Select(p => new Row(
                p.Event!,
                p,
                p.Event!.Report!.Code,
                p.Event!.Report!.Latitude,
                p.Event!.Report!.Longitude,
                p.Event!.LeaderUser!.FullName,
                p.Event!.Participants.Count(x =>
                    x.Status != CommunityCleanupParticipantStatus.Withdrawn
                    && x.Status != CommunityCleanupParticipantStatus.NoShow),
                p.Event!.Report!.Media
                    .Where(m => m.Type == MediaType.Image)
                    .OrderBy(m => m.UploadedAt)
                    .Select(m => m.ThumbnailUrl ?? m.Url)
                    .FirstOrDefault()))
            .OrderByDescending(r => r.Event.CreatedAt);

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);
        var pagination = PaginationMeta.Create(request.Page, request.PageSize, totalCount);

        var rows = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var items = rows.Select(r => new CommunityCleanupListItemDto(
            r.Event.Id, r.Event.ReportId, r.ReportCode, r.Event.Status, r.Event.Title,
            r.Event.LeaderUserId, r.LeaderFullName,
            r.Event.StartsAt, r.Event.JoinClosesAt, r.Event.MaxParticipants,
            r.ParticipantCount, Math.Max(0, r.Event.MaxParticipants - r.ParticipantCount),
            r.Event.ProgressPercent, r.ReportLat, r.ReportLng, r.ThumbnailUrl,
            new CommunityCleanupMyParticipationDto(r.MyParticipation.Status, r.MyParticipation.JoinedAt, r.MyParticipation.Role)))
            .ToList();

        logger.LogInformation("Danh sách chương trình cộng đồng đã tham gia. Số lượng: {Count}", items.Count);
        return new CommunityCleanupListResponse(items, pagination);
    }
}
