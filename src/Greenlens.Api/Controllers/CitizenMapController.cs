using Greenlens.Api.Extensions;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.CitizenMap.GetCitizenMapProvinces;
using Greenlens.Application.Features.CitizenMap.GetCitizenMapWardReports;
using Greenlens.Application.Features.CitizenMap.GetCitizenMapWards;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Greenlens.Api.Controllers;

/// <summary>
/// Dedicated endpoints for the citizen-facing drill-down map (province overview → ward risk
/// levels → ward report pins). Intentionally separate from <see cref="MapController"/>'s viewport
/// endpoints — this flow is code/boundary driven (province/ward code), not bbox-driven.
/// </summary>
[ApiController]
[Route("v1/citizen-map")]
[Produces("application/json")]
[Tags("🗺️ Citizen Map — Province/Ward Drill-down")]
public sealed class CitizenMapController(ISender sender) : ControllerBase
{
    /// <summary>Bước 1: toàn bộ tỉnh/thành kèm boundary GeoJSON.</summary>
    [HttpGet("provinces")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Citizen map — all provinces with boundary",
        Description =
            "Returns every province with its boundary geoJson for the initial national overview. " +
            "FE assigns each province a fixed color by code (no data-driven meaning).")]
    [SwaggerResponse(200, "Province list", typeof(ApiResponse<GetCitizenMapProvincesResponse>))]
    public async Task<IActionResult> GetProvincesAsync(CancellationToken ct)
        => (await sender.Send(new GetCitizenMapProvincesQuery(), ct)).ToHttp();

    /// <summary>Bước 2: phường/xã của 1 tỉnh, kèm boundary + mức rủi ro 5 cấp.</summary>
    [HttpGet("provinces/{provinceCode}/wards")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Citizen map — wards in a province with risk level",
        Description =
            "Returns every ward in the province with boundary geoJson and a 5-tier risk level " +
            "(1 None – 5 Critical) + colorHex computed from active report count. Call after the " +
            "user clicks a province on the map.")]
    [SwaggerResponse(200, "Ward list", typeof(ApiResponse<GetCitizenMapWardsResponse>))]
    [SwaggerResponse(404, "Unknown province code", typeof(ApiResponse))]
    [SwaggerResponse(422, "Invalid province code format", typeof(ApiResponse))]
    public async Task<IActionResult> GetWardsAsync(
        [FromRoute] string provinceCode,
        CancellationToken ct)
        => (await sender.Send(new GetCitizenMapWardsQuery(provinceCode.Trim()), ct)).ToHttp();

    /// <summary>Bước 3: điểm báo cáo ô nhiễm thuộc 1 phường/xã.</summary>
    [HttpGet("wards/{wardCode}/reports")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Citizen map — report pins in a ward",
        Description =
            "Returns pollution report pins (card preview fields included) for one ward. Call only " +
            "after the user clicks a specific ward, not before.")]
    [SwaggerResponse(200, "Report pin list", typeof(ApiResponse<GetCitizenMapWardReportsResponse>))]
    [SwaggerResponse(404, "Unknown ward code", typeof(ApiResponse))]
    [SwaggerResponse(422, "Invalid ward code format", typeof(ApiResponse))]
    public async Task<IActionResult> GetWardReportsAsync(
        [FromRoute] string wardCode,
        CancellationToken ct)
        => (await sender.Send(new GetCitizenMapWardReportsQuery(wardCode.Trim()), ct)).ToHttp();
}
