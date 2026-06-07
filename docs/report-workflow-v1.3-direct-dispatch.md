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

| Đặc điểm | v1.2 (cũ) | v1.3 (mới) |
|---|---|---|
| Routing | DEO dispatch thủ công | Auto-route bằng WardCode |
| Xác minh | DEO | **LEO** (cấp xã/phường) |
| Phân công team | DEO | **LEO** (hoặc CompanyManager cho đội công ty) |
| Trạng thái `Dispatched`/`Assigned` | Có | **Loại bỏ** |
| InspectionReport | Gộp trong Report | **Tách sub-process riêng** |
| Company Manager | Không có | **Có dashboard riêng** |

---

## 2. Actors & Vai Trò Trong Luồng

| Actor | Vai trò trong luồng |
|---|---|
| **Citizen** | Gửi báo cáo (có ảnh + GPS), theo dõi trạng thái, đóng báo cáo |
| **LEO** (Local Environmental Officer) | Xác minh báo cáo, phân công team xử lý (đội cộng đồng hoặc công ty), lập InspectionReport nếu vi phạm |
| **DEO** (Department Environmental Officer) | Quản lý fallback queue (báo cáo ở phường chưa onboard), quản lý hợp đồng công ty |
| **Company Manager (CM)** | Nhận task từ LEO, phân công cho đội dọn dẹp của công ty, theo dõi dashboard |
| **Cleaner** | Thành viên CleanupTeam (cộng đồng), nhận task → accept → cập nhật tiến độ → resolve |
| **Company Staff (CS)** | Nhân viên công ty, luồng xử lý giống Cleaner |

---

## 3. State Machine

### 3.1 Report Lifecycle (Umbrella — Nhánh Dọn Dẹp)

```
                   ┌─► Rejected   (LEO, reason ≥ 20 chars)
Submitted ─────────┼─► Verified ──► InProgress ──► Resolved ──┬─► Closed (Citizen confirm OR auto 7d)
                   └─► Duplicate  (LEO/AI)                     └─► InProgress (re-open, max 2 lần)
```

**Enum `ReportStatus`:** `Submitted` → `Verified` → `InProgress` → `Resolved` → `Closed` | `Rejected` | `Duplicate`

### 3.2 InspectionReport Lifecycle (Sub-process — Nhánh Xử Phạt)

```
Draft ──► PenaltyIssued ──► (Paid / PartiallyPaid / Overdue) ──► Closed
                                                 └─► Draft → Closed (CloseNoViolation)
```

**Enum `InspectionStatus`:** `Draft` → `PenaltyIssued` → `Paid` / `PartiallyPaid` / `Overdue` → `Closed`

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

### Phase 3: LEO Phân Công Team

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

1. LEO xem danh sách team rảnh (`/teams?isAvailable=true`)
2. LEO chọn 1 hoặc nhiều team → assign
3. **Dispatch by need**: LEO tự quyết loại team (CleanupTeam / InspectionTeam), không ràng buộc bởi category
4. Report chuyển `Verified → InProgress`

> **Lưu ý:** LEO có thể assign team **cộng đồng** (CleanupTeam thuộc LocalOffice) **HOẶC** team **công ty** (thuộc EnvironmentalServiceCompany). Cả hai loại đều dùng cùng endpoint.

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

## 5. Company Manager Dashboard (Mới v1.3)

### 5.1 Luồng Company Manager

CompanyManager (CM) quản lý đội dọn dẹp thuộc công ty dịch vụ môi trường. Luồng:

```
┌──────────────┐
│ DEO tạo      │─── POST /v1/companies ──────────► Company created (PendingActivation)
│ Company      │─── POST /v1/companies/{id}/token ► Generate activation token
└──────────────┘

┌──────────────┐
│ CM nhận      │─── POST /v1/companies/activate ──► Company: Active
│ token, kích  │    (token hash match + not expired)
│ hoạt         │
└──────────────┘

┌──────────────┐
│ CM quản lý   │─── POST /v1/companies/my/staff ──► Thêm nhân viên (Company Staff)
│ nhân sự      │─── GET  /v1/companies/my/staff ──► Danh sách nhân viên
│              │─── DELETE /v1/companies/my/staff/{id} ► Xóa nhân viên
└──────────────┘
```

### 5.2 CM Nhận Task Từ LEO

Khi LEO assign team **công ty** cho một report, CompanyManager sẽ thấy task trên dashboard:

```
┌──────────────┐     GET /v1/teams/my-tasks
│ Company      │───────────────────────────────────► Tasks assigned to company teams
│ Manager      │
│              │     (CM delegate cho Company Staff xử lý thực địa)
│              │
│ Company      │     PUT /v1/teams/my-tasks/{id}/accept
│ Staff        │───────────────────────────────────► Accept & execute task
│              │     PUT /v1/reports/{id}/update-progress
│              │     PUT /v1/reports/{id}/resolve
└──────────────┘
```

> **Lưu ý quan trọng:** CompanyManager **KHÔNG** tạo team. Team thuộc LocalOffice, do **LEO** tạo. LEO cũng là người assign team cho report. CompanyManager chỉ quản lý **nhân sự** trong công ty.

### 5.3 Contract-Window Authorization

Mọi request từ CompanyManager / CompanyStaff chỉ được chấp nhận khi:
- `Company.Status == Active`
- `now ∈ [ContractStartDate, ContractEndDate]`
- Token chưa hết hạn

---

## 6. API Endpoints Theo Thứ Tự Sử Dụng

### 🔵 Phase 0: Setup (Admin / DEO)

