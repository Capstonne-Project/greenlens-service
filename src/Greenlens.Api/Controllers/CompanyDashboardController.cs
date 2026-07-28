using Greenlens.Api.Extensions;
using Greenlens.Application.Features.Analytics.GetCompanyOverview;
using Greenlens.Application.Features.Analytics.GetCompanyQueueAging;
using Greenlens.Application.Features.Analytics.GetCompanyRecentActivities;
using Greenlens.Application.Features.Analytics.GetCompanyStaffPerformance;
using Greenlens.Application.Features.Analytics.GetCompanyTaskStatus;
using Greenlens.Application.Features.Analytics.GetCompanyTeamPerformance;
using Greenlens.Application.Features.Analytics.GetCompanyUpcomingDeadlines;
using Greenlens.Application.Features.Analytics.GetCompanyWorkloadTrend;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Greenlens.Api.Controllers;

/// <summary>Company dashboard — KPIs scoped to the caller's environmental service company.</summary>
[ApiController]
[Route("v1/dashboard/company")]
[Produces("application/json")]
[Authorize(Roles = "CompanyManager,Admin")]
[Tags("🏢 Company Dashboard")]
public sealed class CompanyDashboardController(ISender sender) : ControllerBase
{
    [HttpGet("overview")]
    [SwaggerOperation(Summary = "[CompanyManager] Company dashboard overview KPIs")]
    public async Task<IActionResult> GetOverviewAsync(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetCompanyOverviewQuery(from, to), ct)).ToHttp();

    [HttpGet("workload-trend")]
    [SwaggerOperation(Summary = "[CompanyManager] Daily dispatched vs completed task trend")]
    public async Task<IActionResult> GetWorkloadTrendAsync(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetCompanyWorkloadTrendQuery(from, to), ct)).ToHttp();

    [HttpGet("task-status")]
    [SwaggerOperation(Summary = "[CompanyManager] Assignment status distribution")]
    public async Task<IActionResult> GetTaskStatusAsync(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetCompanyTaskStatusQuery(from, to), ct)).ToHttp();

    [HttpGet("team-performance")]
    [SwaggerOperation(Summary = "[CompanyManager] Per-team performance KPIs")]
    public async Task<IActionResult> GetTeamPerformanceAsync(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetCompanyTeamPerformanceQuery(from, to), ct)).ToHttp();

    [HttpGet("staff-performance")]
    [SwaggerOperation(Summary = "[CompanyManager] Per-staff performance KPIs")]
    public async Task<IActionResult> GetStaffPerformanceAsync(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetCompanyStaffPerformanceQuery(from, to), ct)).ToHttp();

    [HttpGet("queue-aging")]
    [SwaggerOperation(Summary = "[CompanyManager] Company task queue age distribution")]
    public async Task<IActionResult> GetQueueAgingAsync(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetCompanyQueueAgingQuery(from, to), ct)).ToHttp();

    [HttpGet("recent-activities")]
    [SwaggerOperation(Summary = "[CompanyManager] Recent company task lifecycle events")]
    public async Task<IActionResult> GetRecentActivitiesAsync(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetCompanyRecentActivitiesQuery(from, to), ct)).ToHttp();

    [HttpGet("upcoming-deadlines")]
    [SwaggerOperation(Summary = "[CompanyManager] Tasks approaching their SLA deadline")]
    public async Task<IActionResult> GetUpcomingDeadlinesAsync(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetCompanyUpcomingDeadlinesQuery(from, to), ct)).ToHttp();
}
