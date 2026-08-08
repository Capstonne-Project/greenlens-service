using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Inspection.GetInspectionReportById;

/// <summary>Full inspection detail including checklist workflow state (BR-INS-033).</summary>
/// <remarks>Implements: BR-INS-010, BR-INS-033.</remarks>
public sealed class GetInspectionReportByIdQueryHandler(
    IInspectionReportRepository inspections,
    IInspectionEvidenceRepository evidences,
    IReportRepository reports,
    ITeamMemberRepository teamMembers,
    IUserRepository users,
    ICurrentUser currentUser,
    ILogger<GetInspectionReportByIdQueryHandler> logger)
    : IRequestHandler<GetInspectionReportByIdQuery, Result<InspectionReportDetailResponse>>
{
    public async Task<Result<InspectionReportDetailResponse>> Handle(
        GetInspectionReportByIdQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting inspection report by id {InspectionId}", request.InspectionId);

        var ir = await inspections.QueryAsNoTracking()
            .Include(x => x.Report)
            .Include(x => x.CreatedByOfficer)
            .Include(x => x.IssuedByInspector)
            .Include(x => x.AssignedTeam)
            .Include(x => x.ViolatingEntity)
            .Include(x => x.Payments).ThenInclude(p => p.RecordedByUser)
            .FirstOrDefaultAsync(x => x.Id == request.InspectionId, ct)
            .ConfigureAwait(false);

        if (ir is null)
        {
            logger.LogWarning("Inspection not found for inspection {InspectionId}", request.InspectionId);
            return Errors.Inspections.InspectionNotFound;
        }

        if (currentUser.Role != UserRole.Admin.ToString())
        {
            var scopeError = await InspectionTeamAuthorization.ValidateInspectionReadAccessAsync(
                ir, reports, teamMembers, users, currentUser, ct).ConfigureAwait(false);
            if (scopeError is not null)
                return scopeError;
        }

        var checklistItems = await evidences.QueryAsNoTracking()
            .Where(e => e.InspectionReportId == ir.Id)
            .OrderBy(e => e.UploadedAt)
            .Select(e => new InspectionEvidenceItemDto(
                e.Id,
                e.Category,
                e.MediaUrl,
                e.MimeType,
                e.SizeBytes,
                e.Description,
                e.DurationSeconds,
                e.UploadedAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var veDto = ir.ViolatingEntity is not null
            ? new ViolatingEntityEmbeddedDto(
                ir.ViolatingEntity.Id,
                ir.ViolatingEntity.Name,
                ir.ViolatingEntity.Type,
                ir.ViolatingEntity.Address,
                ir.ViolatingEntity.TaxCode,
                ir.ViolatingEntity.IdentityNumber,
                ir.ViolatingEntity.PhoneNumber)
            : null;

        var payments = ir.Payments
            .OrderByDescending(p => p.PaidAt)
            .Select(p => new PenaltyPaymentDto(
                p.Id,
                p.Amount,
                p.PaidAt,
                p.EvidenceUrl,
                p.Note,
                p.RecordedByUserId,
                p.RecordedByUser?.FullName,
                p.CreatedAt))
            .ToList();

        var fieldSubmitted = ir.FieldInvestigationSubmittedAt.HasValue;
        var inProgress = ir.Status == InspectionStatus.InProgress;

        return new InspectionReportDetailResponse(
            ir.Id,
            ir.ReportId,
            ir.Report!.Code,
            ir.Report.Latitude,
            ir.Report.Longitude,
            ir.Status,
            ir.AssignedTeamId,
            ir.AssignedTeam?.Name,
            ir.ViolationDescription,
            ir.ViolatorName,
            ir.ViolatorAddress,
            ir.ViolatorIdentity,
            ir.ViolationLevel,
            ir.PenaltyAmount,
            ir.PenaltyDecisionNumber,
            ir.PenaltyIssuedAt,
            ir.PenaltyDueDate,
            ir.PaidAmount,
            ir.AdditionalPenaltyMeasures,
            ir.IsRepeatOffender,
            ir.ViolatingEntityId,
            veDto,
            payments,
            ir.AcceptedAt,
            ir.AcceptedByUserId,
            ir.ArrivalConfirmedAt,
            ir.ArrivalLatitude,
            ir.ArrivalLongitude,
            ir.ArrivalNote,
            ir.FieldInvestigationSubmittedAt,
            ir.FieldInvestigationSubmittedByUserId,
            checklistItems,
            ir.CreatedByOfficerId,
            ir.CreatedByOfficer?.FullName,
            ir.IssuedByInspectorId,
            ir.IssuedByInspector?.FullName,
            ir.SlaInspectionDueAt,
            ir.ClosedAt,
            ir.ClosedReason,
            ir.CreatedAt,
            CanAcceptTask: ir.Status == InspectionStatus.Draft && ir.AssignedTeamId.HasValue,
            CanConfirmArrival: inProgress && !fieldSubmitted,
            CanEditChecklist: inProgress && !fieldSubmitted,
            CanSubmitFieldReport: inProgress && !fieldSubmitted,
            CanEditDetails: inProgress && !fieldSubmitted,
            CanIssuePenalty: inProgress && fieldSubmitted,
            CanCloseNoViolation: inProgress && fieldSubmitted,
            CanRecordPayment: ir.Status is InspectionStatus.PenaltyIssued or InspectionStatus.Overdue,
            CanClose: ir.Status == InspectionStatus.Paid);
    }
}
