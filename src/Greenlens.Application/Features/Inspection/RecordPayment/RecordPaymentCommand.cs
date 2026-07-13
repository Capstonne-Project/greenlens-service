using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Inspection.RecordPayment;

/// <summary>
/// BR-INS-020: Record penalty payment (in-person at ward office).
/// Supports partial payment (multiple records). Evidence = ảnh biên lai.
/// </summary>
public sealed record RecordPaymentCommand(
    Guid InspectionId,
    decimal PaidAmount,
    DateTime PaidAt,
    string? EvidenceUrl = null,
    string? Note = null) : IRequest<Result>;
