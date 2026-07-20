# Tổng thể Luồng Nghiệp vụ CleanUp và Inspector

Tài liệu này cung cấp bức tranh toàn cảnh về cách các Actor (`CleanUp Team`, `Inspector Team`, `LEO`, `DEO`) tương tác với hệ thống để xử lý các báo cáo ô nhiễm môi trường, từ khi nhận task cho tới khi hoàn tất.

---

## 1. CleanUp Workflow (Đội vệ sinh)

Đội vệ sinh (CleanUp Team) chịu trách nhiệm dọn dẹp các điểm rác thải hoặc ô nhiễm không thuộc diện cần xử phạt hành chính.

### 1.1. Luồng trạng thái (Task Flow)

1. **Assign (Được phân công):**
   * LEO (Local Environmental Officer) xem các báo cáo đã `Verified`.
   * LEO gán báo cáo cho một `EnvironmentalTeam` (loại `CleanUp`) thông qua API `POST /reports/{id}/assign`.
   * Trạng thái báo cáo chuyển từ `Verified` sang `InProgress`.

2. **Check-In Hiện Trường (BR-CLN-002):**
   * Team Leader đến hiện trường và thực hiện Check-In qua API `POST /reports/{id}/check-in`.
   * Hệ thống sẽ tính toán khoảng cách giữa tọa độ check-in và tọa độ báo cáo. Nếu vượt quá `200m`, check-in sẽ bị từ chối.
   * Đây là bước bắt buộc để ghi nhận đội đã thực sự bắt đầu dọn dẹp.

3. **Cập Nhật Tiến Độ (Update Progress):**
   * Team Leader có thể cập nhật tiến độ (ví dụ: đang thu gom, chờ xe rác) qua API `PUT /reports/{id}/progress`.
   * Hành động này giúp LEO theo dõi sát sao tình hình xử lý (BR-CLN-003).

4. **Báo Cáo Hoàn Tất (Mark Resolved) (BR-CLN-004):**
   * Sau khi dọn dẹp xong, Team Leader phải upload ít nhất 2 ảnh "sau khi dọn" (After-photo).
   * Dùng API `PUT /reports/{id}/resolve`.
   * Trạng thái chuyển từ `InProgress` sang `Resolved`. Công việc của CleanUp kết thúc ở đây (công dân hoặc hệ thống sẽ tự động Close báo cáo sau đó).

### 1.2. Escalation (Báo Cáo Bất Thường)

* Nếu có sự cố (ví dụ: lượng rác quá lớn, cần máy xúc, hoặc phát hiện hóa chất độc hại), Team Leader có thể dùng API `POST /reports/{id}/escalate`.
* Hành động này sẽ gửi cảnh báo cho LEO và ghi chú lại trong lịch sử hệ thống, giúp LEO có hướng điều phối hỗ trợ.

---

## 2. Inspector Workflow (Đội Thanh tra)

Đội thanh tra (Inspector) được huy động khi báo cáo ô nhiễm liên quan đến các hành vi vi phạm hành chính (ví dụ: công ty xả thải lén lút, đổ trộm phế thải xây dựng).

### 2.1. Đối tượng Vi Phạm (Violating Entity)

Đây là một phần quan trọng của phân hệ Thanh tra. `ViolatingEntity` là một thực thể lưu trữ thông tin của người hoặc tổ chức bị phạt.

* **Cá nhân:** Được định danh bằng CMND/CCCD (`IdentityNumber`).
* **Tổ chức/Doanh nghiệp:** Được định danh bằng Mã số thuế (`TaxCode`).
* **Quản lý (CRUD):** LEO hoặc Inspector có thể tạo mới, cập nhật, lấy thông tin và xóa mềm đối tượng này (API `/violating-entities`).
* **Mục đích:** Để theo dõi hồ sơ vi phạm (BR-INS-022), nếu một đối tượng bị phát hiện tái phạm (≥ 2 lần trong 12 tháng), hệ thống sẽ gán cờ tái phạm và gợi ý nâng mức phạt.

### 2.2. Luồng xử lý Hồ Sơ Xử Phạt (Inspection Report)

Một `InspectionReport` luôn được gắn với một `Report` gốc.

1. **Khởi tạo và Gán Task:**
   * LEO quyết định báo cáo này cần thanh tra (tạo `InspectionReport`) và gán cho một `Inspector Team` qua API `POST /reports/{id}/assign-inspection`.
   * Trạng thái báo cáo gốc vẫn là `InProgress`.
   * Trạng thái của `InspectionReport` là `Draft` hoặc `Assigned`.

2. **Chấp nhận / Từ chối (Decline) (BR-INS-003):**
   * Inspector Team Leader có `24 giờ` để từ chối task (với lý do). Nếu từ chối, hồ sơ quay về cho LEO xử lý lại. Nếu không, coi như đã nhận.

3. **Check-In Hiện Trường (BR-INS-004):**
   * Tương tự như CleanUp, Inspector phải check-in trong bán kính `200m`. Trạng thái của Inspection chuyển sang `InField`.

4. **Cập nhật bằng chứng (BR-INS-010):**
   * Inspector thu thập lời khai, giấy tờ, và **bắt buộc upload ít nhất 2 ảnh hiện trường** trước khi có thể ra quyết định phạt.

5. **Đóng không vi phạm (Close No Violation) (BR-INS-013):**
   * Nếu sau khi kiểm tra, Inspector xác định không có vi phạm, họ có thể đóng hồ sơ với lý do (ít nhất 50 ký tự). Trạng thái `ClosedNoViolation`.

6. **Ra quyết định xử phạt (Issue Penalty) & PenaltyPayment (BR-INS-020):**
   * Nếu có vi phạm, Inspector tạo/liên kết một `ViolatingEntity`.
   * Nhập số tiền phạt, số biên bản, ngày đến hạn nộp phạt.
   * Trạng thái Inspection chuyển sang `PenaltyIssued`.
   * **Penalty Payment (Nộp phạt):**
     * Đối tượng vi phạm đến phường/xã nộp phạt. Inspector ghi nhận các khoản thu (`PenaltyPayment`).
     * `PaidAmount` được cộng dồn.
     * Nếu nộp đủ tiền phạt (`PaidAmount >= PenaltyAmount`), trạng thái tự động thành `Paid`. Nếu chưa đủ, thành `PartiallyPaid`.

7. **Đóng Hồ Sơ (Close):**
   * Khi trạng thái là `Paid`, Inspector có thể đóng hồ sơ hoàn toàn (`Closed`).
   * Nếu quá hạn nộp phạt, background job sẽ tự động đổi trạng thái thành `Overdue` (BR-INS-021).

### 2.3. Tóm tắt luồng State Machine của Inspection

`Assigned` -> `InField` -> (Thu thập bằng chứng) -> `PenaltyIssued` -> (`PartiallyPaid`) -> `Paid` -> `Closed`.
*(Hoặc rẽ nhánh sang `ClosedNoViolation` nếu không có tội, hoặc `Overdue` nếu chậm nộp phạt).*
