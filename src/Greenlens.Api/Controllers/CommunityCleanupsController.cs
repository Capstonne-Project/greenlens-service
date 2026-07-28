using Greenlens.Api.Extensions;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.CommunityCleanup.CancelCommunityCleanup;
using Greenlens.Application.Features.CommunityCleanup.CheckInCommunityCleanup;
using Greenlens.Application.Features.CommunityCleanup.CloseJoinCommunityCleanup;
using Greenlens.Application.Features.CommunityCleanup.Common;
using Greenlens.Application.Features.CommunityCleanup.CreateCommunityCleanup;
using Greenlens.Application.Features.CommunityCleanup.GetCommunityCleanupById;
using Greenlens.Application.Features.CommunityCleanup.GetCommunityParticipants;
using Greenlens.Application.Features.CommunityCleanup.GetLedCommunityCleanups;
using Greenlens.Application.Features.CommunityCleanup.GetMyCommunityCleanups;
using Greenlens.Application.Features.CommunityCleanup.GetOfficeCommunityQueue;
using Greenlens.Application.Features.CommunityCleanup.GetOpenCommunityCleanups;
using Greenlens.Application.Features.CommunityCleanup.JoinCommunityCleanup;
using Greenlens.Application.Features.CommunityCleanup.RejectCommunityVerification;
using Greenlens.Application.Features.CommunityCleanup.StartCommunityCleanup;
using Greenlens.Application.Features.CommunityCleanup.SubmitCommunityBeforeImages;
using Greenlens.Application.Features.CommunityCleanup.SubmitCommunityVerification;
using Greenlens.Application.Features.CommunityCleanup.UpdateCommunityProgress;
using Greenlens.Application.Features.CommunityCleanup.VerifyCommunityCleanup;
using Greenlens.Application.Features.CommunityCleanup.WithdrawCommunityCleanup;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Greenlens.Api.Controllers;

/// <summary>
/// Community Cleanup — crowdsourced cleanup programs on Verified reports.
/// Draft spec: docs/community-cleanup-feature-spec.md (BR-CMU-* pending official IDs).
/// </summary>
[ApiController]
[Route("v1/community-cleanups")]
[Produces("application/json")]
[Tags("🧹 Community Cleanup")]
public sealed class CommunityCleanupsController(ISender sender) : ControllerBase
{
    // ═══════════════════════════════════════════
    // ██  LEO — Web
    // ═══════════════════════════════════════════

    [HttpPost("~/v1/reports/{reportId:guid}/community-cleanups")]
    [Authorize(Roles = "LEO,Admin")]
    [SwaggerOperation(Summary = "[LEO] Mở chương trình dọn cộng đồng", Description = "Mở chương trình trên báo cáo Verified và chỉ định Leader (Cleaner). Chuyển report Verified → InProgress; chặn AssignTeam/DispatchToCompany trong khi active.")]
    [SwaggerResponse(201, "Đã tạo", typeof(ApiResponse<CommunityCleanupEventDetailResponse>))]
    [SwaggerResponse(409, "Đã có chương trình active hoặc report đã được phân công", typeof(ApiResponse))]
    [SwaggerResponse(422, "Report chưa Verified hoặc Leader không hợp lệ", typeof(ApiResponse))]
    public async Task<IActionResult> CreateAsync(
        [FromRoute] Guid reportId, [FromBody] CreateCommunityCleanupRequest request, CancellationToken ct)
        => (await sender.Send(new CreateCommunityCleanupCommand(
            reportId, request.Title, request.Description, request.LeaderUserId,
            request.StartsAt, request.EndsAt, request.JoinClosesAt,
            request.MaxParticipants ?? 50, request.MeetingNote,
            request.MeetingLatitude, request.MeetingLongitude), ct)).ToHttpCreated();

    [HttpPost("{eventId:guid}/close-join")]
    [Authorize(Roles = "LEO,Admin")]
    [SwaggerOperation(Summary = "[LEO] Đóng đăng ký", Description = "OpenForJoin → JoinClosed.")]
    [SwaggerResponse(200, "Đã đóng đăng ký", typeof(ApiResponse))]
    public async Task<IActionResult> CloseJoinAsync([FromRoute] Guid eventId, CancellationToken ct)
        => (await sender.Send(new CloseJoinCommunityCleanupCommand(eventId), ct)).ToHttpNoContent("Đã đóng đăng ký.");

