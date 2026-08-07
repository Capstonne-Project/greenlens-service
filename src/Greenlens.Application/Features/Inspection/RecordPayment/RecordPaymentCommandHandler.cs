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

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        await auditLogger.LogAsync(
            "RecordPaymentAndClose",
            "InspectionReport",
            inspection.Id.ToString(),
            oldValues: oldSnapshot,
            newValues: JsonSerializer.Serialize(new
            {
                status = inspection.Status.ToString(),
                paidAmount = inspection.PaidAmount,
                paymentAmount = request.PaidAmount
            }),
            ct).ConfigureAwait(false);

        logger.LogInformation(
            "Payment {Amount} VND recorded on InspectionReport {Id} (paid at {PaidAt}). Dossier closed. Total paid: {TotalPaid}/{Total}",
            request.PaidAmount, inspection.Id, request.PaidAt,
            inspection.PaidAmount, inspection.PenaltyAmount);

        if (inspection.IssuedByInspectorId.HasValue)
        {
            var report = await reports.GetByIdAsync(inspection.ReportId, ct).ConfigureAwait(false);
            var placeholders = NotificationPlaceholders.ForInspectionPenaltyPaidAndClosed(
                report?.Code ?? string.Empty,
                request.PaidAmount);

            // referenceId = InspectionId (không phải ReportId) — mobile Inspector route
            // /(inspector)/inspection/{id} gọi GET /v1/inspections/{id}, cần đúng InspectionId.
            await notificationService.SendFromTemplateAsync(
                inspection.IssuedByInspectorId.Value,
                NotificationType.InspectionPenaltyPaidAndClosed,
                placeholders,
                inspection.Id,
                ct).ConfigureAwait(false);
        }

        return Result.Success();
    }
}