| # | Method | Endpoint | Actor | Mô tả |
|---|--------|----------|-------|-------|
| 0.1 | POST | `/v1/auth/register` | Public | Đăng ký tài khoản Citizen |
| 0.2 | POST | `/v1/auth/login` | Public | Đăng nhập, nhận JWT |

### 🟢 Phase 1: Citizen Submit Report

| # | Method | Endpoint | Actor | Mô tả |
|---|--------|----------|-------|-------|
| 1.1 | POST | `/v1/reports/analyze` | Citizen | Upload ảnh → AI phân tích |
| 1.2 | GET | `/v1/catalog/categories` | Citizen | Lấy danh mục ô nhiễm |
| 1.3 | POST | `/v1/reports` | Citizen | Tạo báo cáo (kèm ảnh, GPS, category) |
| 1.4 | GET | `/v1/reports/my` | Citizen | Xem báo cáo của tôi |
| 1.5 | GET | `/v1/reports/{id}` | Citizen | Chi tiết báo cáo |

### 🟡 Phase 2: LEO Xác Minh & Phân Công

| # | Method | Endpoint | Actor | Mô tả |
|---|--------|----------|-------|-------|
| 2.1 | GET | `/v1/reports/queue` | LEO | Xem hàng đợi báo cáo chờ xử lý |
| 2.2 | GET | `/v1/reports/{id}` | LEO | Xem chi tiết báo cáo |
| 2.3 | PUT | `/v1/reports/{id}/verify` | LEO | Xác minh (Submitted → Verified) |
| 2.4 | PUT | `/v1/reports/{id}/reject` | LEO | Từ chối (Submitted → Rejected) |
| 2.5 | GET | `/v1/teams?isAvailable=true` | LEO | Xem team rảnh |
| 2.6 | POST | `/v1/reports/{id}/assign` | LEO | Phân công team (Verified → InProgress) |
| 2.7 | GET | `/v1/reports/progress-board` | LEO | Board tổng quan InProgress |
| 2.8 | GET | `/v1/reports/{id}/progress` | LEO | Chi tiết tiến trình |
| 2.9 | PUT | `/v1/reports/{id}/reassign` | LEO | Chuyển team (nếu cần) |

### 🔴 Phase 3: Team Xử Lý (Cleaner / Company Staff)

| # | Method | Endpoint | Actor | Mô tả |
|---|--------|----------|-------|-------|
| 3.1 | GET | `/v1/teams/my-profile` | Cleaner/CS | Xem profile team |
| 3.2 | GET | `/v1/teams/my-tasks` | Cleaner/CS | Danh sách task |
| 3.3 | GET | `/v1/teams/my-tasks/{reportId}` | Cleaner/CS | Chi tiết task |
| 3.4 | PUT | `/v1/teams/my-tasks/{reportId}/accept` | Cleaner/CS | Chấp nhận task |
| 3.5 | PUT | `/v1/teams/my-tasks/{reportId}/decline` | Cleaner/CS | Từ chối task (trong 2h) |
| 3.6 | PUT | `/v1/reports/{id}/update-progress` | Cleaner/CS | Cập nhật tiến độ + ảnh |
| 3.7 | PUT | `/v1/reports/{id}/resolve` | Cleaner/CS | Hoàn thành xử lý |

### 🟣 Phase 4: Citizen Close

| # | Method | Endpoint | Actor | Mô tả |
|---|--------|----------|-------|-------|
| 4.1 | PUT | `/v1/reports/{id}/close` | Citizen | Đóng báo cáo (Resolved → Closed) |
| 4.2 | PUT | `/v1/reports/{id}/reopen` | Citizen | Mở lại (Resolved → InProgress, max 2 lần) |

### ⚪ Phase 5: LEO Staff & Team Management

| # | Method | Endpoint | Actor | Mô tả |
|---|--------|----------|-------|-------|
| 5.1 | POST | `/v1/teams` | LEO | Tạo team (Cleanup / Inspection) |
| 5.2 | PUT | `/v1/teams/{id}` | LEO | Cập nhật team |
| 5.3 | POST | `/v1/teams/{teamId}/members` | LEO | Thêm thành viên |
| 5.4 | DELETE | `/v1/teams/{teamId}/members/{userId}` | LEO | Xóa thành viên |
| 5.5 | PUT | `/v1/teams/{teamId}/members/{userId}/transfer` | LEO | Chuyển team |

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
| Cột | Bảng | Lý do |
|-----|------|-------|
| `dispatched_at` | `reports` | Không còn tầng dispatch |
| `dispatched_by_id` | `reports` | Không còn DEO dispatch |
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

| Bảng | Mô tả | Quan hệ |
|------|-------|---------|
| `inspection_reports` | Sub-process xử phạt | FK → `reports`, `users` |
| `environmental_service_companies` | Công ty dịch vụ MT | FK → `departments` |
| `company_staff` | Nhân viên công ty | FK → `users`, `companies` |

---

## 11. Checklist Tích Hợp FE

- [ ] Bỏ màn hình DEO Dispatch
- [ ] LEO Dashboard: thêm nút Verify/Reject cho report Submitted
- [ ] LEO Dashboard: thêm modal assign team (chọn team + note)
- [ ] Company Manager Dashboard: hiển thị tasks assigned cho đội công ty
- [ ] Cleaner/CS: giữ nguyên luồng accept → progress → resolve
- [ ] Citizen: giữ nguyên luồng close/reopen
- [ ] Cập nhật status filter: bỏ Dispatched, Assigned, PenaltyIssued, ClosedNoViolation
