using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Application.Features.Reports.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.GetCompanyQueue;

/// <summary>
/// Returns reports dispatched to the caller's company that are awaiting team assignment.
/// Filters: Status == InProgress AND AssignedCompanyId == caller's companyId AND no active assignments.
/// </summary>
/// <remarks>Implements: BR-CMP-005, BR-CMP-021.</remarks>
public sealed class GetCompanyQueueQueryHandler(
    IReportRepository reports,
    IReportMediaRepository reportMedia,
    ICompanyStaffRepository companyStaff,
    ICurrentUser currentUser,
    ILogger<GetCompanyQueueQueryHandler> logger) : IRequestHandler<GetCompanyQueueQuery, Result<GetCompanyQueueResponse>>
{
    public async Task<Result<GetCompanyQueueResponse>> Handle(GetCompanyQueueQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting company queue for user {UserId}", currentUser.UserId);

        var companyIdResult = await CompanyContextResolver
            .ResolveCompanyIdAsync(companyStaff, currentUser.UserId, ct)
            .ConfigureAwait(false);
        if (companyIdResult.IsFailure)
            return companyIdResult.Error!;

        var companyId = companyIdResult.Value;
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        logger.LogInformation("Company ID: {CompanyId}", companyId);

        var baseQuery = reports.QueryAsNoTracking()
            .Include(r => r.Category)
            .Include(r => r.VerifiedByUser)
            .Include(r => r.DispatchedByUser)
            .Include(r => r.AssignedOffice!)
                .ThenInclude(o => o.Ward)
            .Where(r => r.Status == ReportStatus.InProgress
                        && r.AssignedCompanyId == companyId
                        && !r.Assignments.Any(a =>
                            a.Status == AssignmentStatus.Assigned
                            || a.Status == AssignmentStatus.InProgress));

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var keyword = request.Search.Trim().ToLowerInvariant();
            logger.LogInformation("Search: {Search}", request.Search);
            baseQuery = baseQuery.Where(r =>
                r.Code.ToLower().Contains(keyword) ||
                (r.Address != null && r.Address.ToLower().Contains(keyword)) ||
                (r.WardCode != null && r.WardCode.ToLower().Contains(keyword)) ||
                r.Category.NameVi.ToLower().Contains(keyword) ||
                r.Category.NameEn.ToLower().Contains(keyword));
        }

        if (request.Severity.HasValue)
        {
            logger.LogInformation("Filtering by severity: {Severity}", request.Severity.Value);
            baseQuery = baseQuery.Where(r => r.Severity == request.Severity.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.WardCode))
        {
            var ward = request.WardCode.Trim();
            logger.LogInformation("Filtering by ward: {WardCode}", ward);
            baseQuery = baseQuery.Where(r => r.WardCode == ward);
        }

        if (request.CategoryId.HasValue)
        {
            logger.LogInformation("Filtering by category: {CategoryId}", request.CategoryId.Value);
            baseQuery = baseQuery.Where(r => r.CategoryId == request.CategoryId.Value);
        }

        if (request.FromDate.HasValue)
        {
            var from = DateTime.SpecifyKind(request.FromDate.Value.Date, DateTimeKind.Utc);
            logger.LogInformation("From date: {FromDate}", from);
            baseQuery = baseQuery.Where(r =>
                r.DispatchedToCompanyAt != null && r.DispatchedToCompanyAt >= from);
        }

        if (request.ToDate.HasValue)
        {
            var toExclusive = DateTime.SpecifyKind(request.ToDate.Value.Date.AddDays(1), DateTimeKind.Utc);
            logger.LogInformation("To date (exclusive): {ToDate}", toExclusive);
            baseQuery = baseQuery.Where(r =>
                r.DispatchedToCompanyAt != null && r.DispatchedToCompanyAt < toExclusive);
        }

        var total = await baseQuery.CountAsync(ct).ConfigureAwait(false);
        var pagination = PaginationMeta.Create(page, pageSize, total);

        var sortBy = request.SortBy?.Trim().ToLowerInvariant();
        logger.LogInformation("Sort by: {SortBy}", sortBy);
        var orderedQuery = sortBy switch
        {
            "code" => request.SortDesc
                ? baseQuery.OrderByDescending(r => r.Code)
                : baseQuery.OrderBy(r => r.Code),
            "severity" => request.SortDesc
                ? baseQuery.OrderByDescending(r => r.Severity)
                : baseQuery.OrderBy(r => r.Severity),
            "dispatchedat" => request.SortDesc
                ? baseQuery.OrderByDescending(r => r.DispatchedToCompanyAt)
                : baseQuery.OrderBy(r => r.DispatchedToCompanyAt),
            "verifiedat" => request.SortDesc
                ? baseQuery.OrderByDescending(r => r.VerifiedAt)
                : baseQuery.OrderBy(r => r.VerifiedAt),
            "createdat" => request.SortDesc
                ? baseQuery.OrderByDescending(r => r.CreatedAt)
                : baseQuery.OrderBy(r => r.CreatedAt),
            "slaresolvedueat" => request.SortDesc
                ? baseQuery.OrderByDescending(r => r.SlaResolveDueAt)
                : baseQuery.OrderBy(r => r.SlaResolveDueAt),
            "priorityscore" => request.SortDesc
                ? baseQuery.OrderByDescending(r => r.PriorityScore)
                : baseQuery.OrderBy(r => r.PriorityScore),
            _ => baseQuery.OrderByDescending(r => r.PriorityScore).ThenByDescending(r => r.DispatchedToCompanyAt)
        };

        var rows = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new QueueRow(
                r.Id,
                r.Code,
                r.Address,
                r.WardCode,
                r.ProvinceCode,
                r.Latitude,
                r.Longitude,
                r.Category.NameVi,
                r.Severity,
                r.DispatchedToCompanyAt,
                r.VerifiedAt,
                r.VerifiedByUser != null ? r.VerifiedByUser.FullName : null,
                r.SlaResolveDueAt,
                r.AssignedOfficeId,
                r.AssignedOffice != null ? r.AssignedOffice.Name : null,
                r.AssignedOffice != null && r.AssignedOffice.Ward != null ? r.AssignedOffice.Ward.Name : null,
                r.DispatchedByOfficerId ?? r.VerifiedBy,
                r.DispatchedByUser != null
                    ? r.DispatchedByUser.FullName
                    : r.VerifiedByUser != null ? r.VerifiedByUser.FullName : null))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var reportIds = rows.Select(r => r.ReportId).ToList();
        var firstMediaByReportId = await CitizenReportMediaLoader
            .LoadFirstByReportIdsAsync(reportMedia, reportIds, ct)
            .ConfigureAwait(false);

        var items = rows.Select(r => new CompanyQueueItem(
            r.ReportId,
            r.Code,
            r.Address,
            r.WardCode,
            r.ProvinceCode,
            r.Latitude,
            r.Longitude,
            r.CategoryName,
            r.Severity,
            r.DispatchedAt,
            r.VerifiedAt,
            r.VerifiedByName,
            r.SlaResolveDueAt,
            CitizenReportMediaLoader.GetFirstMediaList(firstMediaByReportId, r.ReportId),
            new CompanyDispatchSourceDto(
                r.LocalOfficeId,
                r.LocalOfficeName,
                r.WardCode,
                r.WardName,
                r.LeoUserId,
                r.LeoFullName)))
            .ToList();

        logger.LogInformation(
            "CompanyManager {UserId} viewed queue: {Count}/{Total} reports for company {CompanyId}",
            currentUser.UserId, items.Count, total, companyId);

        return new GetCompanyQueueResponse(items, pagination);
    }

    private sealed record QueueRow(
        Guid ReportId,
        string Code,
        string? Address,
        string? WardCode,
        string? ProvinceCode,
        decimal Latitude,
        decimal Longitude,
        string CategoryName,
        Severity Severity,
        DateTime? DispatchedAt,
        DateTime? VerifiedAt,
        string? VerifiedByName,
        DateTime? SlaResolveDueAt,
        Guid? LocalOfficeId,
        string? LocalOfficeName,
        string? WardName,
        Guid? LeoUserId,
        string? LeoFullName);
}
