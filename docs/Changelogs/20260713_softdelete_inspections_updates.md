# Báo cáo thay đổi: Cập nhật cơ chế Soft Delete & Business Rules

## 1. Thêm mới (Additions)

- **API Endpoints (Soft Delete):**
  - `DELETE v1/admin/pollution-categories/{id}`: Xóa mềm danh mục vi phạm.
  - `DELETE v1/admin/waste-tags/{id}`: Xóa mềm thẻ rác thải.
  - `DELETE v1/companies/{id}`: Xóa mềm công ty xử lý môi trường.
  - `DELETE v1/teams/{id}`: Xóa mềm đội xử lý / thanh tra.
  - `DELETE v1/violating-entities/{id}`: Xóa mềm đối tượng vi phạm.
  - `DELETE v1/inspections/payments/{paymentId}`: Xóa mềm khoản thanh toán nộp phạt (Penalty Payment).
- **Commands & Handlers:**
  - `DeleteCategoryCommand`
  - `DeleteWasteTagCommand`
  - `DeleteEnvironmentalCompanyCommand`
  - `SoftDeleteCompanyTeamCommand`
  - `DeleteViolatingEntityCommand`
  - `DeletePenaltyPaymentCommand`
- **Domain & Business Logic:**
  - Thêm logic vào Entity `InspectionReport` thông qua phương thức `RemovePayment(payment)` để trừ lùi `PaidAmount` và tự động đánh giá lại `Status` (từ `Paid` về `PartiallyPaid`, `PenaltyIssued`, hoặc `Overdue`).
  - Kế thừa `SoftDeletableEntity` cho `PenaltyPayment`.
- **Cascade Delete (UserPoints):**
  - Tích hợp tự động xóa mềm `UserPoints` của một tài khoản khi tài khoản `User` đó bị Admin thực thi xóa mềm (nằm trong `DeleteUserCommandHandler`).

## 2. Chỉnh sửa (Modifications)

- **Teams API:**
  - Đổi cơ chế API `PUT v1/teams/{id}/toggle-status` (Archive/Unarchive) thành `ArchiveCompanyTeamCommand`.
- **Inspection Penalty Validation:**
  - Triển khai BR-INS-010: Trong `IssuePenaltyCommandHandler`, bổ sung xác thực yêu cầu phải có ít nhất `2` ảnh hiện trường trong `InspectionReport.Evidences` trước khi ra quyết định xử phạt.
- **Error Handling:**
  - Bổ sung các mã lỗi mới vào `InspectionErrors.cs` (`PaymentNotFound`, `InsufficientEvidenceImages`).
  - Cập nhật và sửa một số lỗi build liên quan đến namespace `Errors.Inspections`.

## 3. Lược bỏ (Deletions)

- Không xóa bỏ bảng hoặc cột nào khỏi DB.
- Các hành động Hard Delete (xóa cứng) đã được loại bỏ hoàn toàn khỏi các nghiệp vụ quản lý danh mục và được thay bằng hành động gọi hàm `.SoftDelete(userId)`.

---

# Báo cáo thay đổi: Tích hợp Notification Templates (Không hardcode câu chữ)

## 1. Thêm mới (Additions)

- **Seed Data:**
  - Thêm mới `NotificationTemplateSeeder` để tự động khởi tạo 11 mẫu thông báo (Notification Templates) chuẩn bị sẵn cho cả tiếng Việt (vi-VN) và tiếng Anh (en-US). Các mẫu này được thiết lập tự động `IsPublished = true` từ ban đầu.
  - Tích hợp việc gọi `NotificationTemplateSeeder` vào luồng chạy gốc trong `AdminSeeder`.

## 2. Chỉnh sửa (Modifications)

- **Core Notification Service (`INotificationService` / `NotificationService`):**
  - Đổi tên hàm `SendAsync` thành `SendRawAsync` (chủ yếu dùng cho thao tác Test Template của Admin).
  - Xây dựng mới hàm `SendFromTemplateAsync`: Hàm này tự động tra cứu template trong Database dựa vào loại sự kiện (`NotificationType`), thay thế các biến động (`placeholders` như `{report_id}`) bằng nội dung thực tế thông qua Regex, sau đó mới gửi thông báo đi.
- **Event Handlers (Loại bỏ Hardcode):**
  - Cập nhật `ReportStatusNotificationHandler` (và các handler liên quan): Xóa bỏ các dòng mã hardcode câu chữ bằng tiếng Việt. Thay vào đó, gọi `SendFromTemplateAsync` và truyền vào bộ dữ liệu (placeholders) tương ứng với báo cáo.
- **Admin Test Notification:**
  - Chỉnh sửa `TestNotificationTemplateCommandHandler` để gọi hàm `SendRawAsync` thay vì `SendAsync` đã cũ.
