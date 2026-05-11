using Greenlens.Api.Extensions;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Catalog.GetProvinces;
using Greenlens.Application.Features.Catalog.GetWardsByProvince;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Greenlens.Api.Controllers;

[ApiController]
[Route("v1/catalog")]
[Produces("application/json")]
public sealed class CatalogController(ISender sender) : ControllerBase
{
    /// <summary>All provinces / centrally governed cities (address level 1).</summary>
    [HttpGet("provinces")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "List provinces",
        Description =
            "Returns official 2-digit codes, Vietnamese names, and optional boundaryUrl (GeoJSON on CDN) " +
            "for drawing province polygons on the client map.")]
    [SwaggerResponse(200, "Province list", typeof(ApiResponse<GetProvincesResponse>))]
    public async Task<IActionResult> GetProvincesAsync(CancellationToken ct)
        => (await sender.Send(new GetProvincesQuery(), ct)).ToHttp();

    /// <summary>Wards/communes for one province (address level 2).</summary>
    [HttpGet("provinces/{provinceCode}/wards")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "List wards by province",
        Description =
            "Returns official 5-digit ward codes, unit type label, and optional boundaryUrl (GeoJSON) " +
            "for ward polygons. Call after user selects a province.")]
    [SwaggerResponse(200, "Ward list", typeof(ApiResponse<GetWardsByProvinceResponse>))]
    [SwaggerResponse(404, "Unknown province code", typeof(ApiResponse))]
    [SwaggerResponse(422, "Invalid province code format", typeof(ApiResponse))]
    public async Task<IActionResult> GetWardsByProvinceAsync(
        [FromRoute] string provinceCode,
        CancellationToken ct)
        => (await sender.Send(new GetWardsByProvinceQuery(provinceCode.Trim()), ct)).ToHttp();
}
