using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Admin.AuditLogs.GetAuditLogById;

/// <summary>
/// Get a single audit log entry with full OldValues/NewValues JSON.
/// </summary>
/// <remarks>Implements: BR-ADM-010.</remarks>
public sealed record GetAuditLogByIdQuery(Guid Id) : IRequest<Result<AuditLogDetailResponse>>;

public sealed record AuditLogDetailResponse(
    Guid Id,
    Guid UserId,
    string? UserEmail,
    UserRole? ActorRole,
    string Action,
    string EntityType,
    string? EntityId,
    string? OldValues,
    string? NewValues,
    string IpAddress,
    string? UserAgent,
    DateTime CreatedAt);
