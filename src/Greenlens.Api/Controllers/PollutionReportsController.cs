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
        Summary = "Submit pollution report",
        Description =
            "Flow: (1) POST /v1/media/reports/images for each photo (authorized); " +
            "(2) POST here with `images[]` filled from upload response `url` plus client `mimeType` and `sizeBytes`. " +
            "Creates a report in Submitted status. Use isAnonymous=true without login; " +
            "otherwise send Bearer token and set isAnonymous=false to attach reporter.")]
    [SwaggerResponse(201, "Report created", typeof(ApiResponse<SubmitPollutionReportResponse>))]
    [SwaggerResponse(404, "Category not found", typeof(ApiResponse))]
    [SwaggerResponse(422, "Validation", typeof(ApiResponse))]
    public async Task<IActionResult> SubmitAsync(
        [FromBody] SubmitPollutionReportCommand command,
        CancellationToken ct)
        => (await sender.Send(command, ct)).ToHttpCreated();
}
