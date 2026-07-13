using Greenlens.Domain.Common;

namespace Greenlens.Domain.Entities;

/// <summary>
/// Ghi nhận từng lần nộp phạt tại phường/xã. Immutable sau khi tạo.
/// Hỗ trợ partial payment (nhiều lần nộp). Bằng chứng = ảnh biên lai.
/// </summary>
/// <remarks>
/// Implements: BR-INS-020 (ghi nhận nộp phạt — Paid / PartiallyPaid).
/// SUM(PenaltyPayment.Amount) vs InspectionReport.PenaltyAmount → Paid hay PartiallyPaid.
/// Hiện chỉ hỗ trợ nộp trực tiếp tại phường/xã (InPerson).
/// TODO: Bổ sung PaymentMethod enum (Cash/BankTransfer) khi cần hỗ trợ online payment.
/// </remarks>
public sealed class PenaltyPayment : SoftDeletableEntity
{
    private PenaltyPayment() { } // EF Core constructor

    /// <summary>FK → InspectionReport that this payment is for.</summary>
    public Guid InspectionReportId { get; private set; }

    /// <summary>Số tiền nộp trong lần này (VND).</summary>
    public decimal Amount { get; private set; }

    /// <summary>Thời điểm thực tế nộp phạt (có thể khác CreatedAt nếu ghi nhận sau).</summary>
    public DateTime PaidAt { get; private set; }

    /// <summary>URL ảnh biên lai / bằng chứng nộp phạt (S3 presigned).</summary>
    public string? EvidenceUrl { get; private set; }

    /// <summary>Ghi chú bổ sung (VD: "nộp tại UBND phường X, biên lai số 123").</summary>
    public string? Note { get; private set; }

    /// <summary>Inspector ghi nhận khoản nộp phạt này.</summary>
    public Guid RecordedByUserId { get; private set; }

    // ── Navigation ──
    public InspectionReport? InspectionReport { get; private set; }
    public User? RecordedByUser { get; private set; }

    // ────────────────────────────────────────────────────
    // Factory
    // ────────────────────────────────────────────────────

    /// <summary>BR-INS-020: Create a payment record for in-person payment at ward office.</summary>
    public static PenaltyPayment Create(
        Guid inspectionReportId,
        decimal amount,
        DateTime paidAt,
        Guid recordedByUserId,
        string? evidenceUrl = null,
        string? note = null)
    {
        return new PenaltyPayment
        {
            InspectionReportId = inspectionReportId,
            Amount = amount,
            PaidAt = paidAt,
            RecordedByUserId = recordedByUserId,
            EvidenceUrl = evidenceUrl,
            Note = note
        };
    }
}
