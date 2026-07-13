# Báo cáo thay đổi: Cập nhật cơ chế Soft Delete & Business Rules

## 1. Thêm mới (Additions)

* **API Endpoints (Soft Delete):**
  * `DELETE v1/admin/pollution-categories/{id}`: Xóa mềm danh mục vi phạm.
  * `DELETE v1/admin/waste-tags/{id}`: Xóa mềm thẻ rác thải.
  * `DELETE v1/companies/{id}`: Xóa mềm công ty xử lý môi trường.
  * `DELETE v1/teams/{id}`: Xóa mềm đội xử lý / thanh tra.
  * `DELETE v1/violating-entities/{id}`: Xóa mềm đối tượng vi phạm.
  * `DELETE v1/inspections/payments/{paymentId}`: Xóa mềm khoản thanh toán nộp phạt (Penalty Payment).
* **Commands & Handlers:**
  * `DeleteCategoryCommand`
  * `DeleteWasteTagCommand`
  * `DeleteEnvironmentalCompanyCommand`
  * `SoftDeleteCompanyTeamCommand`
  * `DeleteViolatingEntityCommand`
  * `DeletePenaltyPaymentCommand`
* **Domain & Business Logic:**
  * Thêm logic vào Entity `InspectionReport` thông qua phương thức `RemovePayment(payment)` để trừ lùi `PaidAmount` và tự động đánh giá lại `Status` (từ `Paid` về `PartiallyPaid`, `PenaltyIssued`, hoặc `Overdue`).
  * Kế thừa `SoftDeletableEntity` cho `PenaltyPayment`.
* **Cascade Delete (UserPoints):**
  * Tích hợp tự động xóa mềm `UserPoints` của một tài khoản khi tài khoản `User` đó bị Admin thực thi xóa mềm (nằm trong `DeleteUserCommandHandler`).

## 2. Chỉnh sửa (Modifications)

* **Teams API:**
  * Đổi cơ chế API `PUT v1/teams/{id}/toggle-status` (Archive/Unarchive) thành `ArchiveCompanyTeamCommand`.
* **Inspection Penalty Validation:**
  * Triển khai BR-INS-010: Trong `IssuePenaltyCommandHandler`, bổ sung xác thực yêu cầu phải có ít nhất `2` ảnh hiện trường trong `InspectionReport.Evidences` trước khi ra quyết định xử phạt.
* **Error Handling:**
  * Bổ sung các mã lỗi mới vào `InspectionErrors.cs` (`PaymentNotFound`, `InsufficientEvidenceImages`).
  * Cập nhật và sửa một số lỗi build liên quan đến namespace `Errors.Inspections`.

## 3. Lược bỏ (Deletions)

* Không xóa bỏ bảng hoặc cột nào khỏi DB.
* Các hành động Hard Delete (xóa cứng) đã được loại bỏ hoàn toàn khỏi các nghiệp vụ quản lý danh mục và được thay bằng hành động gọi hàm `.SoftDelete(userId)`.
