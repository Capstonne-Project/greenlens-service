# Luồng Xử Lý Báo Cáo Ô Nhiễm v1.3 — Direct-to-Local-Office Dispatch

> **Phiên bản:** v1.3 (07/06/2026)
> **Thay đổi chính:** Loại bỏ tầng DEO Dispatch trung gian, auto-routing GPS→LocalOffice, LEO trực tiếp xác minh & điều phối, CompanyManager quản lý dashboard riêng.

---

## 1. Tổng Quan Kiến Trúc Mới

### Trước (v1.2 — DEO Dispatch)

```
Citizen → Submit → DEO review & dispatch → LEO verify → Team xử lý
```

### Sau (v1.3 — Direct-to-Local-Office)

```
Citizen → Submit (auto-route GPS→Ward→LocalOffice) → LEO verify & assign → Team xử lý
```

### Điểm khác biệt quan trọng

| Đặc điểm                           | v1.2 (cũ)             | v1.3 (mới)                                    |
| ---------------------------------- | --------------------- | --------------------------------------------- |
| Routing                            | DEO dispatch thủ công | Auto-route bằng WardCode                      |
| Xác minh                           | DEO                   | **LEO** (cấp xã/phường)                       |
| Phân công team                     | DEO                   | **LEO** (hoặc CompanyManager cho đội công ty) |
| Trạng thái `Dispatched`/`Assigned` | Có                    | **Loại bỏ**                                   |
| InspectionReport                   | Gộp trong Report      | **Tách sub-process riêng**                    |
| Company Manager                    | Không có              | **Có dashboard riêng**                        |

---

## 2. Actors & Vai Trò Trong Luồng

| Actor                                      | Vai trò trong luồng                                                                                                                      |
| ------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------- |
| **Citizen**                                | Gửi báo cáo (có ảnh + GPS), theo dõi trạng thái, đóng báo cáo                                                                            |
| **LEO** (Local Environmental Officer)      | Xác minh báo cáo, phân công **đội cộng đồng** trực tiếp, điều phối task sang **công ty**, quản lý InspectionTeam (đội xử phạt phường/xã) |
| **DEO** (Department Environmental Officer) | Quản lý fallback queue (báo cáo ở phường chưa onboard), quản lý hợp đồng công ty                                                         |
| **Company Manager (CM)**                   | Nhận task từ LEO, **CRUD + quản lý team công ty**, phân công team công ty xử lý, theo dõi dashboard                                      |
| **Cleaner**                                | Thành viên CleanupTeam (cộng đồng hoặc công ty), nhận task → accept → cập nhật tiến độ → resolve                                         |
| **Company Staff (CS)**                     | Nhân viên công ty, luồng xử lý giống Cleaner                                                                                             |

---

## 3. State Machine

### 3.1 Report Lifecycle (Umbrella — Nhánh Dọn Dẹp)

```
                   ┌─► Rejected   (LEO, reason ≥ 20 chars)
Submitted ─────────┼─► Verified ──┬─► InProgress ──► Resolved ──┬─► Closed (Citizen confirm OR auto 7d)
                   └─► Duplicate  │  (LEO/AI)                   └─► InProgress (re-open, max 2 lần)
                                  │
                                  ├─► [Community team] LEO assign trực tiếp → InProgress
                                  │
                                  └─► [Company team] LEO dispatch to Company → Verified (giữ nguyên)
                                       └─► CM assign company team → InProgress
```

**Enum `ReportStatus`:** `Submitted` → `Verified` → `InProgress` → `Resolved` → `Closed` | `Rejected` | `Duplicate`

> **Quan trọng:** Khi LEO dispatch sang company, report **giữ Verified** + set `AssignedCompanyId`. CompanyManager filter bằng `Status == Verified AND AssignedCompanyId == myCompanyId`.

### 3.2 InspectionReport Lifecycle (Sub-process — Nhánh Xử Phạt)

```
Draft ──► PenaltyIssued ──► (Paid / PartiallyPaid / Overdue) ──► Closed
                                                 └─► Draft → Closed (CloseNoViolation)
```

### 3.3 Assignment Lifecycle (Task gán cho Team)

```
Assigned ──► InProgress ──► Completed
         └─► Declined (trong 2h, reason ≥ 20 chars)
```

---

