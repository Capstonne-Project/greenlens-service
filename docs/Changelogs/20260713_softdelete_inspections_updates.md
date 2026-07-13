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

---

# Báo cáo thay đổi: Cấu hình SignalR Real-time Notifications & Dọn dẹp Code

## 1. Tích hợp SignalR cho Web Dashboard

- **Khởi tạo SignalR:**
  - Đăng ký `builder.Services.AddSignalR()` và khai báo route `app.MapHub<NotificationHub>("/hubs/notifications")` trong `Program.cs`.
  - Thêm `FrameworkReference` tới `Microsoft.AspNetCore.App` trong `Greenlens.Infrastructure.csproj`.
- **Bảo mật (Authentication):**
  - Cập nhật cấu hình JWT trong `DependencyInjection.cs`: Thêm xử lý `OnMessageReceived` để đọc token trực tiếp từ Query String (`?access_token=`) do WebSockets không hỗ trợ truyền qua Header.
- **Xử lý sự kiện gửi thông báo (Hub & Service):**
  - Tạo `NotificationHub` với `[Authorize]` để quản lý các client (Admin, Officer, Company) đang trực tuyến.
  - Sửa `NotificationService`: Nhúng `IHubContext` vào để sau khi lưu Database và bắn FCM/Email, hệ thống sẽ gọi hàm `ReceiveNotification` qua SignalR đẩy dữ liệu xuống thẳng UI.
- **Tài liệu cho FE & Mobile:**
  - Viết tài liệu [frontend_mobile_notifications_guide.md](file:///d:/LEARNING/S9SU26/SEP490/greenlens-service/docs/Guides/frontend_mobile_notifications_guide.md) hướng dẫn hai team cách móc nối sự kiện Notification mới.

## 2. Fix Build Warnings (Chore)

- **Fix CS0108 (Thuộc tính bị ẩn):**
  - Xóa bỏ việc khai báo lặp lại cột `CreatedAt` trong các thực thể `PollutionCategory`, `PointTransaction`, `WasteTag`, `UserPoints` vì các class này đã kế thừa sẵn từ `SoftDeletableEntity`/`AuditableEntity`.
- **Fix CS9107 (Lỗi Capture Primary Constructor trong Repositories):**
  - Chuyển toàn bộ các lệnh truy vấn `db.Set<T>()` thành thuộc tính `Context` có sẵn ở class cha (`GenericRepository`) trong các file `BadgeRepository`, `UserPointsRepository`, `UserBadgeRepository`, `EnvironmentalServiceCompanyRepository`, `ViolatingEntityRepository`. Việc này giải phóng bộ nhớ không bị bắt chết trong Primary Constructor.
- **Fix EF Core Runtime Warning (Skip/Take without OrderBy):**
  - Bổ sung lệnh `.OrderBy(r => r.Id)` vào trước hàm `.Take(BatchSize)` trong 2 Background Jobs (`DraftCleanupJob` và `OverdueReportNotificationJob`) để ngăn chặn cảnh báo runtime liên quan đến thứ tự phân trang (paging).
