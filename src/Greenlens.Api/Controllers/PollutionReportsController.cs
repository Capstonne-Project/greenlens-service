using Greenlens.Api.Extensions;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Reports.SubmitPollutionReport;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Greenlens.Api.Controllers;

[ApiController]
[Route("v1/pollution-reports")]
[Produces("application/json")]
public sealed class PollutionReportsController(ISender sender) : ControllerBase
{
    /// <summary>Submit a new pollution report (citizen; anonymous allowed).</summary>
    [HttpPost]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "[Citizen/Anonymous] Tạo báo cáo ô nhiễm",
        Description =
            "Tạo báo cáo mới. Hỗ trợ anonymous (không cần login). " +
            "Flow: (1) POST /v1/media/reports/images cho mỗi ảnh; " +
            "(2) POST endpoint này với images[] từ kết quả upload. " +
            "Hệ thống tự động gán SLA 24h và route báo cáo theo wardCode đến LocalOffice hoặc Department queue.")]
    [SwaggerResponse(201, "Report created", typeof(ApiResponse<SubmitPollutionReportResponse>))]
    [SwaggerResponse(400, "Authentication required for non-anonymous reports", typeof(ApiResponse))]
    [SwaggerResponse(400, "Invalid ward-province pair", typeof(ApiResponse))]
    [SwaggerResponse(404, "Category not found", typeof(ApiResponse))]
    [SwaggerResponse(422, "Validation", typeof(ApiResponse))]
    public async Task<IActionResult> SubmitAsync(
        [FromBody] SubmitPollutionReportCommand command,
        CancellationToken ct)
        => (await sender.Send(command, ct)).ToHttpCreated();
}
