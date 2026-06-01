using Greenlens.Api.Extensions;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Map.GetMapViewportSummary;
using Greenlens.Application.Features.Map.GetPublicMapReports;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Greenlens.Api.Controllers;

[ApiController]
[Route("v1/map")]
[Produces("application/json")]
[Tags("🗺️ Map — Public Map")]
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

    /// <summary>Report count and daily trend for the home "Khu vực đang xem" card.</summary>
    [HttpGet("summary")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Public map — viewport summary (report count + chart)",
        Description =
            "Returns total public reports in the bounding box over the last N days (default 30) " +
            "and daily counts for the home chart. Same visibility rules as GET /map/reports (Verified+). " +
            "Use the same bbox as GET /map/reports when the user pans/zooms.")]
    [SwaggerResponse(200, "Viewport summary", typeof(ApiResponse<MapViewportSummaryResponse>))]
    [SwaggerResponse(404, "Category not found", typeof(ApiResponse))]
    [SwaggerResponse(422, "Validation", typeof(ApiResponse))]
    public async Task<IActionResult> GetViewportSummaryAsync(
        [FromQuery] GetMapViewportSummaryQuery query,
        CancellationToken ct)
        => (await sender.Send(query, ct)).ToHttp();
}
