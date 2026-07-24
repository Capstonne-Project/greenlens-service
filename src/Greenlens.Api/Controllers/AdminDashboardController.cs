using Greenlens.Api.Extensions;
using Greenlens.Application.Features.Analytics.GetAdminAlerts;
using Greenlens.Application.Features.Analytics.GetAdminCompanyPerformance;
using Greenlens.Application.Features.Analytics.GetAdminGeographic;
using Greenlens.Application.Features.Analytics.GetAdminOfficerPerformance;
using Greenlens.Application.Features.Analytics.GetAdminOverview;
using Greenlens.Application.Features.Analytics.GetAdminPollutionAnalytics;
using Greenlens.Application.Features.Analytics.GetAdminQueueAging;
using Greenlens.Application.Features.Analytics.GetAdminRecentActivities;
using Greenlens.Application.Features.Analytics.GetAdminReportFunnel;
using Greenlens.Application.Features.Analytics.GetAdminReportStatusDistribution;
using Greenlens.Application.Features.Analytics.GetAdminReportTrend;
using Greenlens.Application.Features.Analytics.GetAdminResolutionDistribution;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Greenlens.Api.Controllers;

/// <summary>Admin dashboard — system-wide KPIs, trends, and operational alerts.</summary>
[ApiController]
[Route("v1/dashboard/admin")]
[Produces("application/json")]
[Authorize(Roles = "Admin")]
[Tags("📊 Admin Dashboard")]
public sealed class AdminDashboardController(ISender sender) : ControllerBase
{
    [HttpGet("overview")]
    [SwaggerOperation(Summary = "Dashboard overview KPIs")]
    public async Task<IActionResult> GetOverviewAsync(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetAdminOverviewQuery(from, to), ct)).ToHttp();

    [HttpGet("report-status")]
    [SwaggerOperation(Summary = "Report status distribution")]
    public async Task<IActionResult> GetReportStatusAsync(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetAdminReportStatusDistributionQuery(from, to), ct)).ToHttp();

    [HttpGet("report-trend")]
    [SwaggerOperation(Summary = "Report trend (created vs resolved)")]
    public async Task<IActionResult> GetReportTrendAsync(
        [FromQuery] ReportTrendGroupBy groupBy = ReportTrendGroupBy.Day,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetAdminReportTrendQuery(groupBy, from, to), ct)).ToHttp();

    [HttpGet("pollution-analytics")]
    [SwaggerOperation(Summary = "Report counts by pollution category")]
    public async Task<IActionResult> GetPollutionAnalyticsAsync(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetAdminPollutionAnalyticsQuery(from, to), ct)).ToHttp();

    [HttpGet("geographic")]
    [SwaggerOperation(Summary = "Geographic heatmap and markers")]
    public async Task<IActionResult> GetGeographicAsync(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetAdminGeographicQuery(from, to), ct)).ToHttp();

    [HttpGet("report-funnel")]
    [SwaggerOperation(Summary = "Report lifecycle funnel")]
    public async Task<IActionResult> GetReportFunnelAsync(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetAdminReportFunnelQuery(from, to), ct)).ToHttp();

    [HttpGet("company-performance")]
    [SwaggerOperation(Summary = "Company performance KPIs")]
    public async Task<IActionResult> GetCompanyPerformanceAsync(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetAdminCompanyPerformanceQuery(from, to), ct)).ToHttp();

    [HttpGet("officer-performance")]
    [SwaggerOperation(Summary = "Officer (LEO) performance KPIs")]
    public async Task<IActionResult> GetOfficerPerformanceAsync(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetAdminOfficerPerformanceQuery(from, to), ct)).ToHttp();

    [HttpGet("queue-aging")]
    [SwaggerOperation(Summary = "Pending queue age distribution")]
    public async Task<IActionResult> GetQueueAgingAsync(CancellationToken ct = default)
        => (await sender.Send(new GetAdminQueueAgingQuery(), ct)).ToHttp();

    [HttpGet("resolution-distribution")]
    [SwaggerOperation(Summary = "Resolution time histogram")]
    public async Task<IActionResult> GetResolutionDistributionAsync(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetAdminResolutionDistributionQuery(from, to), ct)).ToHttp();

    [HttpGet("recent-activities")]
    [SwaggerOperation(Summary = "Recent report lifecycle events")]
    public async Task<IActionResult> GetRecentActivitiesAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => (await sender.Send(new GetAdminRecentActivitiesQuery(page, pageSize), ct)).ToHttp();

    [HttpGet("alerts")]
    [SwaggerOperation(Summary = "System operational alerts")]
    public async Task<IActionResult> GetAlertsAsync(CancellationToken ct = default)
        => (await sender.Send(new GetAdminAlertsQuery(), ct)).ToHttp();
}
