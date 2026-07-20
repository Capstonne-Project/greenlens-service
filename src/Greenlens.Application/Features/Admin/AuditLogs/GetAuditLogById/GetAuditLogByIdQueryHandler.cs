using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Admin.AuditLogs.GetAuditLogById;

/// <summary>
/// Returns a single audit log entry with full values for admin detail view.
/// </summary>
/// <remarks>Implements: BR-ADM-010.</remarks>
public sealed class GetAuditLogByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAuditLogByIdQuery, Result<AuditLogDetailResponse>>
{
    public async Task<Result<AuditLogDetailResponse>> Handle(
        GetAuditLogByIdQuery request,
        CancellationToken ct)
    {
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
            return Result<AuditLogDetailResponse>.Failure(
                new Error("AuditLog.NotFound", "Audit log entry not found.", ErrorType.NotFound));

        return log;
    }
}
