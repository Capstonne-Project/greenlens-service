using Greenlens.Api.Extensions;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Media.UploadReportImage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Greenlens.Api.Controllers;

[ApiController]
[Route("v1/media")]
[Authorize]
[Produces("application/json")]
public sealed class MediaController(ISender sender) : ControllerBase
{
    [HttpPost("reports/images")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Upload Report Image",
        Description =
            "Upload an image for a pollution report (jpg/png/webp/heic, max 10MB). Stored on R2 under reports/images. " +
            "Anonymous allowed so images can be uploaded before POST /v1/pollution-reports (pair with rate limiting BR-REP-010).")]
    [SwaggerResponse(200, "Image uploaded", typeof(ApiResponse<UploadReportImageResponse>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse))]
    [SwaggerResponse(422, "Invalid image type or too large", typeof(ApiResponse))]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadReportImageAsync(
        IFormFile file,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new ApiResponse
            {
                Code = "FILE_REQUIRED",
                Message = "Vui lòng chọn file ảnh.",
                Status = 400
            });

        await using var stream = file.OpenReadStream();

        var command = new UploadReportImageCommand(
            stream,
            file.FileName,
            file.ContentType,
            file.Length);

        return (await sender.Send(command, ct)).ToHttp();
    }
}
