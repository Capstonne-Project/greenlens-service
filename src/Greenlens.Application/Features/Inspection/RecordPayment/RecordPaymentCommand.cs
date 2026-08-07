using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Inspection.RecordPayment;

/// <summary>
/// BR-INS-020: Record penalty payment (in-person at ward office).
/// Chỉ chấp nhận nộp đúng 1 lần, đủ toàn bộ số tiền còn lại — không hỗ trợ nộp từng phần.
/// Evidence = ảnh biên lai. Ghi nhận thành công sẽ tự động đóng hồ sơ (Paid → Closed).
/// </summary>
public sealed record RecordPaymentCommand(
    Guid InspectionId,
    decimal PaidAmount,
    DateTime PaidAt,
    string? EvidenceUrl = null,
    string? Note = null,
    byte[]? ReceiptBytes = null,
    string? ReceiptFileName = null,
    string? ReceiptContentType = null) : IRequest<Result>;
