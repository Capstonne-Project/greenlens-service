# Tóm tắt nhánh `feat/report-lifecycle-hardening`

> Từ commit đầu tiên (`2a2399b`) đến hiện tại (bao gồm uncommitted changes)

---

## 1. Báo cáo ô nhiễm — Hardening & Lifecycle

- Thêm cảnh báo quá hạn (overdue alerts) cho báo cáo chưa được xử lý
- Bắt buộc ảnh "trước khi dọn" (before images) khi nhận task
- Chặn xóa báo cáo đã có assignment hoặc đã xử lý (delete guard)
- Giới hạn cửa sổ mở lại báo cáo (reopen window) sau khi Resolved

## 2. Đánh giá hài lòng & Bản nháp

- Citizen đánh giá mức hài lòng (1–5 sao) sau khi báo cáo Resolved
- Lưu bản nháp (draft) báo cáo — tạo, sửa, xóa, xem danh sách draft
- Tự động dọn draft quá 30 ngày

**API mới:**

- `POST /v1/reports/{id}/rate` — đánh giá hài lòng
- `POST /v1/reports/drafts` — lưu draft
- `GET /v1/reports/drafts` — danh sách draft
- `DELETE /v1/reports/drafts/{id}` — xóa draft

## 3. Officer Dashboard — SLA, KPI, Export

- Thông báo vi phạm SLA cho officer (push notification khi report gần/quá hạn)
- Job tính điểm ưu tiên (priority score) cho báo cáo chưa xử lý
- KPI officer: tổng tiếp nhận, hoàn thành, tỉ lệ SLA, thời gian xử lý TB
- Xuất danh sách báo cáo ra CSV hoặc Excel (ClosedXML)

**API mới:**

- `GET /v1/officers/kpi` — thống kê KPI cá nhân (hỗ trợ lọc theo tháng/quý/năm)
- `GET /v1/reports/export?format=csv|xlsx` — xuất báo cáo

## 4. Quyền riêng tư dữ liệu

- Người dùng đồng ý điều khoản dữ liệu (consent) trước khi gửi báo cáo
- Job tự động xóa ảnh/video > 2 năm (data retention)
- Xuất dữ liệu cá nhân (GDPR/NĐ-13) ra JSON hoặc CSV

**API mới:**

- `POST /v1/users/consent` — chấp nhận điều khoản dữ liệu
- `GET /v1/users/me/export?format=json|csv` — xuất dữ liệu cá nhân

## 5. Refactor — Tách Repository

- Tách `IReportDraftRepository` và `IReportSatisfactionRepository` ra khỏi `IGenericRepository` để tuân thủ convention DI (fix lỗi startup)
- Sửa 4 handler liên quan, đăng ký DI

## 6. Quản lý vòng đời Công ty DVMT

- Tạm ngưng công ty (Suspend): tự động từ chối task đang chờ, đưa báo cáo về trạng thái Verified
- Chấm dứt hợp đồng (Terminate): tương tự Suspend + khóa vĩnh viễn
- Kích hoạt lại công ty (Reactivate) sau khi tạm ngưng
- Tự động hết hạn (Expire) công ty Bidding khi hết hợp đồng + cảnh báo 30/7/1 ngày trước
- Giới hạn workload: tối đa 6 task/đội, cảnh báo khi đạt 5

**API mới:**

- `POST /v1/companies/{id}/suspend` — tạm ngưng công ty
- `POST /v1/companies/{id}/terminate` — chấm dứt hợp đồng
- `POST /v1/companies/{id}/reactivate` — kích hoạt lại

## 7. Gia hạn hợp đồng & Lịch sử _(chưa commit)_

- Entity `ContractPeriod` lưu lịch sử từng kỳ hợp đồng
- Gia hạn hợp đồng Bidding: tạo kỳ mới, cập nhật ngày kết thúc, tự động kích hoạt lại nếu đang Expired
- Xem lịch sử các kỳ hợp đồng (DEO xem any, CM xem công ty mình)
- Migration tự động seed kỳ ban đầu cho các công ty đã tồn tại

**API mới:**

- `POST /v1/companies/{id}/renew-contract` — gia hạn hợp đồng
- `GET /v1/companies/{id}/contract-history` — lịch sử kỳ HĐ (DEO)
- `GET /v1/companies/my/contract-history` — lịch sử kỳ HĐ (CM)

## 8. KPI Công ty _(chưa commit)_

