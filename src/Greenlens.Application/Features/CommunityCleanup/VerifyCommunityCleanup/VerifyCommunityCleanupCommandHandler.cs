using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.CommunityCleanup.VerifyCommunityCleanup;

/// <remarks>Draft rule BR-CMU-010: LEO approve → Completed + Report Resolved.</remarks>
public sealed class VerifyCommunityCleanupCommandHandler(
    ICommunityCleanupEventRepository events,
    IReportRepository reports,
    IReportStatusHistoryRepository statusHistory,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<VerifyCommunityCleanupCommandHandler> logger) : IRequestHandler<VerifyCommunityCleanupCommand, Result>
{
    public async Task<Result> Handle(VerifyCommunityCleanupCommand request, CancellationToken ct)
    {
        var ev = await events.GetByIdAsync(request.EventId, ct).ConfigureAwait(false);
        if (ev is null)
            return Errors.CommunityCleanup.EventNotFound;

        if (ev.Status != CommunityCleanupStatus.PendingVerification)
            return Errors.CommunityCleanup.InvalidStatusTransition;

        var report = await reports.GetByIdAsync(ev.ReportId, ct).ConfigureAwait(false);
        if (report is null)
            return Errors.Reports.ReportNotFound;

        ev.Approve(currentUser.UserId);

        try
        {
            report.Resolve();
        }
        catch (InvalidOperationException)
        {
            return Errors.Reports.InvalidStatusTransition;
        }

        statusHistory.Add(ReportStatusHistory.Create(
            report.Id, ReportStatus.InProgress, ReportStatus.Resolved, currentUser.UserId));

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("LEO {LeoId} approved community cleanup {EventId}, report {ReportId} resolved",
            currentUser.UserId, request.EventId, report.Id);
        return Result.Success();
    }
}
