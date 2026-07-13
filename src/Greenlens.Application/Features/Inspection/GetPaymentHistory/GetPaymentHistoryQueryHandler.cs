using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Inspection.GetInspectionReportById;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Inspection.GetPaymentHistory;

/// <summary>BR-INS-020: Returns all PenaltyPayment records for an InspectionReport.</summary>
public sealed class GetPaymentHistoryQueryHandler(
    IInspectionReportRepository inspections)
    : IRequestHandler<GetPaymentHistoryQuery, Result<GetPaymentHistoryResponse>>
{
    public async Task<Result<GetPaymentHistoryResponse>> Handle(
        GetPaymentHistoryQuery request, CancellationToken ct)
    {
        var ir = await inspections.QueryAsNoTracking()
            .Include(x => x.Payments).ThenInclude(p => p.RecordedByUser)
            .FirstOrDefaultAsync(x => x.Id == request.InspectionId, ct)
            .ConfigureAwait(false);

        if (ir is null)
            return Errors.Inspections.InspectionNotFound;

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

        var remaining = Math.Max(0, (ir.PenaltyAmount ?? 0) - (ir.PaidAmount ?? 0));

        return new GetPaymentHistoryResponse(
            ir.Id,
            ir.PenaltyAmount,
            ir.PaidAmount,
            remaining,
            payments);
    }
}
