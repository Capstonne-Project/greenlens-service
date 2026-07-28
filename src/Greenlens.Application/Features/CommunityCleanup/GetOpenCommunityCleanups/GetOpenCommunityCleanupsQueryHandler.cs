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

namespace Greenlens.Application.Features.CommunityCleanup.GetOpenCommunityCleanups;

/// <summary>Draft rule BR-CMU-015: no participant PII, only counts.</summary>
public sealed class GetOpenCommunityCleanupsQueryHandler(
    ICommunityCleanupEventRepository events,
    ICurrentUser currentUser,
    ILogger<GetOpenCommunityCleanupsQueryHandler> logger)
    : IRequestHandler<GetOpenCommunityCleanupsQuery, Result<CommunityCleanupListResponse>>
{
    private sealed record Row(
        CommunityCleanupEvent Event,
        string ReportCode,
        decimal ReportLat,
        decimal ReportLng,
        string LeaderFullName,
        int ParticipantCount,
        string? ThumbnailUrl,
        CommunityCleanupParticipant? MyParticipation);

    public async Task<Result<CommunityCleanupListResponse>> Handle(GetOpenCommunityCleanupsQuery request, CancellationToken ct)
    {
        var rows = await events.QueryAsNoTracking()
            .Where(e => e.Status == CommunityCleanupStatus.OpenForJoin)
            .Select(e => new Row(
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
                    .FirstOrDefault(),
                currentUser.IsAuthenticated
                    ? e.Participants.FirstOrDefault(p => p.UserId == currentUser.UserId)
                    : null))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var withDistance = rows.Select(r => (
            Row: r,
            DistanceMeters: request.NearLat.HasValue && request.NearLng.HasValue
                ? HaversineMeters((double)request.NearLat.Value, (double)request.NearLng.Value, (double)r.ReportLat, (double)r.ReportLng)
                : (double?)null));

        if (request.NearLat.HasValue && request.NearLng.HasValue && request.RadiusMeters.HasValue)
            withDistance = withDistance.Where(x => x.DistanceMeters!.Value <= request.RadiusMeters.Value);

        var ordered = request.NearLat.HasValue && request.NearLng.HasValue
            ? withDistance.OrderBy(x => x.DistanceMeters).ToList()
            : withDistance.OrderBy(x => x.Row.Event.StartsAt).ToList();

        var totalCount = ordered.Count;
        var pagination = PaginationMeta.Create(request.Page, request.PageSize, totalCount);

        var items = ordered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => MapItem(x.Row))
            .ToList();

        logger.LogInformation("Danh sách chương trình dọn cộng đồng đang mở. Số lượng: {Count}", items.Count);
        return new CommunityCleanupListResponse(items, pagination);
    }

    private static CommunityCleanupListItemDto MapItem(Row r)
    {
        var ev = r.Event;
        var participation = r.MyParticipation is { } p
            ? new CommunityCleanupMyParticipationDto(p.Status, p.JoinedAt, p.Role)
            : null;

        return new CommunityCleanupListItemDto(
            ev.Id, ev.ReportId, r.ReportCode, ev.Status, ev.Title,
            ev.LeaderUserId, r.LeaderFullName,
            ev.StartsAt, ev.JoinClosesAt, ev.MaxParticipants,
            r.ParticipantCount, Math.Max(0, ev.MaxParticipants - r.ParticipantCount),
            ev.ProgressPercent, r.ReportLat, r.ReportLng, r.ThumbnailUrl, participation);
    }

    private static double HaversineMeters(double lat1, double lng1, double lat2, double lng2)
    {
        const double earthRadiusMeters = 6_371_000;
        var dLat = ToRadians(lat2 - lat1);
        var dLng = ToRadians(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
            + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusMeters * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}
