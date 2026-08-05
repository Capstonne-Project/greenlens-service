using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Notifications;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Inspection.SubmitFieldInvestigation;

/// <summary>
/// BR-INS-033: Team Leader locks checklist and enables penalty / close-no-violation actions.
/// </summary>
/// <remarks>Implements: BR-INS-033, BR-INS-012.</remarks>
public sealed class SubmitFieldInvestigationCommandHandler(
    IInspectionReportRepository inspections,
    IInspectionEvidenceRepository evidences,
    IReportRepository reports,
    ITeamMemberRepository teamMembers,
    IInspectionAssignmentActivityNotifier activityNotifier,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<SubmitFieldInvestigationCommandHandler> logger)
    : IRequestHandler<SubmitFieldInvestigationCommand, Result>
{
    public async Task<Result> Handle(SubmitFieldInvestigationCommand request, CancellationToken ct)
    {
        var inspection = await inspections.GetByIdAsync(request.InspectionId, ct).ConfigureAwait(false);
        if (inspection is null)
            return Errors.Inspections.InspectionNotFound;

        var authError = await InspectionTeamAuthorization.ValidateTeamLeaderAsync(
            inspection, teamMembers, currentUser, ct).ConfigureAwait(false);
        if (authError is not null)
            return authError;

        var items = await evidences.GetByInspectionReportIdAsync(request.InspectionId, ct)
            .ConfigureAwait(false);

        var checklistError = InspectionChecklistValidator.Validate(items);
        if (checklistError is not null)
            return checklistError;

        var result = inspection.SubmitFieldInvestigation(currentUser.UserId);
        if (result.IsFailure)
            return result;

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
                    InspectionActivityLabels.FieldReportSubmitted,
                    ct).ConfigureAwait(false);
            }
        }

        logger.LogInformation(
            "Field investigation submitted for inspection {InspectionId} by {UserId}",
            request.InspectionId, currentUser.UserId);

        return Result.Success();
    }
}
