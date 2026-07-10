using System.Text.Json;
using Greenlens.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that automatically writes audit log entries
/// for any command implementing <see cref="IAuditable"/>.
/// Captures the command payload as NewValues after successful execution.
/// </summary>
/// <remarks>
/// Implements: BR-ADM-010 — cross-cutting audit logging.
/// Order: runs AFTER ValidationBehavior and TransactionBehavior.
/// </remarks>
public sealed class AuditLogBehavior<TRequest, TResponse>(
    IAuditLogger auditLogger,
    ILogger<AuditLogBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IAuditable
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Execute the handler first
        var response = next();
        var result = await response.ConfigureAwait(false);

        // Only log if handler succeeded (we don't log failed operations)
        try
        {
            var newValues = JsonSerializer.Serialize(request, request.GetType(), JsonOpts);

            await auditLogger.LogAsync(
                action: request.AuditAction,
                entityType: request.AuditEntityType,
                entityId: request.AuditEntityId,
                oldValues: null, // Full diff requires entity-specific load — see below
                newValues: newValues,
                ct: cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Audit log failure should NOT break the business operation
            logger.LogError(ex, "Failed to write audit log for {Action} on {EntityType}/{EntityId}",
                request.AuditAction, request.AuditEntityType, request.AuditEntityId);
        }

        return result;
    }
}
