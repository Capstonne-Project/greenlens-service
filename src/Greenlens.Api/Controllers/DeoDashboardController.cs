using Greenlens.Api.Extensions;
using Greenlens.Application.Features.Analytics.GetAdminReportTrend;
using Greenlens.Application.Features.Analytics.GetDeoAlerts;
using Greenlens.Application.Features.Analytics.GetDeoCompanyPerformance;
using Greenlens.Application.Features.Analytics.GetDeoGeographic;
using Greenlens.Application.Features.Analytics.GetDeoOfficerPerformance;
using Greenlens.Application.Features.Analytics.GetDeoOverview;
using Greenlens.Application.Features.Analytics.GetDeoPollutionAnalytics;
using Greenlens.Application.Features.Analytics.GetDeoQueueAging;
using Greenlens.Application.Features.Analytics.GetDeoRecentActivities;
using Greenlens.Application.Features.Analytics.GetDeoReportFunnel;
using Greenlens.Application.Features.Analytics.GetDeoReportStatusDistribution;
using Greenlens.Application.Features.Analytics.GetDeoReportTrend;
using Greenlens.Application.Features.Analytics.GetDeoResolutionDistribution;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Greenlens.Api.Controllers;

/// <summary>
/// DEO dashboard — province-scoped monitoring KPIs. Read-only for reports; no verify/dispatch actions.
/// </summary>
[ApiController]
[Route("v1/dashboard/deo")]
[Produces("application/json")]
[Authorize(Roles = "DEO")]
[Tags("🔍 DEO Dashboard")]
public sealed class DeoDashboardController(ISender sender) : ControllerBase
{
    [HttpGet("overview")]
    [SwaggerOperation(Summary = "Department overview KPIs (reports + org structure)")]
    public async Task<IActionResult> GetOverviewAsync(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetDeoOverviewQuery(from, to), ct)).ToHttp();

    [HttpGet("alerts")]
    [SwaggerOperation(Summary = "Operational alerts for the department (SLA, duplicates, contracts)")]
    public async Task<IActionResult> GetAlertsAsync(CancellationToken ct = default)
        => (await sender.Send(new GetDeoAlertsQuery(), ct)).ToHttp();

    [HttpGet("report-status")]
    [SwaggerOperation(Summary = "Report status distribution in department")]
    public async Task<IActionResult> GetReportStatusAsync(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetDeoReportStatusDistributionQuery(from, to), ct)).ToHttp();

    [HttpGet("report-trend")]
    [SwaggerOperation(Summary = "Report trend (created vs resolved) in department")]
    public async Task<IActionResult> GetReportTrendAsync(
        [FromQuery] ReportTrendGroupBy groupBy = ReportTrendGroupBy.Day,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetDeoReportTrendQuery(groupBy, from, to), ct)).ToHttp();

    [HttpGet("pollution-analytics")]
    [SwaggerOperation(Summary = "Report counts by pollution category in department")]
    public async Task<IActionResult> GetPollutionAnalyticsAsync(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetDeoPollutionAnalyticsQuery(from, to), ct)).ToHttp();

    [HttpGet("geographic")]
    [SwaggerOperation(Summary = "Geographic heatmap and markers in department")]
    public async Task<IActionResult> GetGeographicAsync(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetDeoGeographicQuery(from, to), ct)).ToHttp();

    [HttpGet("report-funnel")]
    [SwaggerOperation(Summary = "Report lifecycle funnel in department")]
    public async Task<IActionResult> GetReportFunnelAsync(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetDeoReportFunnelQuery(from, to), ct)).ToHttp();

    [HttpGet("company-performance")]
    [SwaggerOperation(Summary = "Environmental service company KPIs in department")]
    public async Task<IActionResult> GetCompanyPerformanceAsync(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetDeoCompanyPerformanceQuery(from, to), ct)).ToHttp();

    [HttpGet("officer-performance")]
    [SwaggerOperation(Summary = "LEO verification KPIs for reports in department")]
    public async Task<IActionResult> GetOfficerPerformanceAsync(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetDeoOfficerPerformanceQuery(from, to), ct)).ToHttp();

    [HttpGet("queue-aging")]
    [SwaggerOperation(Summary = "Pending report age distribution in department")]
    public async Task<IActionResult> GetQueueAgingAsync(CancellationToken ct = default)
        => (await sender.Send(new GetDeoQueueAgingQuery(), ct)).ToHttp();

    [HttpGet("resolution-distribution")]
    [SwaggerOperation(Summary = "Resolution time histogram in department")]
    public async Task<IActionResult> GetResolutionDistributionAsync(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetDeoResolutionDistributionQuery(from, to), ct)).ToHttp();

    [HttpGet("recent-activities")]
    [SwaggerOperation(Summary = "Recent report lifecycle events in department")]
    public async Task<IActionResult> GetRecentActivitiesAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => (await sender.Send(new GetDeoRecentActivitiesQuery(page, pageSize), ct)).ToHttp();
}
