using Greenlens.Api.Extensions;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Reports.GetDuplicateCandidates;
using Greenlens.Application.Features.Reports.GetDuplicateCandidatesV2;
using Greenlens.Application.Features.Reports.GetOfficerQueue;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Greenlens.Api.Controllers;

/// <summary>Reports API v2 — grouped / enriched read models.</summary>
[ApiController]
[Route("v2/reports")]
[Produces("application/json")]
public sealed class ReportsV2Controller(ISender sender) : ControllerBase
{
    [HttpGet("duplicate-candidates")]
    [Authorize(Roles = "LEO,DEO,Admin")]
    [Tags("📌 LEO Dashboard")]
    [SwaggerOperation(
        Summary = "[LEO/DEO] Danh sách nghi ngờ trùng lặp (nhóm theo báo cáo gốc)",
        Description = "BR-REP-031 v2: Trả về các báo cáo gốc (primary) kèm toàn bộ báo cáo bị gắn cờ possible_duplicate " +
            "trùng với báo cáo gốc đó. Hỗ trợ filter primaryReportId và các filter/search/sort giống v1. " +
            "Pagination áp dụng trên số nhóm báo cáo gốc, không phải từng báo cáo trùng riêng lẻ.")]
    [SwaggerResponse(200, "Danh sách nhóm nghi ngờ trùng lặp", typeof(ApiResponse<GetDuplicateCandidatesV2Response>))]
    public async Task<IActionResult> GetDuplicateCandidatesAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? primaryReportId = null,
        [FromQuery] ReportStatus? status = null,
        [FromQuery] Severity? severity = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] string? wardCode = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? search = null,
        [FromQuery] string? duplicateDetectionSource = null,
        [FromQuery] decimal? minAiSimilarityScore = null,
        [FromQuery] DuplicateCandidateSortBy sortBy = DuplicateCandidateSortBy.CreatedAt,
        [FromQuery] SortDirection sortDir = SortDirection.Desc,
        CancellationToken ct = default)
        => (await sender.Send(new GetDuplicateCandidatesV2Query(
            page,
            pageSize,
            primaryReportId,
            status,
            severity,
            categoryId,
            wardCode,
            fromDate,
            toDate,
            search,
            duplicateDetectionSource,
            minAiSimilarityScore,
            sortBy,
            sortDir), ct)).ToHttp();
}