- Thống kê hiệu suất công ty: số task tiếp nhận/hoàn thành/từ chối, tỉ lệ đúng SLA, thời gian xử lý TB
- DEO xem theo công ty, CM tự động xem công ty mình

**API mới:**

- `GET /v1/companies/{id}/kpi` — KPI công ty (DEO)
- `GET /v1/companies/my/kpi` — KPI công ty tôi (CM)

## 9. Kiểm tra phân tách dữ liệu _(chưa commit)_

- Audit toàn bộ 11 handler CM/CompanyStaff — xác nhận mọi truy vấn đều lọc theo companyId từ token, không có lỗ hổng truy cập chéo công ty

## 10. Bổ sung ảnh citizen vào danh sách báo cáo

- Thêm `FirstImageUrl` (thumbnail ảnh đầu tiên citizen chụp) vào response của 2 API danh sách:
  - `GET /v1/reports/queue` — hàng đợi báo cáo cho LEO/DEO (`OfficerQueueItem`)
  - `GET /v1/departments/my/reports` — tất cả báo cáo trong department (`DepartmentReportItem`)
- Logic: lấy ảnh `MediaType.Image` đầu tiên (theo `UploadedAt`), ưu tiên `ThumbnailUrl`, fallback `Url`
- Không thêm API mới, chỉ mở rộng response DTO có sẵn

---

## Tổng hợp API mới trên nhánh

| #   | Method | Endpoint                              | Mô tả                        |
| --- | ------ | ------------------------------------- | ---------------------------- |
| 1   | POST   | `/v1/reports/{id}/rate`               | Đánh giá hài lòng            |
| 2   | POST   | `/v1/reports/drafts`                  | Lưu bản nháp                 |
| 3   | GET    | `/v1/reports/drafts`                  | Danh sách bản nháp           |
| 4   | DELETE | `/v1/reports/drafts/{id}`             | Xóa bản nháp                 |
| 5   | GET    | `/v1/officers/kpi`                    | KPI officer                  |
| 6   | GET    | `/v1/reports/export`                  | Xuất báo cáo CSV/Excel       |
| 7   | POST   | `/v1/users/consent`                   | Chấp nhận điều khoản dữ liệu |
| 8   | GET    | `/v1/users/me/export`                 | Xuất dữ liệu cá nhân         |
| 9   | POST   | `/v1/companies/{id}/suspend`          | Tạm ngưng công ty            |
| 10  | POST   | `/v1/companies/{id}/terminate`        | Chấm dứt hợp đồng            |
| 11  | POST   | `/v1/companies/{id}/reactivate`       | Kích hoạt lại                |
| 12  | POST   | `/v1/companies/{id}/renew-contract`   | Gia hạn hợp đồng             |
| 13  | GET    | `/v1/companies/{id}/contract-history` | Lịch sử kỳ HĐ (DEO)          |
| 14  | GET    | `/v1/companies/my/contract-history`   | Lịch sử kỳ HĐ (CM)           |
| 15  | GET    | `/v1/companies/{id}/kpi`              | KPI công ty (DEO)            |
| 16  | GET    | `/v1/companies/my/kpi`                | KPI công ty (CM)             |

## Background Jobs mới

| Job                        | Lịch            | Mô tả                                              |
| -------------------------- | --------------- | -------------------------------------------------- |
| `CompanyContractExpiryJob` | Daily 02:00 UTC | Auto-expire Bidding hết hạn + cảnh báo 30/7/1 ngày |
| `DataRetentionJob`         | Daily           | Xóa ảnh/video > 2 năm                              |
| `DraftCleanupJob`          | Daily           | Dọn draft > 30 ngày                                |

## Migration mới

| Migration                            | Mô tả                                |
| ------------------------------------ | ------------------------------------ |
| `AddCompanyLastExpiryWarningAt`      | Cột theo dõi cảnh báo hết hạn        |
| `AddContractPeriods` _(chưa commit)_ | Bảng lịch sử kỳ hợp đồng + data seed |

---

## 10. Administration Module (BR-ADM-001..012)

### 10.1. Penalty Framework — BR-ADM-008

- Entity `PenaltyFramework`: khung mức phạt theo loại ô nhiễm + cấp vi phạm
- CRUD đầy đủ: Create, Update, Get (paged), Deactivate/Activate
- Unique constraint: 1 active entry per (CategoryId, ViolationLevel)

### 10.2. Audit Log — BR-ADM-010 (cross-cutting)

