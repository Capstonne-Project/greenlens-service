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

namespace Greenlens.Application.Features.CommunityCleanup.GetLedCommunityCleanups;

public sealed class GetLedCommunityCleanupsQueryHandler(
    ICommunityCleanupEventRepository events,
    ICurrentUser currentUser,
    ILogger<GetLedCommunityCleanupsQueryHandler> logger)
    : IRequestHandler<GetLedCommunityCleanupsQuery, Result<CommunityCleanupListResponse>>
{
    private sealed record Row(
        CommunityCleanupEvent Event,
        string ReportCode,
        decimal ReportLat,
        decimal ReportLng,
        string LeaderFullName,
        int ParticipantCount,
        string? ThumbnailUrl);

    public async Task<Result<CommunityCleanupListResponse>> Handle(GetLedCommunityCleanupsQuery request, CancellationToken ct)
    {
        var query = events.QueryAsNoTracking().Where(e => e.LeaderUserId == currentUser.UserId);

        if (request.Status.HasValue)
            query = query.Where(e => e.Status == request.Status.Value);

        var projected = query.Select(e => new Row(
            e,
            e.Report!.Code,
            e.Report!.Latitude,
            e.Report!.Longitude,
            e.LeaderUser!.FullName,
            e.Participants.Count(p =>
                p.Status != CommunityCleanupParticipantStatus.Withdrawn
                && p.Status != CommunityCleanupParticipantStatus.NoShow),
            e.Report!.Media
                .Where(m => m.Type == MediaType.Image)
                .OrderBy(m => m.UploadedAt)
                .Select(m => m.ThumbnailUrl ?? m.Url)
                .FirstOrDefault()))
            .OrderByDescending(r => r.Event.StartsAt);

        var totalCount = await projected.CountAsync(ct).ConfigureAwait(false);
        var pagination = PaginationMeta.Create(request.Page, request.PageSize, totalCount);

        var rows = await projected
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var items = rows.Select(r => new CommunityCleanupListItemDto(
            r.Event.Id, r.Event.ReportId, r.ReportCode, r.Event.Status, r.Event.Title,
            r.Event.LeaderUserId, r.LeaderFullName,
            r.Event.StartsAt, r.Event.JoinClosesAt, r.Event.MaxParticipants,
            r.ParticipantCount, Math.Max(0, r.Event.MaxParticipants - r.ParticipantCount),
            r.Event.ProgressPercent, r.ReportLat, r.ReportLng, r.ThumbnailUrl, MyParticipation: null))
            .ToList();

        logger.LogInformation("Danh sách chương trình Leader dẫn dắt. Số lượng: {Count}", items.Count);
        return new CommunityCleanupListResponse(items, pagination);
    }
}
