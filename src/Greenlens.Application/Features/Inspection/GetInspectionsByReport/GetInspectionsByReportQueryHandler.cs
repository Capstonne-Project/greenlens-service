using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Inspection.GetInspectionsByReport;

public sealed class GetInspectionsByReportQueryHandler(
    IInspectionReportRepository inspections,
    IReportRepository reports)
    : IRequestHandler<GetInspectionsByReportQuery, Result<GetInspectionsByReportResponse>>
{
    public async Task<Result<GetInspectionsByReportResponse>> Handle(
        GetInspectionsByReportQuery request, CancellationToken ct)
    {
        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
            return Errors.Reports.ReportNotFound;

        var items = await inspections.QueryAsNoTracking()
            .Include(ir => ir.CreatedByOfficer)
            .Include(ir => ir.ViolatingEntity)
            .Where(ir => ir.ReportId == request.ReportId)
            .OrderByDescending(ir => ir.CreatedAt)
            .Select(ir => new InspectionSummaryDto(
                ir.Id,
                ir.Status,
                ir.ViolatorName,
                ir.ViolationLevel,
                ir.PenaltyAmount,
                ir.PaidAmount,
                ir.IsRepeatOffender,
                ir.ViolatingEntityId,
                ir.ViolatingEntity != null ? ir.ViolatingEntity.Name : null,
                ir.CreatedByOfficerId,
                ir.CreatedByOfficer!.FullName,
                ir.SlaInspectionDueAt,
                ir.ClosedAt,
                ir.CreatedAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new GetInspectionsByReportResponse(items);
    }
}