## 4. Luồng Xử Lý Chi Tiết (Từng Bước)

### Phase 1: Citizen Submit

```
┌──────────────┐     POST /v1/reports/analyze     ┌───────────┐
│   Citizen    │──────────────────────────────────►│  AI phân  │
│   (Mobile)   │◄─────── suggestedCategory ───────│   tích    │
│              │                                   └───────────┘
│              │     POST /v1/reports
│              │──────────────────────────────────► Report created
│              │◄─────── reportId, code ──────────  Status: Submitted
└──────────────┘                                    Auto-route: WardCode → LocalOffice
```

1. Citizen upload ảnh → AI phân tích trả về `suggestedCategory`
2. Citizen submit báo cáo kèm GPS
3. Hệ thống tự động lookup `LocalOffice.WardCode == report.WardCode`
   - **Có office:** gán `AssignedOfficeId` + `AssignedDepartmentId`
   - **Chưa có office:** chỉ gán `AssignedDepartmentId` → rơi vào DEO fallback queue

### Phase 2: LEO Xác Minh

```
┌──────────────┐     GET /v1/reports/queue         ┌───────────┐
│     LEO      │──────────────────────────────────►│  Danh sách │
│  Dashboard   │◄─────── reports (Submitted) ──────│  chờ xác  │
│              │                                   │  minh     │
│              │     GET /v1/reports/{id}           └───────────┘
│              │──────────────────────────────────► Report detail
│              │
│              │     PUT /v1/reports/{id}/verify    (hoặc /reject)
│              │──────────────────────────────────► Status: Submitted → Verified
│              │     (option: override severity, category, wasteTagIds)
└──────────────┘
```

1. LEO mở dashboard → xem queue (`status=Submitted`)
2. LEO xem chi tiết báo cáo, kiểm tra ảnh + vị trí
3. LEO xác minh (`/verify`) hoặc từ chối (`/reject`)
   - Verify: có thể override `severity`, `categoryId`, thêm `wasteTagIds`
   - Reject: yêu cầu reason ≥ 20 ký tự

### Phase 3A: LEO Phân Công Team Cộng Đồng (Trực Tiếp)

```
┌──────────────┐     GET /v1/teams?isAvailable=true ┌───────────┐
│     LEO      │───────────────────────────────────►│ Teams rảnh │
│  Dashboard   │◄─────── available teams ───────────│           │
│              │                                    └───────────┘
│              │     POST /v1/reports/{id}/assign
│              │───────────────────────────────────► Status: Verified → InProgress
│              │     body: { teams: [{teamId, note}], wasteTagIds? }
└──────────────┘
```

1. LEO xem danh sách team **cộng đồng** rảnh (team có `CompanyId == null`)
2. LEO chọn 1 hoặc nhiều team → assign **trực tiếp**
3. Report chuyển `Verified → InProgress`

> **Guard:** LEO **KHÔNG THỂ** assign trực tiếp team của công ty qua endpoint này. Nếu team có `CompanyId != null` → trả lỗi `CANNOT_ASSIGN_COMPANY_TEAM_DIRECTLY`.

### Phase 3B: LEO Điều Phối Sang Công Ty → CM Phân Công

```
┌──────────────┐     POST /v1/reports/{id}/dispatch-to-company
│     LEO      │───────────────────────────────────► Report: AssignedCompanyId set
│              │     body: { companyId, note? }       Status: vẫn Verified
└──────────────┘

┌──────────────┐     GET /v1/reports/company-queue
│  Company     │───────────────────────────────────► Danh sách reports chờ phân công
│  Manager     │     (Status==Verified + AssignedCompanyId==myCompanyId)
│              │
│              │     POST /v1/reports/{id}/assign-company-team
│              │───────────────────────────────────► Status: Verified → InProgress
│              │     body: { teams: [{teamId, note}] }
└──────────────┘
```

1. LEO chọn công ty (trực thuộc hoặc đấu thầu) → dispatch task
2. Report **giữ Verified**, `AssignedCompanyId` được set
3. CompanyManager thấy task trên **company-queue**
4. CM chọn team của công ty → assign → Report chuyển `Verified → InProgress`

### Phase 4: Team Xử Lý (Cleaner / Company Staff)

