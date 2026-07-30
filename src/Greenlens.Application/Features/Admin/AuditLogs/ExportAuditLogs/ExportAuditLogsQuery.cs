using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Admin.AuditLogs.ExportAuditLogs;

/// <remarks>Implements: BR-ADM-010, BR-OFF-022 (streaming export).</remarks>
public sealed record ExportAuditLogsQuery(
    DateTime FromDate,
    DateTime ToDate,
    Guid? UserId = null,
    UserRole? ActorRole = null,
    string? EntityType = null,
    string? Action = null) : IRequest<Result<ExportAuditLogsResponse>>;

public sealed record ExportAuditLogsResponse(
    byte[] Content,
    string ContentType,
    string FileName);
