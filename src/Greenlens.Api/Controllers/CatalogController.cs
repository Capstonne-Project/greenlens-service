using Greenlens.Api.Extensions;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Catalog.GetPollutionCategories;
using Greenlens.Application.Features.Catalog.GetProvinceBoundary;
using Greenlens.Application.Features.Catalog.GetProvinces;
using Greenlens.Application.Features.Catalog.GetWardBoundary;
using Greenlens.Application.Features.Catalog.GetWardsByProvince;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Greenlens.Api.Controllers;

[ApiController]
[Route("v1/catalog")]
[Produces("application/json")]
[Tags("📚 Catalog — Reference Data")]
public sealed class CatalogController(ISender sender) : ControllerBase
{
    /// <summary>Active pollution categories for report submission.</summary>
    [HttpGet("pollution-categories")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "List pollution categories",
        Description = "Returns active categories (id, code, names, icon) for create-report dropdowns.")]
    [SwaggerResponse(200, "Category list", typeof(ApiResponse<GetPollutionCategoriesResponse>))]
    public async Task<IActionResult> GetPollutionCategoriesAsync(CancellationToken ct)
        => (await sender.Send(new GetPollutionCategoriesQuery(), ct)).ToHttp();

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

    /// <summary>Province boundary lookup directly by province code.</summary>
    [HttpGet("provinces/{provinceCode}/boundary")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Get province boundary by province code",
        Description =
            "Returns the province's geoJson (GeoJSON geometry from PostGIS) for map overlay, looked up " +
            "directly by provinceCode.")]
    [SwaggerResponse(200, "Province boundary", typeof(ApiResponse<GetProvinceBoundaryResponse>))]
    [SwaggerResponse(404, "Unknown province code", typeof(ApiResponse))]
    [SwaggerResponse(422, "Invalid province code format", typeof(ApiResponse))]
    public async Task<IActionResult> GetProvinceBoundaryAsync(
        [FromRoute] string provinceCode,
        CancellationToken ct)
        => (await sender.Send(new GetProvinceBoundaryQuery(provinceCode.Trim()), ct)).ToHttp();

    /// <summary>Ward boundary lookup directly by ward code (no province code required).</summary>
    [HttpGet("wards/{wardCode}/boundary")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Get ward boundary by ward code",
        Description =
            "Returns the ward's boundaryUrl (GeoJSON on CDN) for map overlay, looked up directly by " +
            "wardCode. Used by the LEO map, which only knows wardCode/wardName from the officer's own " +
            "office, not provinceCode.")]
    [SwaggerResponse(200, "Ward boundary", typeof(ApiResponse<GetWardBoundaryResponse>))]
    [SwaggerResponse(404, "Unknown ward code", typeof(ApiResponse))]
    [SwaggerResponse(422, "Invalid ward code format", typeof(ApiResponse))]
    public async Task<IActionResult> GetWardBoundaryAsync(
        [FromRoute] string wardCode,
        CancellationToken ct)
        => (await sender.Send(new GetWardBoundaryQuery(wardCode.Trim()), ct)).ToHttp();
}
