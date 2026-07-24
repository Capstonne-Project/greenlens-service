using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Admin.AuditLogs.GetAuditLogById;

/// <summary>
/// Returns a single audit log entry with full values for admin detail view.
/// </summary>
/// <remarks>Implements: BR-ADM-010.</remarks>
public sealed class GetAuditLogByIdQueryHandler(IApplicationDbContext db, 
    ILogger<GetAuditLogByIdQueryHandler> logger) : IRequestHandler<GetAuditLogByIdQuery, Result<AuditLogDetailResponse>>
{
    public async Task<Result<AuditLogDetailResponse>> Handle(
        GetAuditLogByIdQuery request,
        CancellationToken ct)
    {
        logger.LogInformation("Getting audit log for ID {Id}", request.Id);

        var log = await db.Set<AuditLog>()
            .AsNoTracking()
            .Where(a => a.Id == request.Id)
            .Select(a => new AuditLogDetailResponse(
                a.Id,
                a.UserId,
                a.User != null ? a.User.Email : null,
                a.Action,
                a.EntityType,
                a.EntityId,
                a.OldValues,
                a.NewValues,
                a.IpAddress,
                a.UserAgent,
                a.CreatedAt))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (log is null)
        {
            logger.LogWarning("Audit log not found for ID {Id}", request.Id);
            return Result<AuditLogDetailResponse>.Failure(Errors.Admin.AuditLogNotFound);
        }

        return log;
    }
}
