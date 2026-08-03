using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Reports.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.GetViolationRecurrenceCandidates;

/// <summary>
/// Returns paginated reports flagged as suspected violation recurrence for LEO triage.
/// </summary>
/// <remarks>
/// Implements: BR-REP-034 (recurrence flag on submit), BR-OFF-005 (LEO review queue support).
/// </remarks>
public sealed class GetViolationRecurrenceCandidatesQueryHandler(
    IReportRepository reports,
    IReportMediaRepository reportMedia,
    ILogger<GetViolationRecurrenceCandidatesQueryHandler> logger)
    : IRequestHandler<GetViolationRecurrenceCandidatesQuery, Result<GetViolationRecurrenceCandidatesResponse>>
{
    public async Task<Result<GetViolationRecurrenceCandidatesResponse>> Handle(
        GetViolationRecurrenceCandidatesQuery request,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var query = reports.QueryAsNoTracking()
            .Where(r => r.IsSuspectedViolationRecurrence)
            .Where(r => r.Status != ReportStatus.Duplicate && r.Status != ReportStatus.Rejected);

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);
        var pagination = PaginationMeta.Create(request.Page, request.PageSize, totalCount);

        var rows = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new Row(
                r.Id,
                r.Code,
                r.Category.NameVi,
                r.Severity,
                r.Status,
                r.Latitude,
                r.Longitude,
                r.Address,
                r.CreatedAt,
                r.SuspectedRecurrenceOfReportId))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var reportIds = rows
            .Select(r => r.Id)
            .Concat(rows.Where(r => r.SuspectedRecurrenceOfReportId.HasValue).Select(r => r.SuspectedRecurrenceOfReportId!.Value))
            .Distinct()
            .ToList();

        var mediaByReportId = await CitizenReportMediaLoader.LoadByReportIdsAsync(reportMedia, reportIds, ct)
            .ConfigureAwait(false);

        var priorIds = rows
            .Where(r => r.SuspectedRecurrenceOfReportId.HasValue)
            .Select(r => r.SuspectedRecurrenceOfReportId!.Value)
            .Distinct()
            .ToList();

        var priors = priorIds.Count == 0
            ? []
            : await reports.QueryAsNoTracking()
                .Where(p => priorIds.Contains(p.Id))
                .Select(p => new PriorRow(p.Id, p.Code, p.Address, p.Status, p.ClosedAt))
                .ToListAsync(ct)
                .ConfigureAwait(false);

        var priorById = priors.ToDictionary(p => p.Id);

        var items = rows.Select(r =>
        {
            ViolationRecurrencePriorReport? prior = null;
            if (r.SuspectedRecurrenceOfReportId is { } pid && priorById.TryGetValue(pid, out var p))
            {
                prior = new ViolationRecurrencePriorReport(
                    p.Id,
                    p.Code,
                    p.Address,
                    p.Status,
                    p.ClosedAt,
                    p.ClosedAt.HasValue
                        ? (int)Math.Floor((now - p.ClosedAt.Value).TotalDays)
                        : null,
                    CitizenReportMediaLoader.GetMediaOrEmpty(mediaByReportId, p.Id));
            }

            return new ViolationRecurrenceCandidateItem(
                r.Id,
                r.Code,
                r.CategoryName,
                r.Severity,
                r.Status,
                r.Latitude,
                r.Longitude,
                r.Address,
                r.CreatedAt,
                CitizenReportMediaLoader.GetMediaOrEmpty(mediaByReportId, r.Id),
                prior);
        }).ToList();

        logger.LogInformation("Lấy danh sách nghi ngờ tái phạm vi phạm. Số lượng: {Count}", items.Count);

        return new GetViolationRecurrenceCandidatesResponse(items, pagination);
    }

    private sealed record Row(
        Guid Id,
        string Code,
        string CategoryName,
        Severity Severity,
        ReportStatus Status,
        decimal Latitude,
        decimal Longitude,
        string? Address,
        DateTime CreatedAt,
        Guid? SuspectedRecurrenceOfReportId);

    private sealed record PriorRow(
        Guid Id,
        string Code,
        string? Address,
        ReportStatus Status,
        DateTime? ClosedAt);
}
