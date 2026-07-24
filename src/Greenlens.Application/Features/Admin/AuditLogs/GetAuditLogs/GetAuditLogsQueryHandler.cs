using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace Greenlens.Application.Features.Admin.AuditLogs.GetAuditLogs;

/// <summary>
/// Returns a paginated list of audit log entries with optional filters.
/// </summary>
/// <remarks>Implements: BR-ADM-010.</remarks>
public sealed class GetAuditLogsQueryHandler(IApplicationDbContext db, 
    ILogger<GetAuditLogsQueryHandler> logger) : IRequestHandler<GetAuditLogsQuery, Result<GetAuditLogsResponse>>
{
    public async Task<Result<GetAuditLogsResponse>> Handle(
        GetAuditLogsQuery request,
        CancellationToken ct)
    {
        logger.LogInformation("Getting audit logs");

        var query = db.Set<AuditLog>()
            .AsNoTracking()
            .AsQueryable();

        if (request.UserId.HasValue)
        {
            query = query.Where(a => a.UserId == request.UserId.Value);
            logger.LogInformation("User ID: {UserId}", request.UserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.EntityType))
        {
            query = query.Where(a => a.EntityType == request.EntityType);
            logger.LogInformation("Entity type: {EntityType}", request.EntityType);
        }

        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            query = query.Where(a => a.Action.Contains(request.Action));
            logger.LogInformation("Action: {Action}", request.Action);
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(a => a.CreatedAt >= DateTime.SpecifyKind(request.FromDate.Value, DateTimeKind.Utc));
            logger.LogInformation("From date: {FromDate}", request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(a => a.CreatedAt <= DateTime.SpecifyKind(request.ToDate.Value, DateTimeKind.Utc));
            logger.LogInformation("To date: {ToDate}", request.ToDate.Value);
        }

        query = query.OrderByDescending(a => a.CreatedAt);
        logger.LogInformation("Query: {Query}", query.ToQueryString());

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Total count of audit logs: {TotalCount}", totalCount);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new AuditLogItem(
                a.Id,
                a.UserId,
                a.User != null ? a.User.Email : null,
                a.Action,
                a.EntityType,
                a.EntityId,
                a.IpAddress,
                a.UserAgent,
                a.CreatedAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        logger.LogInformation("Audit logs retrieved successfully");
        var pagination = PaginationMeta.Create(request.Page, request.PageSize, totalCount);
        logger.LogInformation("Pagination: {Pagination}", pagination);

        logger.LogInformation("Audit logs retrieved successfully");
        return Result<GetAuditLogsResponse>.Success(new GetAuditLogsResponse(items, pagination));
    }
}