- Entity `AuditLog` (immutable): ghi log hành động nhạy cảm kèm OldValues/NewValues JSON
- MediatR pipeline behavior `AuditLogBehavior` — tự động ghi log mọi Command implement `IAuditable`
- Commands đã gắn `IAuditable`: UpdateUserRole, ForceUpdateReportStatus, ToggleBanUser, SuspendCompany, TerminateCompany
- Infrastructure `AuditLogger`: resolve IP, UserAgent, serialize command payload

### 10.3. Content Moderation — BR-ADM-006

- Report entity: thêm `IsHidden`, `HiddenAt`, `HiddenBy`, `HiddenReason` + `Hide()`/`Unhide()`
- Admin có thể ẩn báo cáo vi phạm khỏi public (reversible soft-hide ≠ soft-delete)
- Các public query đã filter `IsHidden`: GetReports, GetReportById, GetPublicMapReports, GetMapViewportSummary

### 10.4. Spam Dashboard — BR-ADM-007

- Heuristic-based SQL: submit ≥5/h, rejected ≥3/7d, AI flagged ≥2
- Configurable thresholds qua query params
- Không gọi AI realtime — đọc flag `IsSuspicious` có sẵn trong Report

### 10.5. Notification Templates — BR-ADM-004

- Entity `NotificationTemplate`: template key, i18n (Vi/En), channel, type, publish lifecycle
- Placeholder validation whitelist: `{user_name}`, `{report_id}`, `{priority}`, `{status}`, ...
- Tính năng "Test gửi trước khi publish" — render placeholder + gửi thử đến admin

### 10.6. Gamification Config — BR-ADM-005

- Entity `GamificationConfig`: admin chỉnh số điểm cho mỗi hành động
- Seed data mặc định 6 PointReason (ReportVerified=10, ReportResolved=20, ...)
- Event handlers đã dùng config thay vì hardcoded — fallback nếu chưa có config

### 10.7. Company Monitoring — BR-ADM-012

- `GetCompaniesQueryHandler` sửa scope: DEO chỉ thấy công ty có ServiceArea trong tỉnh mình

**API mới (16 endpoints):**

| #   | Method | Endpoint                                      | Mô tả                         |
| --- | ------ | --------------------------------------------- | ------------------------------ |
| 1   | GET    | `/v1/admin/penalty-frameworks`                | Danh sách khung phạt           |
| 2   | POST   | `/v1/admin/penalty-frameworks`                | Tạo khung phạt                 |
| 3   | PUT    | `/v1/admin/penalty-frameworks/{id}`           | Cập nhật khung phạt            |
| 4   | PATCH  | `/v1/admin/penalty-frameworks/{id}/toggle`    | Bật/tắt khung phạt             |
| 5   | GET    | `/v1/admin/audit-logs`                        | Danh sách audit log            |
| 6   | GET    | `/v1/admin/audit-logs/{id}`                   | Chi tiết audit log             |
| 7   | POST   | `/v1/admin/reports/{id}/hide`                 | Ẩn báo cáo vi phạm            |
| 8   | POST   | `/v1/admin/reports/{id}/unhide`               | Hiện lại báo cáo               |
| 9   | GET    | `/v1/admin/spam-suspects`                     | Spam dashboard                 |
| 10  | GET    | `/v1/admin/gamification-configs`              | Cấu hình điểm gamification    |
| 11  | PUT    | `/v1/admin/gamification-configs/{id}`         | Cập nhật điểm                  |
| 12  | GET    | `/v1/admin/notification-templates`            | Danh sách template             |
| 13  | POST   | `/v1/admin/notification-templates`            | Tạo template                   |
| 14  | PATCH  | `/v1/admin/notification-templates/{id}/publish` | Publish/Unpublish            |
| 15  | POST   | `/v1/admin/notification-templates/{id}/test`  | Test gửi template              |

**Entity mới:**

| Entity                   | Table                    | Mô tả                                      |
| ------------------------ | ------------------------ | ------------------------------------------- |
| `PenaltyFramework`       | `penalty_frameworks`     | Khung mức phạt theo category + level        |
| `AuditLog`               | `audit_logs`             | Log hành động nhạy cảm (immutable)          |
| `GamificationConfig`     | `gamification_configs`   | Cấu hình điểm cho mỗi action               |
| `NotificationTemplate`   | `notification_templates` | Template thông báo với placeholders         |

**Migration pending**: chạy `dotnet ef migrations add AddAdminModule` sau khi merge.
