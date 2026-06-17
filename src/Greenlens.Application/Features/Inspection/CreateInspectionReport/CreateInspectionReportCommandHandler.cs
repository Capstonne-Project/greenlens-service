using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Inspection.CreateInspectionReport;

/// <summary>
/// BR-INS-001: LEO creates InspectionReport (Draft) for a verified Report.
/// BR-OFF-005: Triage decision at verification — LEO identifies violator.
/// </summary>
public sealed class CreateInspectionReportCommandHandler(
    IReportRepository reports,
    IInspectionReportRepository inspections,
    IEnvironmentalTeamRepository teams,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<CreateInspectionReportCommandHandler> logger)
    : IRequestHandler<CreateInspectionReportCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateInspectionReportCommand request, CancellationToken ct)
    {
        // 1. Validate report exists and is Verified
        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
            return Errors.Reports.ReportNotFound;

        if (report.Status != ReportStatus.Verified && report.Status != ReportStatus.InProgress)
            return Errors.Inspections.ReportNotVerified;

        // 2. Check for existing active inspection on same report
        var existing = await inspections.GetByReportIdAsync(request.ReportId, ct).ConfigureAwait(false);
        var hasActive = existing.Any(ir =>
            ir.Status is not (InspectionStatus.Closed or InspectionStatus.ClosedNoViolation));
        if (hasActive)
            return Errors.Inspections.InspectionAlreadyExistsForReport;

        // 3. Validate team if provided
        if (request.AssignedTeamId.HasValue)
        {
            var team = await teams.GetByIdAsync(request.AssignedTeamId.Value, ct).ConfigureAwait(false);
            if (team is null)
                return Errors.Inspections.TeamNotFound;

            if (team.TeamType != TeamType.Inspection)
                return Errors.Inspections.TeamNotInspectionType;
        }

        // 4. Create inspection report
        var inspection = InspectionReport.Create(
            request.ReportId,
            currentUser.UserId,
            report.Severity,
            request.AssignedTeamId,
            request.ViolationDescription,
            request.ViolatorName,
            request.ViolatorAddress,
            request.ViolatorIdentity);

        inspections.Add(inspection);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "InspectionReport {InspectionId} created for Report {ReportId} by LEO {UserId}",
            inspection.Id, request.ReportId, currentUser.UserId);

        return inspection.Id;
    }
}
