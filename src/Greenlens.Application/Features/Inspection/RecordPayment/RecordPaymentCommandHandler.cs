using System.Text.Json;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Notifications;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Inspection.RecordPayment;

/// <summary>
/// BR-INS-020: Record in-person penalty payment at ward/commune office.
/// Creates a PenaltyPayment record with evidence, updates InspectionReport status,
/// and immediately closes the dossier (Paid → Closed) — LEO thực hiện cả 2 bước cùng lúc.
/// BR-ADM-010 (audit log).
/// </summary>
public sealed class RecordPaymentCommandHandler(
    IInspectionReportRepository inspections,
    IReportRepository reports,
    ILocalOfficeRepository localOffices,
    IFileStorageService fileStorage,
    INotificationService notificationService,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    IAuditLogger auditLogger,
    ILogger<RecordPaymentCommandHandler> logger)
    : IRequestHandler<RecordPaymentCommand, Result>
{
    public async Task<Result> Handle(RecordPaymentCommand request, CancellationToken ct)
    {
        logger.LogInformation("Getting record payment");

        var inspection = await inspections.GetByIdAsync(request.InspectionId, ct).ConfigureAwait(false);
        if (inspection is null)
        {
            logger.LogWarning("Inspection not found for inspection {InspectionId}", request.InspectionId);
            return Errors.Inspections.InspectionNotFound;
        }

        var authError = await InspectionTeamAuthorization.ValidateLeoForReportAsync(
            inspection, reports, localOffices, currentUser, ct).ConfigureAwait(false);
        if (authError is not null)
        {
            logger.LogWarning("LEO validation failed for inspection {InspectionId}", request.InspectionId);
            return authError;
        }

        var evidenceUrl = request.EvidenceUrl;
        if (request.ReceiptBytes is { Length: > 0 })
        {
            using var stream = new MemoryStream(request.ReceiptBytes);
            var uploaded = await fileStorage.UploadAsync(
                stream,
                request.ReceiptFileName ?? "receipt.jpg",
                request.ReceiptContentType ?? "image/jpeg",
                $"inspections/{inspection.Id}/payments",
                ct).ConfigureAwait(false);
            evidenceUrl = uploaded.Url;
        }

        if (string.IsNullOrWhiteSpace(evidenceUrl))
        {
            logger.LogWarning("Payment receipt missing for inspection {InspectionId}", request.InspectionId);
            return Errors.Inspections.PaymentReceiptRequired;
        }

        var oldSnapshot = JsonSerializer.Serialize(new
        {
            status = inspection.Status.ToString(),
            paidAmount = inspection.PaidAmount
        });

        // Create PenaltyPayment record (in-person at ward office)
        var payment = PenaltyPayment.Create(
            inspection.Id,
            request.PaidAmount,
            request.PaidAt,
            currentUser.UserId,
            evidenceUrl,
            request.Note);

        var result = inspection.RecordPayment(payment);
        if (result.IsFailure)
        {
            logger.LogWarning("Failed to record payment for inspection {InspectionId}", request.InspectionId);
            return result;
        }

        // LEO ghi nhận nộp đủ và đóng hồ sơ cùng một hành động — không chờ Inspector đóng riêng.
        var closeResult = inspection.Close("Đóng hồ sơ sau khi LEO ghi nhận nộp phạt đủ.");
        if (closeResult.IsFailure)
        {
            logger.LogWarning(
                "Failed to auto-close inspection {InspectionId} after payment", request.InspectionId);
            return closeResult;
        }

        // ── Đọc mọi dữ liệu cần cho side-effect TRƯỚC khi ghi ──
        // NotificationService.SendRawAsync gọi ChangeTracker.Clear() (detach toàn bộ entity)
        // ngay giữa transaction đang mở. Nếu còn thao tác EF nào trên `inspection` sau đó,
        // EF sẽ UPDATE một entity đã detach → 0 rows affected → DbUpdateConcurrencyException
        // → middleware trả 409 CONCURRENCY_CONFLICT dù không hề có ai sửa đồng thời.
        // Vì vậy: load report trước, ghi một lần, rồi mới bắn notification ở cuối cùng.
        var inspectorId = inspection.IssuedByInspectorId;
        var reportCode = inspectorId.HasValue
            ? (await reports.GetByIdAsync(inspection.ReportId, ct).ConfigureAwait(false))?.Code
              ?? string.Empty
            : string.Empty;

        var newSnapshot = JsonSerializer.Serialize(new
        {
            status = inspection.Status.ToString(),
            paidAmount = inspection.PaidAmount,
            paymentAmount = request.PaidAmount
        });
        var totalPaid = inspection.PaidAmount;
        var penaltyAmount = inspection.PenaltyAmount;
        var inspectionId = inspection.Id;

        // Audit log ghi cùng lượt SaveChanges với payment + status — một transaction, một lần ghi.
        await auditLogger.EnqueueAsync(
            "RecordPaymentAndClose",
            "InspectionReport",
            inspectionId.ToString(),
            oldValues: oldSnapshot,
            newValues: newSnapshot,
            ct).ConfigureAwait(false);

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Payment {Amount} VND recorded on InspectionReport {Id} (paid at {PaidAt}). Dossier closed. Total paid: {TotalPaid}/{Total}",
            request.PaidAmount, inspectionId, request.PaidAt, totalPaid, penaltyAmount);

        // Side-effect cuối cùng — sau đây không được chạm vào `inspection` nữa.
        if (inspectorId.HasValue)
        {
            var placeholders = NotificationPlaceholders.ForInspectionPenaltyPaidAndClosed(
                reportCode,
                request.PaidAmount);

            // referenceId = InspectionId (không phải ReportId) — mobile Inspector route
            // /(inspector)/inspection/{id} gọi GET /v1/inspections/{id}, cần đúng InspectionId.
            await notificationService.SendFromTemplateAsync(
                inspectorId.Value,
                NotificationType.InspectionPenaltyPaidAndClosed,
                placeholders,
                inspectionId,
                ct).ConfigureAwait(false);
        }

        return Result.Success();
    }
}
