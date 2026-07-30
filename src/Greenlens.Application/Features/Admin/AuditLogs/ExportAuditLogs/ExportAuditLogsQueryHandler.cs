using System.Globalization;
using System.Text;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Admin.AuditLogs.ExportAuditLogs;

/// <remarks>Implements: BR-ADM-010, BR-OFF-022.</remarks>
public sealed class ExportAuditLogsQueryHandler(
    IApplicationDbContext db,
    ILogger<ExportAuditLogsQueryHandler> logger)
    : IRequestHandler<ExportAuditLogsQuery, Result<ExportAuditLogsResponse>>
{
    public async Task<Result<ExportAuditLogsResponse>> Handle(
        ExportAuditLogsQuery request,
        CancellationToken ct)
    {
        logger.LogInformation("Exporting audit logs from {From} to {To}", request.FromDate, request.ToDate);

        var fromUtc = DateTime.SpecifyKind(request.FromDate, DateTimeKind.Utc);
        var toUtc = DateTime.SpecifyKind(request.ToDate, DateTimeKind.Utc);

        var query = db.Set<AuditLog>()
            .AsNoTracking()
            .Where(a => a.CreatedAt >= fromUtc && a.CreatedAt <= toUtc);

        if (request.UserId.HasValue)
            query = query.Where(a => a.UserId == request.UserId.Value);

        if (request.ActorRole.HasValue)
            query = query.Where(a => a.User != null && a.User.Role == request.ActorRole.Value);

        if (!string.IsNullOrWhiteSpace(request.EntityType))
            query = query.Where(a => a.EntityType == request.EntityType);

        if (!string.IsNullOrWhiteSpace(request.Action))
            query = query.Where(a => a.Action.Contains(request.Action));

        var rows = await query
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new
            {
                a.Id,
                a.UserId,
                a.Action,
                a.EntityType,
                a.EntityId,
                a.IpAddress,
                a.CreatedAt
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var sb = new StringBuilder();
        sb.AppendLine("Id,UserId,Action,EntityType,EntityId,IpAddress,CreatedAtUtc");
        foreach (var r in rows)
        {
            sb.Append(Csv(r.Id));
            sb.Append(',');
            sb.Append(Csv(r.UserId));
            sb.Append(',');
            sb.Append(Csv(r.Action));
            sb.Append(',');
            sb.Append(Csv(r.EntityType));
            sb.Append(',');
            sb.Append(Csv(r.EntityId));
            sb.Append(',');
            sb.Append(Csv(r.IpAddress));
            sb.Append(',');
            sb.AppendLine(r.CreatedAt.ToString("O", CultureInfo.InvariantCulture));
        }

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmm", CultureInfo.InvariantCulture);
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();

        return new ExportAuditLogsResponse(bytes, "text/csv", $"audit_logs_{timestamp}.csv");
    }

    private static string Csv(object? value)
    {
        if (value is null)
            return string.Empty;

        var s = value.ToString() ?? string.Empty;
        if (s.Contains('"') || s.Contains(',') || s.Contains('\n'))
            return $"\"{s.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        return s;
    }
}
