using Greenlens.Api.Extensions;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Map.GetPublicMapReports;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Greenlens.Api.Controllers;

[ApiController]
[Route("v1/map")]
[Produces("application/json")]
public sealed class MapController(ISender sender) : ControllerBase
{
    /// <summary>Reports in map viewport (verified and later statuses only).</summary>
    [HttpGet("reports")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Public map — reports in bounding box",
        Description =
            "mode=detail: pin list with card preview (imageUrl, title, description, address, reporterCount, categoryIconUrl). " +
            "limit default 200, max 500. " +
            "mode=aggregate: grid cells with count and maxSeverity (gridLevel 1–5, default 3). " +
            "Coordinates in detail cells are rounded per BR-MAP-004.")]
    [SwaggerResponse(200, "Map data", typeof(ApiResponse<PublicMapReportsResponse>))]
    [SwaggerResponse(404, "Category not found", typeof(ApiResponse))]
    [SwaggerResponse(422, "Validation", typeof(ApiResponse))]
    public async Task<IActionResult> GetReportsInViewAsync(
        [FromQuery] GetPublicMapReportsQuery query,
        CancellationToken ct)
        => (await sender.Send(query, ct)).ToHttp();
}