```
┌──────────────┐     GET /v1/teams/my-tasks         ┌───────────┐
│  Cleaner /   │───────────────────────────────────►│  Tasks     │
│  Company     │◄─────── assignments ──────────────│  assigned  │
│  Staff       │                                    └───────────┘
│              │     PUT /v1/teams/my-tasks/{id}/accept
│              │───────────────────────────────────► Assignment: Assigned → InProgress
│              │
│              │     PUT /v1/reports/{id}/update-progress
│              │───────────────────────────────────► Upload ảnh, % hoàn thành
│              │
│              │     PUT /v1/reports/{id}/resolve
│              │───────────────────────────────────► Status: InProgress → Resolved
└──────────────┘
```

1. Team Leader xem task list (`/my-tasks`)
2. Accept task → Assignment: `Assigned → InProgress`
3. Cập nhật tiến độ nhiều lần (`/update-progress`)
4. Hoàn thành → Resolve report (`/resolve`)

> **Decline:** Team có thể từ chối trong vòng 2 giờ (reason ≥ 20 chars). Nếu **tất cả** team đều decline → report quay về `Verified` để LEO re-assign.

### Phase 5: Citizen Đóng Báo Cáo

```
┌──────────────┐     PUT /v1/reports/{id}/close
│   Citizen    │───────────────────────────────────► Status: Resolved → Closed
│              │     (hoặc auto-close sau 7 ngày)
└──────────────┘
```

---

## 5. Company Module (v1.3)

### 5.1 Loại Công Ty

| ContractType | Mô tả                                                               |
| ------------ | ------------------------------------------------------------------- |
| `Subsidiary` | Công ty **trực thuộc** (thuộc sở hữu/quản lý trực tiếp của Sở TNMT) |
| `Bidding`    | Công ty **đấu thầu** (ký hợp đồng thông qua đấu thầu công khai)     |

### 5.2 Onboarding Company

```
DEO ─── POST /v1/companies ──────────► Company created (PendingActivation, contractType)
    ─── POST /v1/companies/{id}/token ► Generate activation token

CM  ─── POST /v1/companies/activate ──► Company: Active
```

### 5.3 CM Quản Lý Nhân Sự & Team

```
CM ─── POST /v1/companies/my/staff ───► Thêm nhân viên
   ─── GET  /v1/companies/my/staff ───► Danh sách nhân viên
```

### 5.4 Luồng Nhận Task (Company Dispatch)

```
LEO ─── POST /reports/{id}/dispatch-to-company ──► AssignedCompanyId set, vẫn Verified
CM  ─── GET  /reports/company-queue ─────────────► Danh sách task chờ phân công
CM  ─── POST /reports/{id}/assign-company-team ──► Verified → InProgress
CS  ─── (accept → progress → resolve) ──────────► Giống Cleaner
```

> **Lưu ý:** Team công ty là `EnvironmentalTeam` với `CompanyId != null`. CompanyManager chỉ assign được team thuộc công ty mình.

### 5.5 Contract-Window Authorization

Mọi request từ CM/CS chỉ được chấp nhận khi:

- `Company.Status == Active`
- `now ∈ [ContractStartDate, ContractEndDate]`

---

## 6. API Endpoints Theo Thứ Tự Sử Dụng

### 🔵 Phase 0: Setup (Admin / DEO)

| #   | Method | Endpoint            | Actor  | Mô tả                     |
| --- | ------ | ------------------- | ------ | ------------------------- |
| 0.1 | POST   | `/v1/auth/register` | Public | Đăng ký tài khoản Citizen |
| 0.2 | POST   | `/v1/auth/login`    | Public | Đăng nhập, nhận JWT       |

### 🟢 Phase 1: Citizen Submit Report

| #   | Method | Endpoint                 | Actor   | Mô tả                                |
| --- | ------ | ------------------------ | ------- | ------------------------------------ |
| 1.1 | POST   | `/v1/reports/analyze`    | Citizen | Upload ảnh → AI phân tích            |
| 1.2 | GET    | `/v1/catalog/categories` | Citizen | Lấy danh mục ô nhiễm                 |
| 1.3 | POST   | `/v1/reports`            | Citizen | Tạo báo cáo (kèm ảnh, GPS, category) |
| 1.4 | GET    | `/v1/reports/my`         | Citizen | Xem báo cáo của tôi                  |
| 1.5 | GET    | `/v1/reports/{id}`       | Citizen | Chi tiết báo cáo                     |

