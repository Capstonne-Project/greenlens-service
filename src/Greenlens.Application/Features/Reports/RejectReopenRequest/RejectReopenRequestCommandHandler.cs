using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Notifications;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.RejectReopenRequest;

/// <summary>LEO rejects citizen reopen request. Report stays Resolved (BR-REP-015, BR-REP-022).</summary>
/// <remarks>
/// Implements: BR-REP-015, BR-REP-022, BR-ORG-012, BR-NTF-002.
/// </remarks>
public sealed class RejectReopenRequestCommandHandler(
    IReportRepository reports,
    IApplicationDbContext db,
    IUserRepository users,
    INotificationService notifications,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<RejectReopenRequestCommandHandler> logger) : IRequestHandler<RejectReopenRequestCommand, Result>
{
    public async Task<Result> Handle(RejectReopenRequestCommand request, CancellationToken ct)
    {
        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
            return Errors.Reports.ReportNotFound;

        var reopenRequest = await db.Set<ReportReopenRequest>()
            .FirstOrDefaultAsync(r => r.Id == request.RequestId && r.ReportId == request.ReportId, ct)
            .ConfigureAwait(false);

        if (reopenRequest is null)
        {
            logger.LogWarning("Reopen request not found for ID {RequestId}", request.RequestId);
            return Errors.Reports.ReopenRequestNotFound;
        }

        if (reopenRequest.Status != ReopenRequestStatus.Pending)
        {
            logger.LogWarning("Reopen request {RequestId} is not pending", request.RequestId);
            return Errors.Reports.ReopenRequestNotPending;
        }

        var scopeError = await ReopenRequestAuthorization.ValidateLeoScopeAsync(
            report, users, currentUser, ct).ConfigureAwait(false);
        
        if (scopeError is not null)
        {
            logger.LogWarning("Invalid LEO scope for report {ReportId}", report.Id);
            return scopeError;
        }

        reopenRequest.Reject(currentUser.UserId, request.Reason);
        report.ClearPendingReopenRequest();

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        if (report.ReporterId.HasValue)
        {
            await notifications.SendFromTemplateAsync(
                report.ReporterId.Value,
                NotificationType.ReopenRequestDecided,
                NotificationPlaceholders.ForReopenDecided(report.Code, approved: false, request.Reason),
                report.Id,
                ct).ConfigureAwait(false);
        }

        logger.LogInformation(
            "LEO {UserId} rejected reopen request {RequestId} for report {ReportId}",
            currentUser.UserId, request.RequestId, report.Id);

        return Result.Success();
    }
}
