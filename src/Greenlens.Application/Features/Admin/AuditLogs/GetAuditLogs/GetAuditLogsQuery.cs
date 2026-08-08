using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Admin.AuditLogs.GetAuditLogs;

/// <summary>
/// List audit log entries with pagination and filters.
/// </summary>
/// <remarks>Implements: BR-ADM-010.</remarks>
public sealed record GetAuditLogsQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? UserId = null,
    UserRole? ActorRole = null,
    string? EntityType = null,
    string? EntityId = null,
    string? Action = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null) : IRequest<Result<GetAuditLogsResponse>>;

public sealed record GetAuditLogsResponse(
    List<AuditLogItem> Items,
    PaginationMeta Pagination);

public sealed record AuditLogItem(
    Guid Id,
    Guid UserId,
    string? UserEmail,
    UserRole? ActorRole,
    string Action,
    string EntityType,
    string? EntityId,
    string IpAddress,
    string? UserAgent,
    DateTime CreatedAt);