### 🟡 Phase 2: LEO Xác Minh & Phân Công

| #   | Method | Endpoint                               | Actor | Mô tả                                                |
| --- | ------ | -------------------------------------- | ----- | ---------------------------------------------------- |
| 2.1 | GET    | `/v1/reports/queue`                    | LEO   | Xem hàng đợi báo cáo chờ xử lý                       |
| 2.2 | GET    | `/v1/reports/{id}`                     | LEO   | Xem chi tiết báo cáo                                 |
| 2.3 | PUT    | `/v1/reports/{id}/verify`              | LEO   | Xác minh (Submitted → Verified)                      |
| 2.4 | PUT    | `/v1/reports/{id}/reject`              | LEO   | Từ chối (Submitted → Rejected)                       |
| 2.5 | GET    | `/v1/teams?isAvailable=true`           | LEO   | Xem team rảnh                                        |
| 2.6 | POST   | `/v1/reports/{id}/assign`              | LEO   | Phân công **community** team (Verified → InProgress) |
| 2.7 | POST   | `/v1/reports/{id}/dispatch-to-company` | LEO   | Điều phối sang công ty (giữ Verified)                |
| 2.8 | GET    | `/v1/reports/progress-board`           | LEO   | Board tổng quan InProgress                           |
| 2.9 | PUT    | `/v1/reports/{id}/reassign`            | LEO   | Chuyển team (nếu cần)                                |

### 🏢 Phase 2.5: Company Manager Phân Công

| #     | Method | Endpoint                               | Actor | Mô tả                                          |
| ----- | ------ | -------------------------------------- | ----- | ---------------------------------------------- |
| 2.5.1 | GET    | `/v1/reports/company-queue`            | CM    | Xem task chờ phân công                         |
| 2.5.2 | POST   | `/v1/reports/{id}/assign-company-team` | CM    | Phân công team công ty (Verified → InProgress) |

### 🔴 Phase 3: Team Xử Lý (Cleaner / Company Staff)

| #   | Method | Endpoint                                | Actor      | Mô tả                   |
| --- | ------ | --------------------------------------- | ---------- | ----------------------- |
| 3.1 | GET    | `/v1/teams/my-profile`                  | Cleaner/CS | Xem profile team        |
| 3.2 | GET    | `/v1/teams/my-tasks`                    | Cleaner/CS | Danh sách task          |
| 3.3 | GET    | `/v1/teams/my-tasks/{reportId}`         | Cleaner/CS | Chi tiết task           |
| 3.4 | PUT    | `/v1/teams/my-tasks/{reportId}/accept`  | Cleaner/CS | Chấp nhận task          |
| 3.5 | PUT    | `/v1/teams/my-tasks/{reportId}/decline` | Cleaner/CS | Từ chối task (trong 2h) |
| 3.6 | PUT    | `/v1/reports/{id}/update-progress`      | Cleaner/CS | Cập nhật tiến độ + ảnh  |
| 3.7 | PUT    | `/v1/reports/{id}/resolve`              | Cleaner/CS | Hoàn thành xử lý        |

### 🟣 Phase 4: Citizen Close

| #   | Method | Endpoint                  | Actor   | Mô tả                                     |
| --- | ------ | ------------------------- | ------- | ----------------------------------------- |
| 4.1 | PUT    | `/v1/reports/{id}/close`  | Citizen | Đóng báo cáo (Resolved → Closed)          |
| 4.2 | PUT    | `/v1/reports/{id}/reopen` | Citizen | Mở lại (Resolved → InProgress, max 2 lần) |

### ⚪ Phase 5: LEO Staff & Team Management

| #   | Method | Endpoint                                       | Actor | Mô tả                           |
| --- | ------ | ---------------------------------------------- | ----- | ------------------------------- |
| 5.1 | POST   | `/v1/teams`                                    | LEO   | Tạo team (Cleanup / Inspection) |
| 5.2 | PUT    | `/v1/teams/{id}`                               | LEO   | Cập nhật team                   |
| 5.3 | POST   | `/v1/teams/{teamId}/members`                   | LEO   | Thêm thành viên                 |
| 5.4 | DELETE | `/v1/teams/{teamId}/members/{userId}`          | LEO   | Xóa thành viên                  |
| 5.5 | PUT    | `/v1/teams/{teamId}/members/{userId}/transfer` | LEO   | Chuyển team                     |