    [HttpPost("{eventId:guid}/cancel")]
    [Authorize(Roles = "LEO,Admin")]
    [SwaggerOperation(Summary = "[LEO] Hủy chương trình", Description = "Yêu cầu lý do ≥ 20 ký tự. Report về Verified nếu chưa Resolved; participants đang Joined/CheckedIn bị rút.")]
    [SwaggerResponse(200, "Đã hủy", typeof(ApiResponse))]
    [SwaggerResponse(422, "Lý do quá ngắn hoặc status không hợp lệ", typeof(ApiResponse))]
    public async Task<IActionResult> CancelAsync(
        [FromRoute] Guid eventId, [FromBody] CancelCommunityCleanupRequest request, CancellationToken ct)
        => (await sender.Send(new CancelCommunityCleanupCommand(eventId, request.Reason), ct)).ToHttpNoContent("Đã hủy chương trình.");

    [HttpPost("{eventId:guid}/verify")]
    [Authorize(Roles = "LEO,Admin")]
    [SwaggerOperation(Summary = "[LEO] Duyệt xác thực", Description = "PendingVerification → Completed; Report → Resolved.")]
    [SwaggerResponse(200, "Đã duyệt", typeof(ApiResponse))]
    [SwaggerResponse(422, "Status không hợp lệ", typeof(ApiResponse))]
    public async Task<IActionResult> VerifyAsync([FromRoute] Guid eventId, CancellationToken ct)
        => (await sender.Send(new VerifyCommunityCleanupCommand(eventId), ct)).ToHttpNoContent("Đã duyệt xác thực thành công.");

    [HttpPost("{eventId:guid}/reject-verification")]
    [Authorize(Roles = "LEO,Admin")]
    [SwaggerOperation(Summary = "[LEO] Từ chối xác thực", Description = "Yêu cầu lý do ≥ 20 ký tự. PendingVerification → InProgress.")]
    [SwaggerResponse(200, "Đã từ chối", typeof(ApiResponse))]
    [SwaggerResponse(422, "Lý do quá ngắn hoặc status không hợp lệ", typeof(ApiResponse))]
    public async Task<IActionResult> RejectVerificationAsync(
        [FromRoute] Guid eventId, [FromBody] CancelCommunityCleanupRequest request, CancellationToken ct)
        => (await sender.Send(new RejectCommunityVerificationCommand(eventId, request.Reason), ct)).ToHttpNoContent("Đã từ chối xác thực.");

