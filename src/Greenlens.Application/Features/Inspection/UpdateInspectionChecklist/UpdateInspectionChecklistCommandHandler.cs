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

namespace Greenlens.Application.Features.Inspection.UpdateInspectionChecklist;

/// <summary>
/// BR-INS-033: Upsert ViolationStatus and Other text items on the checklist.
/// </summary>
/// <remarks>Implements: BR-INS-033.</remarks>
public sealed class UpdateInspectionChecklistCommandHandler(
    IInspectionReportRepository inspections,
    IInspectionEvidenceRepository evidences,
    IReportRepository reports,
    ITeamMemberRepository teamMembers,
    IInspectionAssignmentActivityNotifier activityNotifier,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<UpdateInspectionChecklistCommandHandler> logger)
    : IRequestHandler<UpdateInspectionChecklistCommand, Result>
{
    public async Task<Result> Handle(UpdateInspectionChecklistCommand request, CancellationToken ct)
    {
        var inspection = await inspections.GetByIdAsync(request.InspectionId, ct).ConfigureAwait(false);
        if (inspection is null)
            return Errors.Inspections.InspectionNotFound;

        if (inspection.FieldInvestigationSubmittedAt.HasValue)
            return Errors.Inspections.FieldReportAlreadySubmitted;

        var authError = await InspectionTeamAuthorization.ValidateTeamMemberAsync(
            inspection, teamMembers, currentUser, ct).ConfigureAwait(false);
        if (authError is not null)
            return authError;

        await UpsertTextEvidenceAsync(
            request.InspectionId,
            InspectionEvidenceCategory.ViolationStatus,
            request.ViolationStatusText.Trim(),
            ct).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(request.OtherDescription))
        {
            await UpsertTextEvidenceAsync(
                request.InspectionId,
                InspectionEvidenceCategory.Other,
                request.OtherDescription.Trim(),
                ct).ConfigureAwait(false);
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        if (inspection.AssignedTeamId is Guid teamId)
        {
            var report = await reports.GetByIdAsync(inspection.ReportId, ct).ConfigureAwait(false);
            if (report is not null)
            {
                await activityNotifier.NotifyProgressUpdatedAsync(
                    inspection.CreatedByOfficerId,
                    teamId,
                    report.Id,
                    report.Code,
                    InspectionActivityLabels.ChecklistUpdated,
                    ct).ConfigureAwait(false);
            }
        }

        logger.LogInformation("Checklist text updated for inspection {InspectionId}", request.InspectionId);
        return Result.Success();
    }

    private async Task UpsertTextEvidenceAsync(
        Guid inspectionId,
        InspectionEvidenceCategory category,
        string description,
        CancellationToken ct)
    {
        var existing = await evidences.Query()
            .FirstOrDefaultAsync(
                e => e.InspectionReportId == inspectionId && e.Category == category,
                ct)
            .ConfigureAwait(false);

        if (existing is null)
        {
            evidences.Add(InspectionEvidence.CreateText(
                inspectionId, category, description, currentUser.UserId));
        }
        else
        {
            existing.UpdateDescription(description);
        }
    }
}
