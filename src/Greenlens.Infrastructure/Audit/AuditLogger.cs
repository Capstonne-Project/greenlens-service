using System.Text.Json;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Entities;
using Greenlens.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Infrastructure.Audit;

/// <summary>
/// Persists AuditLog entries to the database (BR-ADM-010).
/// Resolves IP address and User-Agent from HttpContext.
/// </summary>
internal sealed class AuditLogger(
    ApplicationDbContext db,
    ICurrentUser currentUser,
    IHttpContextAccessor httpContextAccessor) : IAuditLogger
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public async Task LogAsync(
        string action,
        string entityType,
        string? entityId,
        string? oldValues,
        string? newValues,
        CancellationToken ct = default)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = httpContext?.Request.Headers.UserAgent.ToString();

        var log = AuditLog.Create(
            userId: currentUser.UserId,
            action: action,
            entityType: entityType,
            entityId: entityId,
            oldValues: oldValues,
            newValues: newValues,
            ipAddress: ipAddress,
            userAgent: userAgent);

        db.AuditLogs.Add(log);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