    [HttpGet("office-queue")]
    [Authorize(Roles = "LEO,Admin")]
    [SwaggerOperation(Summary = "[LEO] Hàng đợi chương trình cộng đồng", Description = "Mặc định lọc PendingVerification, scoped theo office của LEO.")]
    [SwaggerResponse(200, "Danh sách", typeof(ApiResponse<CommunityCleanupListResponse>))]
    public async Task<IActionResult> GetOfficeQueueAsync(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] CommunityCleanupStatus? status = null, CancellationToken ct = default)
        => (await sender.Send(new GetOfficeCommunityQueueQuery(page, pageSize, status), ct)).ToHttp();

    // ═══════════════════════════════════════════
    // ██  Citizen — Mobile
    // ═══════════════════════════════════════════

    [HttpGet]
    [Authorize]
    [SwaggerOperation(Summary = "[Citizen] Danh sách chương trình đang mở", Description = "OpenForJoin, lọc gần vị trí (nearLat/nearLng/radiusMeters) nếu cung cấp.")]
    [SwaggerResponse(200, "Danh sách", typeof(ApiResponse<CommunityCleanupListResponse>))]
    public async Task<IActionResult> GetOpenAsync(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] decimal? nearLat = null, [FromQuery] decimal? nearLng = null,
        [FromQuery] double? radiusMeters = null, CancellationToken ct = default)
        => (await sender.Send(new GetOpenCommunityCleanupsQuery(page, pageSize, nearLat, nearLng, radiusMeters), ct)).ToHttp();

    [HttpGet("{eventId:guid}")]
    [Authorize]
    [SwaggerOperation(Summary = "[Auth] Chi tiết chương trình", Description = "Bao gồm participantCount/spotsLeft, myParticipation, isLeader. Không lộ danh sách tên participant.")]
    [SwaggerResponse(200, "Chi tiết", typeof(ApiResponse<CommunityCleanupEventDetailResponse>))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> GetByIdAsync([FromRoute] Guid eventId, CancellationToken ct)
        => (await sender.Send(new GetCommunityCleanupByIdQuery(eventId), ct)).ToHttp();

    [HttpPost("{eventId:guid}/join")]
    [Authorize(Roles = "Citizen")]
    [SwaggerOperation(Summary = "[Citizen] Tham gia (= Vote)", Description = "Join = vào làm ngay, không cần duyệt.")]
    [SwaggerResponse(200, "Đã tham gia", typeof(ApiResponse))]
    [SwaggerResponse(422, "Đã đóng đăng ký, đã đủ người, hoặc đã tham gia", typeof(ApiResponse))]
    public async Task<IActionResult> JoinAsync([FromRoute] Guid eventId, CancellationToken ct)
        => (await sender.Send(new JoinCommunityCleanupCommand(eventId), ct)).ToHttpNoContent("Đã tham gia chương trình.");

    [HttpPost("{eventId:guid}/withdraw")]
    [Authorize(Roles = "Citizen")]
    [SwaggerOperation(Summary = "[Citizen] Rút khỏi chương trình", Description = "Chỉ trước khi check-in hoặc khi chương trình chưa InProgress.")]
    [SwaggerResponse(200, "Đã rút", typeof(ApiResponse))]
    [SwaggerResponse(422, "Không thể rút ở trạng thái hiện tại", typeof(ApiResponse))]
    public async Task<IActionResult> WithdrawAsync([FromRoute] Guid eventId, CancellationToken ct)
        => (await sender.Send(new WithdrawCommunityCleanupCommand(eventId), ct)).ToHttpNoContent("Đã rút khỏi chương trình.");

    [HttpGet("my")]
    [Authorize(Roles = "Citizen")]
    [SwaggerOperation(Summary = "[Citizen] Chương trình đã tham gia", Description = "Joined/CheckedIn/Withdrawn/NoShow của user hiện tại.")]
    [SwaggerResponse(200, "Danh sách", typeof(ApiResponse<CommunityCleanupListResponse>))]
    public async Task<IActionResult> GetMyAsync(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => (await sender.Send(new GetMyCommunityCleanupsQuery(page, pageSize), ct)).ToHttp();

    [HttpPost("{eventId:guid}/check-in")]
    [Authorize]
    [SwaggerOperation(Summary = "[Citizen/Leader] Check-in hiện trường", Description = "GPS ≤ 200m tới điểm tập trung (hoặc vị trí báo cáo).")]
    [SwaggerResponse(200, "Đã check-in", typeof(ApiResponse))]
    [SwaggerResponse(422, "Quá xa hiện trường hoặc trạng thái không hợp lệ", typeof(ApiResponse))]
    public async Task<IActionResult> CheckInAsync(
        [FromRoute] Guid eventId, [FromBody] CheckInCommunityCleanupRequest request, CancellationToken ct)
        => (await sender.Send(new CheckInCommunityCleanupCommand(eventId, request.Latitude, request.Longitude), ct))
            .ToHttpNoContent("Đã check-in thành công.");

    // ═══════════════════════════════════════════
    // ██  Leader — Mobile
    // ═══════════════════════════════════════════

    [HttpGet("led-by-me")]
    [Authorize(Roles = "Cleaner,Admin")]
    [SwaggerOperation(Summary = "[Leader] Chương trình tôi dẫn", Description = "Danh sách event mà user hiện tại là Leader.")]
    [SwaggerResponse(200, "Danh sách", typeof(ApiResponse<CommunityCleanupListResponse>))]
    public async Task<IActionResult> GetLedByMeAsync(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] CommunityCleanupStatus? status = null, CancellationToken ct = default)
        => (await sender.Send(new GetLedCommunityCleanupsQuery(page, pageSize, status), ct)).ToHttp();

    [HttpGet("{eventId:guid}/participants")]
    [Authorize]
    [SwaggerOperation(Summary = "[Leader/LEO] Danh sách participant đầy đủ", Description = "Chỉ Leader của event hoặc LEO/Admin được xem tên participant.")]
    [SwaggerResponse(200, "Danh sách", typeof(ApiResponse<CommunityCleanupParticipantsResponse>))]
    [SwaggerResponse(403, "Không có quyền", typeof(ApiResponse))]
    public async Task<IActionResult> GetParticipantsAsync(
        [FromRoute] Guid eventId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
        => (await sender.Send(new GetCommunityParticipantsQuery(eventId, page, pageSize), ct)).ToHttp();

    [HttpPost("{eventId:guid}/start")]
    [Authorize(Roles = "Cleaner,Admin")]
    [SwaggerOperation(Summary = "[Leader] Bắt đầu dọn dẹp", Description = "{OpenForJoin, JoinClosed} → InProgress.")]
    [SwaggerResponse(200, "Đã bắt đầu", typeof(ApiResponse))]
    [SwaggerResponse(403, "Không phải Leader của event", typeof(ApiResponse))]
    public async Task<IActionResult> StartAsync([FromRoute] Guid eventId, CancellationToken ct)
        => (await sender.Send(new StartCommunityCleanupCommand(eventId), ct)).ToHttpNoContent("Đã bắt đầu dọn dẹp.");

    [HttpPost("{eventId:guid}/before-images")]
    [Authorize(Roles = "Cleaner,Admin")]
    [SwaggerOperation(Summary = "[Leader] Upload ảnh before", Description = "Presign qua POST /v1/media/presign (purpose=Before, reportId), sau đó gửi URL về đây.")]
    [SwaggerResponse(200, "Đã lưu", typeof(ApiResponse<SubmitCommunityBeforeImagesResponse>))]
    public async Task<IActionResult> UploadBeforeImagesAsync(
        [FromRoute] Guid eventId, [FromBody] CommunityImageUrlsRequest request, CancellationToken ct)
        => (await sender.Send(new SubmitCommunityBeforeImagesCommand(eventId, request.ImageUrls ?? []), ct)).ToHttp();

    [HttpPut("{eventId:guid}/progress")]
    [Authorize(Roles = "Cleaner,Admin")]
    [SwaggerOperation(Summary = "[Leader] Cập nhật tiến độ", Description = "Presign ảnh progress qua POST /v1/media/presign (purpose=Progress, reportId).")]
    [SwaggerResponse(200, "Đã cập nhật", typeof(ApiResponse<UpdateCommunityProgressResponse>))]
    [SwaggerResponse(422, "Percent ngoài khoảng 0-100 hoặc status không hợp lệ", typeof(ApiResponse))]
    public async Task<IActionResult> UpdateProgressAsync(
        [FromRoute] Guid eventId, [FromBody] UpdateCommunityProgressRequest request, CancellationToken ct)
        => (await sender.Send(new UpdateCommunityProgressCommand(
            eventId, request.Percent, request.Note, request.ImageUrls ?? []), ct)).ToHttp();

    [HttpPost("{eventId:guid}/submit-verification")]
    [Authorize(Roles = "Cleaner,Admin")]
    [SwaggerOperation(Summary = "[Leader] Nộp xác thực hoàn thành", Description = "Yêu cầu progress 100%, ≥1 ảnh before + ≥2 ảnh after. InProgress → PendingVerification.")]
    [SwaggerResponse(200, "Đã nộp", typeof(ApiResponse))]
    [SwaggerResponse(422, "Thiếu bằng chứng hoặc tiến độ chưa đạt 100%", typeof(ApiResponse))]
    public async Task<IActionResult> SubmitVerificationAsync(
        [FromRoute] Guid eventId, [FromBody] CommunityImageUrlsRequest request, CancellationToken ct)
        => (await sender.Send(new SubmitCommunityVerificationCommand(eventId, request.ImageUrls ?? []), ct))
            .ToHttpNoContent("Đã nộp xác thực, chờ LEO duyệt.");
}

public sealed record CreateCommunityCleanupRequest(
    string Title,
    string? Description,
    Guid LeaderUserId,
    DateTime StartsAt,
    DateTime? EndsAt,
    DateTime? JoinClosesAt,
    int? MaxParticipants,
    string? MeetingNote,
    decimal? MeetingLatitude,
    decimal? MeetingLongitude);

public sealed record CancelCommunityCleanupRequest(string Reason);

public sealed record CheckInCommunityCleanupRequest(decimal Latitude, decimal Longitude);

public sealed record CommunityImageUrlsRequest(List<string>? ImageUrls);

public sealed record UpdateCommunityProgressRequest(int Percent, string? Note, List<string>? ImageUrls);