---

## 7. Sequence Diagram — Happy Path

```
Citizen          System          LEO            Team(Cleaner/CS)
  │                │               │               │
  │──POST /analyze─►               │               │
  │◄──AI result────│               │               │
  │                │               │               │
  │──POST /reports─►               │               │
  │                │──auto-route──►│               │
  │◄──201 Created──│               │               │
  │                │               │               │
  │                │  GET /queue   │               │
  │                │◄──────────────│               │
  │                │──reports──────►               │
  │                │               │               │
  │                │  PUT /verify  │               │
  │                │◄──────────────│               │
  │                │  Submitted→Verified           │
  │                │               │               │
  │                │  GET /teams   │               │
  │                │◄──────────────│               │
  │                │──available────►               │
  │                │               │               │
  │                │  POST /assign │               │
  │                │◄──────────────│               │
  │                │  Verified→InProgress          │
  │                │               │               │
  │                │               │  GET /my-tasks│
  │                │               │◄──────────────│
  │                │               │──tasks────────►
  │                │               │               │
  │                │               │  PUT /accept  │
  │                │               │◄──────────────│
  │                │               │               │
  │                │               │  PUT /progress│
  │                │               │◄──────────────│
  │                │               │               │
  │                │               │  PUT /resolve │
  │                │               │◄──────────────│
  │                │  InProgress→Resolved          │
  │                │               │               │
  │──PUT /close────►               │               │
  │                │  Resolved→Closed              │
  │◄──200 OK───────│               │               │
```

---

## 8. Fallback Queue (DEO)

Khi một phường/xã **chưa onboard** LocalOffice vào hệ thống:

- Report được gán `AssignedDepartmentId` nhưng **KHÔNG** có `AssignedOfficeId`
- Report rơi vào **DEO fallback queue**
- DEO xem queue: `GET /v1/reports/queue` (chỉ thấy báo cáo không có office)
- DEO xử lý thủ công hoặc onboard LocalOffice rồi reassign

---

## 9. Các Trường Đã Loại Bỏ (Breaking Changes)

### Database

| Cột                   | Bảng      | Lý do                   |
| --------------------- | --------- | ----------------------- |
| `dispatched_at`       | `reports` | Không còn tầng dispatch |
| `dispatched_by_id`    | `reports` | Không còn DEO dispatch  |
| `assigned_officer_id` | `reports` | Thay bằng `verified_by` |

### Enum values removed

- `ReportStatus.Dispatched`
- `ReportStatus.Assigned`
- `ReportStatus.PenaltyIssued` → chuyển sang `InspectionStatus`
- `ReportStatus.ClosedNoViolation` → chuyển sang `InspectionReport.CloseNoViolation()`

### Endpoints removed

- `PUT /v1/reports/{id}/dispatch` → **Removed** (auto-routing thay thế)
- `PUT /v1/reports/{id}/re-dispatch` → **Removed**
- `PUT /v1/reports/{id}/penalty` → **Moved** to InspectionReport module
- `PUT /v1/reports/{id}/close-no-violation` → **Moved** to InspectionReport module

---

## 10. Bảng Mới (Migration Bước 2)

| Bảng                              | Mô tả               | Quan hệ                   |
| --------------------------------- | ------------------- | ------------------------- |
| `inspection_reports`              | Sub-process xử phạt | FK → `reports`, `users`   |
| `environmental_service_companies` | Công ty dịch vụ MT  | FK → `departments`        |
| `company_staff`                   | Nhân viên công ty   | FK → `users`, `companies` |

---

## 11. Checklist Tích Hợp FE

- [ ] Bỏ màn hình DEO Dispatch
- [ ] LEO Dashboard: thêm nút Verify/Reject cho report Submitted
- [ ] LEO Dashboard: thêm modal assign team (chọn team + note)
- [ ] Company Manager Dashboard: hiển thị tasks assigned cho đội công ty
- [ ] Cleaner/CS: giữ nguyên luồng accept → progress → resolve
- [ ] Citizen: giữ nguyên luồng close/reopen
- [ ] Cập nhật status filter: bỏ Dispatched, Assigned, PenaltyIssued, ClosedNoViolation
