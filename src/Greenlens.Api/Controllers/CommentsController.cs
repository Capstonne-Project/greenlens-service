using Greenlens.Api.Attributes;
using Greenlens.Api.Extensions;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Comments.AddComment;
using Greenlens.Application.Features.Comments.DeleteComment;
using Greenlens.Application.Features.Comments.EditComment;
using Greenlens.Application.Features.Comments.GetReportComments;
using Greenlens.Application.Features.Comments.HideComment;
using Greenlens.Application.Features.Comments.ToggleCommentLike;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Greenlens.Api.Controllers;

/// <summary>Comment CRUD on pollution reports. BR-CMT-001..004.</summary>
[ApiController]
[Route("v1")]
[Authorize]
[Produces("application/json")]
[Tags("💬 Comments")]
public sealed class CommentsController(ISender sender) : ControllerBase
{
    [HttpGet("reports/{reportId:guid}/comments")]
    [SwaggerOperation(
        Summary = "[Auth] Danh sách bình luận trên báo cáo",
        Description = "Phân trang bình luận (kèm likeCount / likedByMe / parentCommentId). Citizen không thấy bình luận đã ẩn.")]
    [SwaggerResponse(200, "Danh sách bình luận", typeof(ApiResponse<GetReportCommentsResponse>))]
    [SwaggerResponse(404, "Báo cáo không tồn tại", typeof(ApiResponse))]
    public async Task<IActionResult> GetReportCommentsAsync(
        Guid reportId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => (await sender.Send(new GetReportCommentsQuery(reportId, page, pageSize), ct)).ToHttp();

    [HttpPost("reports/{reportId:guid}/comments")]
    [SupportsIdempotency]
    [SwaggerOperation(
        Summary = "[Auth] Thêm bình luận / trả lời",
        Description = "1–500 ký tự, tối đa 2 ảnh. parentCommentId = trả lời (TikTok-style, 1 cấp). " +
            "Báo cáo ẩn danh: đội xử lý / LEO / người gửi được comment.")]
    [SwaggerResponse(200, "Đã tạo bình luận", typeof(ApiResponse<AddCommentResponse>))]
    [SwaggerResponse(403, "Không được bình luận / chưa đăng nhập", typeof(ApiResponse))]
    [SwaggerResponse(422, "Nội dung không phù hợp hoặc bị khóa bình luận", typeof(ApiResponse))]
    public async Task<IActionResult> AddCommentAsync(
        Guid reportId,
        [FromBody] AddCommentRequest body,
        CancellationToken ct)
    {
        var command = new AddCommentCommand(reportId, body.Content, body.Images, body.ParentCommentId);
        return (await sender.Send(command, ct)).ToHttp("Đã thêm bình luận thành công.");
    }

    [HttpPost("comments/{commentId:guid}/like")]
    [SwaggerOperation(
        Summary = "[Auth] Thích / bỏ thích bình luận",
        Description = "Toggle like. Response: liked + likeCount.")]
    [SwaggerResponse(200, "Đã cập nhật like", typeof(ApiResponse<ToggleCommentLikeResponse>))]
    [SwaggerResponse(404, "Không tìm thấy bình luận", typeof(ApiResponse))]
    public async Task<IActionResult> ToggleLikeAsync(Guid commentId, CancellationToken ct)
        => (await sender.Send(new ToggleCommentLikeCommand(commentId), ct)).ToHttp();

    [HttpPut("comments/{commentId:guid}")]
    [SwaggerOperation(
        Summary = "[Citizen] Sửa bình luận của mình",
        Description = "Chỉ trong 15 phút sau khi đăng.")]
    [SwaggerResponse(200, "Đã cập nhật", typeof(ApiResponse<EditCommentResponse>))]
    [SwaggerResponse(403, "Không phải tác giả", typeof(ApiResponse))]
    [SwaggerResponse(422, "Hết thời gian sửa / nội dung không phù hợp", typeof(ApiResponse))]
    public async Task<IActionResult> EditCommentAsync(
        Guid commentId,
        [FromBody] EditCommentRequest body,
        CancellationToken ct)
        => (await sender.Send(new EditCommentCommand(commentId, body.Content), ct)).ToHttp();

    [HttpDelete("comments/{commentId:guid}")]
    [SwaggerOperation(
        Summary = "[Citizen] Xóa bình luận của mình",
        Description = "Soft-delete trong 15 phút sau khi đăng.")]
    [SwaggerResponse(204, "Đã xóa")]
    [SwaggerResponse(403, "Không phải tác giả", typeof(ApiResponse))]
    [SwaggerResponse(422, "Hết thời gian xóa", typeof(ApiResponse))]
    public async Task<IActionResult> DeleteCommentAsync(Guid commentId, CancellationToken ct)
        => (await sender.Send(new DeleteCommentCommand(commentId), ct)).ToHttpNoContent("Đã xóa bình luận.");

    [HttpPost("comments/{commentId:guid}/hide")]
    [Authorize(Roles = "LEO,Admin")]
    [SwaggerOperation(
        Summary = "[LEO] Ẩn bình luận vi phạm",
        Description = "Lý do tối thiểu 10 ký tự. BR-CMT-004.")]
    [SwaggerResponse(204, "Đã ẩn")]
    [SwaggerResponse(404, "Không tìm thấy bình luận", typeof(ApiResponse))]
    [SwaggerResponse(422, "Đã ẩn trước đó / lý do quá ngắn", typeof(ApiResponse))]
    public async Task<IActionResult> HideCommentAsync(
        Guid commentId,
        [FromBody] HideCommentRequest body,
        CancellationToken ct)
        => (await sender.Send(new HideCommentCommand(commentId, body.Reason), ct)).ToHttpNoContent("Đã ẩn bình luận.");
}

public sealed record AddCommentRequest(
    string Content,
    IReadOnlyList<AddCommentImageItem>? Images = null,
    Guid? ParentCommentId = null);

public sealed record EditCommentRequest(string Content);

public sealed record HideCommentRequest(string Reason);
