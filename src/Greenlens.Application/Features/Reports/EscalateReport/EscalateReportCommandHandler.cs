using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.EscalateReport;

/// <summary>
/// BR-ORG-016: LEO manually escalates a report to the Department (DEO) queue.
/// Used when report is on a city-level route (CITENCO territory) or needs
/// higher-level coordination that LEO cannot handle.
/// </summary>
/// <remarks>
/// Preconditions:
/// - Report must be Verified or InProgress (LEO has already assessed it).
/// - Current user (LEO) must be assigned to the same office as the report.
/// Result: AssignedOfficeId = null → report appears in Department common queue for DEO.
/// </remarks>
public sealed class EscalateReportCommandHandler(
    IReportRepository reports,
    ICurrentUser currentUser,
    IUserRepository users,
    IUnitOfWork uow,
    ILogger<EscalateReportCommandHandler> logger)
    : IRequestHandler<EscalateReportCommand, Result>
{
    public async Task<Result> Handle(EscalateReportCommand request, CancellationToken ct)
    {
        logger.LogInformation("Escalating report {ReportId}", request.ReportId);

        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
        {
            logger.LogWarning("Report not found for ID {ReportId}", request.ReportId);
            return Errors.Reports.ReportNotFound;
        }

        // Only Verified or InProgress reports can be escalated
        if (report.Status is not (ReportStatus.Verified or ReportStatus.InProgress))
        {
            logger.LogWarning("Report {ReportId} is not verified or in progress", request.ReportId);
            return Errors.Reports.InvalidStatusTransition;
        }

        // BR-OFF-004: conflict of interest — cannot escalate own report
        if (report.ReporterId == currentUser.UserId)
        {
            logger.LogWarning("Report {ReportId} is reporter", request.ReportId);
            return Errors.Reports.ConflictOfInterest;
        }

        // LEO must belong to the same office
        var leo = await users.GetByIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (leo is null)
        {
            logger.LogWarning("LEO not found for ID {UserId}", currentUser.UserId);
            return Errors.Users.UserNotFound;
        }

        if (leo.LocalOfficeId != report.AssignedOfficeId)
        {
            logger.LogWarning("LEO {UserId} is not assigned to the same office as report {ReportId}", currentUser.UserId, request.ReportId);
            return Errors.Reports.OutsideJurisdiction;
        }

        // Escalate: clear office assignment → falls into Department queue
        report.EscalateToDepartment();

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "LEO {LeoId} escalated report {ReportId} to Department queue. Reason: {Reason}",
            currentUser.UserId, report.Id, request.Reason);

        return Result.Success();
    }
}
