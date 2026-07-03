using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Inspection.GetInspectionReportById;

public sealed class GetInspectionReportByIdQueryHandler(
    IInspectionReportRepository inspections)
    : IRequestHandler<GetInspectionReportByIdQuery, Result<InspectionReportDetailResponse>>
{
    public async Task<Result<InspectionReportDetailResponse>> Handle(
        GetInspectionReportByIdQuery request, CancellationToken ct)
    {
        var ir = await inspections.QueryAsNoTracking()
            .Include(x => x.Report)
            .Include(x => x.CreatedByOfficer)
            .Include(x => x.IssuedByInspector)
            .Include(x => x.AssignedTeam)
            .FirstOrDefaultAsync(x => x.Id == request.InspectionId, ct)
            .ConfigureAwait(false);

        if (ir is null)
            return Errors.Inspections.InspectionNotFound;

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
