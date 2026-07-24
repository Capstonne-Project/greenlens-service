using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Inspection.GetInspectionReportById;

public sealed class GetInspectionReportByIdQueryHandler(
    IInspectionReportRepository inspections,
    ILogger<GetInspectionReportByIdQueryHandler> logger)
    : IRequestHandler<GetInspectionReportByIdQuery, Result<InspectionReportDetailResponse>>
{
    public async Task<Result<InspectionReportDetailResponse>> Handle(
        GetInspectionReportByIdQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting inspection report by id");

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

        logger.LogInformation("Inspection report: {InspectionReport}", ir);

        return new InspectionReportDetailResponse(
            ir.Id,
            ir.ReportId,
            ir.Report!.Code,
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
            ir.CreatedByOfficerId,
            ir.CreatedByOfficer?.FullName,
            ir.IssuedByInspectorId,
            ir.IssuedByInspector?.FullName,
            ir.SlaInspectionDueAt,
            ir.ClosedAt,
            ir.ClosedReason,
            ir.CreatedAt,
            CanEditDetails: ir.Status == InspectionStatus.Draft,
            CanIssuePenalty: ir.Status == InspectionStatus.Draft,
            CanCloseNoViolation: ir.Status == InspectionStatus.Draft,
            CanRecordPayment: ir.Status is InspectionStatus.PenaltyIssued
                or InspectionStatus.PartiallyPaid
                or InspectionStatus.Overdue,
            CanClose: ir.Status == InspectionStatus.Paid);
    }
}

