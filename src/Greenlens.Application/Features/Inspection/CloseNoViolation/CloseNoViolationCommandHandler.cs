using System.Text.Json;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Notifications;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Inspection.CloseNoViolation;

/// <summary>BR-INS-013: Close inspection — no violation found. BR-ADM-010.</summary>
public sealed class CloseNoViolationCommandHandler(
    IInspectionReportRepository inspections,
    IReportRepository reports,
    ITeamMemberRepository teamMembers,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    IAuditLogger auditLogger,
    IInspectionClosedNoViolationNotifier closedNotifier,
    IInspectionAssignmentActivityNotifier activityNotifier,
    ILogger<CloseNoViolationCommandHandler> logger)
    : IRequestHandler<CloseNoViolationCommand, Result>
{
    public async Task<Result> Handle(CloseNoViolationCommand request, CancellationToken ct)
    {
        var inspection = await inspections.GetByIdAsync(request.InspectionId, ct).ConfigureAwait(false);
        if (inspection is null)
        {
            logger.LogWarning("Inspection not found for inspection {InspectionId}", request.InspectionId);
            return Errors.Inspections.InspectionNotFound;
        }

        var authError = await InspectionTeamAuthorization.ValidateTeamLeaderAsync(
            inspection, teamMembers, currentUser, ct).ConfigureAwait(false);
        if (authError is not null)
        {
            logger.LogWarning("Team leader validation failed for inspection {InspectionId}", request.InspectionId);
            return authError;
        }

        var oldSnapshot = JsonSerializer.Serialize(new { status = inspection.Status.ToString() });

        var result = inspection.CloseNoViolation(request.Reason);
        if (result.IsFailure)
        {
            logger.LogWarning("Close no violation failed for inspection {InspectionId}. Result: {Result}", request.InspectionId, result.Error);
            return result;
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        var report = await reports.GetByIdAsync(inspection.ReportId, ct).ConfigureAwait(false);
        if (report is not null)
        {
            if (inspection.AssignedTeamId is Guid teamId)
            {
                await activityNotifier.NotifyCompletedAsync(
                    inspection.CreatedByOfficerId,
                    teamId,
                    report.Id,
                    inspection.Id,
                    report.Code,
                    InspectionActivityLabels.ClosedNoViolation,
                    ct).ConfigureAwait(false);
            }

            await closedNotifier.NotifyReporterAsync(
                report.Id,
                report.Code,
                report.ReporterId,
                request.Reason,
                ct).ConfigureAwait(false);
        }

        await auditLogger.LogAsync(
            "CloseNoViolation",
            "InspectionReport",
            inspection.Id.ToString(),
            oldValues: oldSnapshot,
            newValues: JsonSerializer.Serialize(new
            {
                status = inspection.Status.ToString(),
                reasonLength = request.Reason.Length
            }),
            ct).ConfigureAwait(false);

        logger.LogInformation("InspectionReport {Id} closed — no violation found", request.InspectionId);
        return Result.Success();
    }
}
