using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.CommunityCleanup.CancelCommunityCleanup;

/// <remarks>Draft rule BR-CMU-012.</remarks>
public sealed class CancelCommunityCleanupCommandHandler(
    ICommunityCleanupEventRepository events,
    ICommunityCleanupParticipantRepository participants,
    IReportRepository reports,
    IReportStatusHistoryRepository statusHistory,
    IUnitOfWork uow,
    ILogger<CancelCommunityCleanupCommandHandler> logger) : IRequestHandler<CancelCommunityCleanupCommand, Result>
{
    public async Task<Result> Handle(CancelCommunityCleanupCommand request, CancellationToken ct)
    {
        var ev = await events.GetByIdAsync(request.EventId, ct).ConfigureAwait(false);
        if (ev is null)
            return Errors.CommunityCleanup.EventNotFound;

        try
        {
            ev.Cancel(request.Reason);
        }
        catch (InvalidOperationException)
        {
            return Errors.CommunityCleanup.InvalidStatusTransition;
        }

        var report = await reports.GetByIdAsync(ev.ReportId, ct).ConfigureAwait(false);
        if (report is not null && report.Status == ReportStatus.InProgress)
        {
            report.RevertToVerified();
            statusHistory.Add(ReportStatusHistory.Create(
                report.Id, ReportStatus.InProgress, ReportStatus.Verified, reason: "Community cleanup cancelled: " + request.Reason));
        }

        var rows = await participants.GetByEventIdAsync(request.EventId, ct).ConfigureAwait(false);
        foreach (var p in rows.Where(p => p.Status is CommunityCleanupParticipantStatus.Joined or CommunityCleanupParticipantStatus.CheckedIn))
            p.ForceWithdraw();

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Community cleanup {EventId} cancelled: {Reason}", request.EventId, request.Reason);
        return Result.Success();
    }
}
