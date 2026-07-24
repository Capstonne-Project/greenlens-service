using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Inspection.GetInspectionsByReport;

public sealed class GetInspectionsByReportQueryHandler(
    IInspectionReportRepository inspections,
    IReportRepository reports,
    ILogger<GetInspectionsByReportQueryHandler> logger)
    : IRequestHandler<GetInspectionsByReportQuery, Result<GetInspectionsByReportResponse>>
{
    public async Task<Result<GetInspectionsByReportResponse>> Handle(
        GetInspectionsByReportQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting inspections by report");

        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
        {
            logger.LogWarning("Report not found for report {ReportId}", request.ReportId);
            return Errors.Reports.ReportNotFound;
        }

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

        logger.LogInformation("Inspections by report: {Items}", items);

        return new GetInspectionsByReportResponse(items);
    }
}
