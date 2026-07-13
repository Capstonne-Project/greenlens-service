using Greenlens.Application.Features.Inspection.GetInspectionReportById;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Inspection.GetPaymentHistory;

/// <summary>
/// BR-INS-020: Get penalty payment history for an InspectionReport.
/// Returns all PenaltyPayment records sorted by PaidAt descending.
/// </summary>
public sealed record GetPaymentHistoryQuery(Guid InspectionId) : IRequest<Result<GetPaymentHistoryResponse>>;

public sealed record GetPaymentHistoryResponse(
    Guid InspectionId,
    decimal? PenaltyAmount,
    decimal? PaidAmount,
    decimal RemainingAmount,
    List<PenaltyPaymentDto> Payments);
