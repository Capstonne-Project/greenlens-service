# Bug: 409 CONCURRENCY_CONFLICT khi LEO ghi nhận nộp phạt

## Triệu chứng

Sau khi Inspector team leader ban hành quyết định xử phạt (`issue-penalty`), LEO vào ghi nhận
nộp phạt (`record-payment`) trên web portal thì bị lỗi ngay lần bấm đầu tiên, dù đã F5 load
fresh data trước đó:

```json
{
  "code": "CONCURRENCY_CONFLICT",
  "message": "Thao tác có thể đã được ghi nhận. Vui lòng tải lại trang để kiểm tra trạng thái trước khi thử lại.",
  "status": 409,
  "data": null
}
```

## Nguyên nhân gốc (đã xác nhận qua code, không phải suy đoán)

Đây **không phải** race condition thật giữa 2 người dùng, và schema **không có** cột
optimistic-concurrency thật (không `RowVersion` / `xmin` / `[ConcurrencyToken]` nào trong
`InspectionReport`).

Lỗi thực ra do:

- `NotificationService.SendRawAsync`
  (`src/Greenlens.Infrastructure/Notifications/NotificationService.cs:142-143`) tự gọi
  `SaveChangesAsync()` rồi `ChangeTracker.Clear()` — detach **toàn bộ** entity đang track trên
  `ApplicationDbContext` của request.
- Nếu handler còn thao tác/save entity `InspectionReport` **sau khi** đã gọi notification giữa
  request, lần `SaveChanges` cuối UPDATE vào entity đã bị detach → EF Core thấy 0 rows affected
  → ném `DbUpdateConcurrencyException`.
- `ExceptionHandlingMiddleware.cs:42-58` bắt `DbUpdateConcurrencyException` và map cứng thành
  `409 CONCURRENCY_CONFLICT` — nhưng bản chất không có ai sửa đồng thời cả, chỉ là thứ tự gọi
  trong handler bị sai.

## Vị trí lỗi cụ thể

- Handler bị lỗi: `src/Greenlens.Application/Features/Inspection/RecordPayment/RecordPaymentCommandHandler.cs`
- Middleware map lỗi: `src/Greenlens.Api/Middlewares/ExceptionHandlingMiddleware.cs:42-58`
- Nguồn gây detach: `src/Greenlens.Infrastructure/Notifications/NotificationService.cs:142-143`

## Fix đã tồn tại — chỉ chưa lên `main`

Commit `b356e91` trên branch `hotfix/test-phase-1` đã sửa đúng bug này trong
`RecordPaymentCommandHandler`:

1. Load hết dữ liệu cần cho notification (`inspectorId`, `reportCode` qua
   `reports.GetByIdAsync`) **trước** khi save cuối.
2. Đổi audit log từ `auditLogger.LogAsync` (tự gọi `SaveChangesAsync` riêng) sang
   `auditLogger.EnqueueAsync` (chỉ track, không save riêng) — để audit log gộp vào đúng 1 lần
   `uow.SaveChangesAsync(ct)` duy nhất.
3. Đẩy `notificationService.SendFromTemplateAsync` (lệnh gây detach) xuống **cuối cùng**, sau
   khi không còn gì động vào entity `inspection` nữa.

`IssuePenaltyCommandHandler` đã được kiểm tra riêng và **an toàn từ đầu** — thứ tự đúng:
`uow.SaveChangesAsync` (dòng 118) chạy trước `activityNotifier.NotifyCompletedAsync`
(dòng 125), và notifier đó chỉ đọc property, không re-save entity.

## Việc cần làm

```bash
git log --oneline main..hotfix/test-phase-1   # xem các commit chưa lên main
git merge-base --is-ancestor b356e91 main     # xác nhận b356e91 chưa có trên main (kết quả: false)
```

- Nếu `hotfix/test-phase-1` an toàn để merge nguyên nhánh → merge vào `main`.
- Nếu không muốn kéo theo các thay đổi khác trên nhánh đó → `git cherry-pick b356e91` riêng lẻ
  vào `main` (kiểm tra conflict, vì file `RecordPaymentCommandHandler.cs` có thể đã đổi khác
  trên `main` từ lúc branch tách ra).
- Sau khi merge, cần kiểm tra thêm các handler khác có cùng pattern "save entity → gọi
  `NotificationService.SendRawAsync` / `SendFromTemplateAsync` → save lại entity đó lần nữa" —
  vì đây là bug pattern có thể lặp ở nơi khác, không chỉ riêng `record-payment`. Gợi ý: grep
  `SendRawAsync\|SendFromTemplateAsync` trong `Features/**/*CommandHandler.cs`, xem handler nào
  gọi notification **không phải ở cuối cùng** của method.
