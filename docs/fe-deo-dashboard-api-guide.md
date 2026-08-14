# DEO Dashboard — API & UI Guide (Web FE)

> **Prefix:** `/v1` · **Envelope:** `{ code, message, status, data }`  
> **Role:** `DEO` (Điều phối viên cấp Tỉnh/TP — Sở TNMT)  
> **Phạm vi dữ liệu:** Mọi query báo cáo/analytics lọc theo `AssignedDepartmentId` = department của DEO đăng nhập.

**Tài liệu bổ sung:** [fe-deo-operations-api-guide.md](./fe-deo-operations-api-guide.md) — báo cáo (read-only), văn phòng MT, công ty DVMT (CRUD), export, duplicate/tái phạm, KPI LEO drill-down.

**Cơ chế scope (BE):** Department lấy từ `users.department_id` của JWT user — **không** nhận `departmentId` từ query string. Dashboard `/v1/dashboard/deo/*` dùng `DepartmentContextResolver`; báo cáo chi tiết/history dùng `ValidateReportAccess`; công ty dùng `CompanyAccessAuthorization`.

---

## 0. Vai trò DEO (2026-08 — read-only báo cáo)

| DEO **được** | DEO **không được** |
|---|---|
| Giám sát KPI, bản đồ, SLA toàn tỉnh | Verify / reject / assign / dispatch báo cáo |
| Xem danh sách & chi tiết báo cáo | `GET /v1/reports/queue` (hàng đợi LEO) |
| Xem cờ duplicate / tái phạm (read-only) | `POST confirm/dismiss duplicate`, `POST dismiss-violation-recurrence` |
| Export CSV/Excel toàn tỉnh (ẩn PII) | Ẩn comment (`POST comments/{id}/hide`) |
| Quản lý công ty DVMT (CRUD + hợp đồng) | Fallback queue / xử lý báo cáo mồ côi |

**Nguyên tắc UI:** Dashboard = **monitoring only**. Mọi nút hành động trên báo cáo (verify, giao việc, gộp trùng…) **ẩn** hoặc chuyển sang link “LEO xử lý”.

---

## 1. Cấu trúc navigation đề xuất

```
DEO Portal
├── 📊 Tổng quan          → /deo/dashboard
├── 📋 Báo cáo            → /deo/reports
│   └── Chi tiết          → /deo/reports/:id
├── 🏢 Công ty DVMT       → /deo/companies
│   └── Chi tiết          → /deo/companies/:id
├── 🏛 Văn phòng MT       → /deo/offices
├── 👮 Hiệu suất LEO      → /deo/officers
├── ⚠️ Cảnh báo           → /deo/alerts (hoặc panel trên dashboard)
└── 📥 Export             → modal trên /deo/reports
```

---

## 2. Màn hình Dashboard — `/deo/dashboard`

**Mục tiêu:** Một trang tổng hợp, lazy-load từng widget (12 API). Dùng **cùng bộ lọc thời gian** `from` / `to` (UTC, mặc định 30 ngày gần nhất) cho các widget có date range.

### 2.1 Layout wireframe

```
┌─────────────────────────────────────────────────────────────────────┐
│ [Tên Sở TNMT]  Bộ lọc: [30 ngày ▼] [Từ] [Đến] [Áp dụng]            │
├─────────────────────────────────────────────────────────────────────┤
│ KPI Cards (overview)                                                │
│ ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐ │
│ │ Tổng BC│ │ Chờ XL │ │ Đã XL  │ │ SLA ⚠ │ │ Trùng  │ │ Tái phạm│ │
│ └────────┘ └────────┘ └────────┘ └────────┘ └────────┘ └────────┘ │
│ ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐                         │
│ │ SLA %  │ │ TG XL  │ │ Cty ⚡ │ │ VP MT  │                         │
│ └────────┘ └────────┘ └────────┘ └────────┘                         │
├──────────────────────────────┬──────────────────────────────────────┤
│ Cảnh báo (alerts) — list     │ Phân bổ trạng thái (donut)           │
│ [High] SLA breach…           │ GET report-status                    │
│ [Med]  HĐ sắp hết hạn…       │                                      │
├──────────────────────────────┴──────────────────────────────────────┤
│ Xu hướng (line chart) — report-trend  groupBy=Day|Week|Month          │
├──────────────────────────────┬──────────────────────────────────────┤
│ Loại ô nhiễm (bar)           │ Funnel vòng đời (funnel chart)       │
│ pollution-analytics          │ report-funnel                        │
├──────────────────────────────┴──────────────────────────────────────┤
│ Bản đồ nhiệt (geographic) — heatmap + markers, click → /reports/:id   │
├──────────────────────────────┬──────────────────────────────────────┤
│ Tuổi hàng đợi (bar)          │ Phân bổ thời gian XL (histogram)     │
│ queue-aging                  │ resolution-distribution              │
├──────────────────────────────┴──────────────────────────────────────┤
│ Top công ty (table)          │ Top LEO (table)                      │
│ company-performance          │ officer-performance                  │
├──────────────────────────────────────────────────────────────────────┤
│ Hoạt động gần đây (timeline) — recent-activities, page=1 pageSize=10  │
└──────────────────────────────────────────────────────────────────────┘
```

### 2.2 Widget → API mapping

| Widget | Method | Endpoint | Query |
|--------|--------|----------|-------|
| KPI cards | GET | `/v1/dashboard/deo/overview` | `from`, `to` |
| Cảnh báo | GET | `/v1/dashboard/deo/alerts` | — |
| Donut trạng thái | GET | `/v1/dashboard/deo/report-status` | `from`, `to` |
| Line trend | GET | `/v1/dashboard/deo/report-trend` | `groupBy`, `from`, `to` |
| Bar category | GET | `/v1/dashboard/deo/pollution-analytics` | `from`, `to` |
| Funnel | GET | `/v1/dashboard/deo/report-funnel` | `from`, `to` |
| Map | GET | `/v1/dashboard/deo/geographic` | `from`, `to` |
| Queue aging | GET | `/v1/dashboard/deo/queue-aging` | — |
| Resolution hist | GET | `/v1/dashboard/deo/resolution-distribution` | `from`, `to` |
| Bảng công ty | GET | `/v1/dashboard/deo/company-performance` | `from`, `to` |
| Bảng LEO | GET | `/v1/dashboard/deo/officer-performance` | `from`, `to` |
| Timeline | GET | `/v1/dashboard/deo/recent-activities` | `page`, `pageSize` |

**Auth:** Tất cả endpoint trên yêu cầu `Authorization: Bearer {token}` role `DEO`.

### 2.3 Response shapes (tóm tắt)

**`overview`** → `DeoOverviewResponse`:

```json
{
  "departmentId": "uuid",
  "departmentName": "Sở TNMT TP.HCM",
  "totalReports": 1200,
  "pendingReports": 85,
  "resolvedReports": 980,
  "slaBreachedCount": 12,
  "duplicateFlagCount": 5,
  "recurrenceFlagCount": 3,
  "slaComplianceRate": 94.2,
  "averageResolutionHours": 36.5,
  "activeCompanies": 8,
  "pendingActivationCompanies": 1,
  "localOfficeCount": 24,
  "onboardedOfficeCount": 20
}
```

**`alerts`** → `AlertItem[]`:

```json
[
  { "code": "SLA_BREACH", "severity": "High", "message": "12 báo cáo vượt quá thời hạn SLA trong tỉnh." },
  { "code": "CONTRACT_EXPIRY", "severity": "Medium", "message": "2 hợp đồng công ty sắp hết hạn trong 30 ngày." }
]
```

Mã alert: `SLA_BREACH`, `OVERDUE_REPORTS`, `POSSIBLE_DUPLICATES`, `VIOLATION_RECURRENCE`, `PENDING_REOPEN`, `CONTRACT_EXPIRY`, `COMPANY_PENDING_ACTIVATION`.

Click alert → deep link:

| Code | Link UI |
|------|---------|
| `SLA_BREACH` | `/deo/reports?slaBreached=true` |
| `POSSIBLE_DUPLICATES` | `/deo/reports?isPossibleDuplicate=true` |
| `CONTRACT_EXPIRY` | `/deo/companies?status=Active` (highlight sắp hết hạn ở FE) |

**`report-trend`** → `ReportTrendItem[]`: `{ date, created, resolved }`

**`geographic`** → `{ heatmap: [{ latitude, longitude, weight }], markers: [{ reportId, latitude, longitude, status }] }`

### 2.4 Widget công ty & LEO & timeline (cập nhật BE 2026-08-13)

#### `GET /v1/dashboard/deo/company-performance`

Bảng hiệu suất **tất cả công ty DVMT** thuộc Sở của DEO — kể cả công ty chưa nhận task trong khoảng thời gian (metrics = 0).

| Query | Mặc định | Ghi chú |
|-------|----------|---------|
| `from` | `to − 30 ngày` | UTC |
| `to` | `now` | UTC |

**Logic scope:**

- Công ty: `environmental_service_companies.department_id` = department DEO.
- Task đếm theo báo cáo có `assigned_company_id` + `dispatched_to_company_at` nằm trong `[from, to]` và `assigned_department_id` = department DEO.
- Công ty chưa có dispatch trong range → vẫn có 1 dòng, KPI = 0.

**Response 200** → `CompanyPerformanceItem[]`:

```json
[
  {
    "companyId": "uuid",
    "companyName": "CITENCO TP.HCM",
    "assignedTasks": 12,
    "completedTasks": 10,
    "onTimeRate": 90.0,
    "slaRate": 91.7,
    "performanceScore": 91.2
  },
  {
    "companyId": "uuid",
    "companyName": "Green Clean Co.",
    "assignedTasks": 0,
    "completedTasks": 0,
    "onTimeRate": 0,
    "slaRate": 0,
    "performanceScore": 0
  }
]
```

| Field | Ý nghĩa |
|-------|---------|
| `assignedTasks` | Số báo cáo LEO dispatch cho công ty trong range |
| `completedTasks` | Trạng thái `Resolved` hoặc `Closed` |
| `onTimeRate` | % task hoàn thành trước `slaResolveDueAt` |
| `slaRate` | % task không bị `slaResolveBreached` |
| `performanceScore` | `0.6 × slaRate + 0.4 × onTimeRate` |

Sắp xếp: `performanceScore` giảm dần, tie-break `companyName` A→Z.

**Empty state FE:**

- `data: []` — Sở chưa có công ty nào (không phải lỗi).
- `data: [{ …, assignedTasks: 0, … }]` — có công ty nhưng chưa dispatch trong range → hiển thị bảng với số 0.

---

#### `GET /v1/dashboard/deo/officer-performance`

Bảng xếp hạng **LEO** theo báo cáo đã verify trong phạm vi tỉnh (widget “Top LEO” trên dashboard).

| Query | Mặc định | Ghi chú |
|-------|----------|---------|
| `from` | `to − 30 ngày` | UTC |
| `to` | `now` | UTC |

**Logic scope:** Chỉ báo cáo `assigned_department_id` = department DEO, có `verified_by` + `verified_at` trong `[from, to]`.

**Response 200** → `OfficerPerformanceItem[]`:

```json
[
  {
    "officerId": "uuid",
    "officerName": "Trần Văn B",
    "verifiedReports": 45,
    "averageHours": 4.5,
    "slaRate": 98.0,
    "score": 96.2
  }
]
```

| Field | Ý nghĩa |
|-------|---------|
| `verifiedReports` | Số báo cáo LEO đã verify |
| `averageHours` | TB giờ từ `createdAt` → `verifiedAt` |
| `slaRate` | % verify không `slaVerifyBreached` |
| `score` | `0.7 × slaRate + 0.3 × speedScore` (speed từ TB giờ vs SLA 24h) |

Chỉ LEO **có ít nhất 1 verify** trong range mới xuất hiện. Drill-down chi tiết: `GET /v1/reports/officer-kpi?officerId={uuid}` (mục 6).

---

#### `GET /v1/dashboard/deo/recent-activities`

Timeline sự kiện đổi trạng thái báo cáo trong tỉnh (đọc từ `report_status_history`).

| Query | Mặc định | Ghi chú |
|-------|----------|---------|
| `page` | `1` | ≥ 1 |
| `pageSize` | `20` | 1–100 |

**Logic scope:** History của báo cáo có `reports.assigned_department_id` = department DEO. Sắp xếp `createdAt` mới nhất trước.

**Response 200** → `RecentActivityItem[]`:

```json
[
  {
    "time": "2026-08-13T10:30:00Z",
    "type": "TeamAssigned",
    "description": "Report #REP-2026-0042 chuyển sang trạng thái InProgress"
  },
  {
    "time": "2026-08-12T08:00:00Z",
    "type": "OfficerVerified",
    "description": "Report #REP-2026-0041 chuyển sang trạng thái Verified (Đã xác minh hiện trường)"
  }
]
```

| `type` | Khi `toStatus` = |
|--------|------------------|
| `OfficerVerified` | `Verified` |
| `TeamAssigned` | `InProgress` |
| `ReportResolved` | `Resolved` |
| `ReportClosed` | `Closed` |
| `ReportRejected` | `Rejected` |
| `ReportMarkedDuplicate` | `Duplicate` |
| `StatusChanged` | Các trạng thái khác (`Submitted`, `Reopened`, …) |

**Lưu ý FE:**

- Dữ liệu history cũ (workflow v2: `Dispatched`, `Assigned`, …) BE map sang trạng thái v3 khi đọc — FE **chỉ** nhận enum hiện tại trong `description`, không cần xử lý `Dispatched`.
- `data: []` hợp lệ khi chưa có history trong tỉnh.
- Widget dashboard gợi ý `pageSize=10`; màn full timeline có thể `pageSize=20` + nút “Xem thêm”.

---

#### Lỗi chung (3 endpoint trên)

| HTTP | `code` | Khi nào |
|------|--------|---------|
| 401 | `UNAUTHORIZED` | Thiếu / hết hạn token |
| 403 | `FORBIDDEN` | Token không phải role `DEO` |
| 404 | `DEPARTMENT_NOT_FOUND` | User DEO chưa được gán `department_id` |

FE: 404 department → full-page “Chưa được gán Sở TNMT, liên hệ Admin” (không retry vô hạn).

---

## 3. Màn hình Báo cáo — `/deo/reports`

**API chính:** `GET /v1/departments/my/reports`

### 3.1 Wireframe

```
┌─────────────────────────────────────────────────────────────────────┐
│ Báo cáo toàn tỉnh                    [Export CSV ▼] [Export Excel]  │
├─────────────────────────────────────────────────────────────────────┤
│ 🔍 Tìm kiếm  │ Trạng thái ▼ │ Loại ▼ │ Mức độ ▼ │ Phường ▼ │ VP ▼ │
│ ☐ SLA breach │ ☐ Trùng lặp  │ ☐ Tái phạm │ ☐ Chờ reopen            │
├─────────────────────────────────────────────────────────────────────┤
│ Mã      │ Trạng thái │ Loại │ VP MT │ SLA │ Cờ      │ Ngày tạo    │
│ REP-…   │ Verified   │ Rác  │ P.1   │ ⚠   │ Dup     │ 01/06/2026  │
│ ...     │            │      │       │     │         │             │
├─────────────────────────────────────────────────────────────────────┤
│ pagination                                                          │
└─────────────────────────────────────────────────────────────────────┘
```

### 3.2 Query params

| Param | Mô tả |
|-------|--------|
| `page`, `pageSize` | Phân trang (max 100) |
| `search` | Mã, mô tả, địa chỉ, tên category |
| `status`, `categoryId`, `severity` | Lọc |
| `wardCode`, `assignedOfficeId` | Lọc địa bàn / văn phòng |
| `fromDate`, `toDate` | Khoảng thời gian tạo |
| `slaBreached` | `true` = có breach verify hoặc resolve |
| `isPossibleDuplicate`, `isSuspectedViolationRecurrence`, `hasPendingReopenRequest` | Cờ monitoring |
| `sortBy`, `sortDesc` | `code`, `status`, `severity`, `priority`, `createdAt`, … |

**Không có** nút Verify / Assign / Dispatch trên row — chỉ **Xem chi tiết**.

### 3.3 Export

`GET /v1/reports/export`

| Query | Ghi chú |
|-------|---------|
| `format` | `csv` hoặc `xlsx` |
| Cùng bộ filter như list | `status`, `severity`, `categoryId`, `wardCode`, `from`, `to`, flags |

DEO export **toàn tỉnh**, **không** có cột PII citizen (email/phone).

### 3.4 Chi tiết báo cáo — `/deo/reports/:id`

| API | Mục đích |
|-----|----------|
| `GET /v1/reports/{id}` | Full detail + media + assignments |
| `GET /v1/reports/{id}/history` | Timeline status |
| `GET /v1/reports/{id}/progress` | Tiến độ cleanup (lat/lng/address) |
| `GET /v1/reports/{id}/inspections` | Hồ sơ xử phạt liên quan (nếu có) |

**Tab read-only monitoring:**

- **Tổng quan:** map pin, status badge, SLA countdown
- **Lịch sử:** timeline từ `history`
- **Cờ đặc biệt:** nếu `isPossibleDuplicate` → link xem so sánh (mục 3.5)
- **Tiến độ:** progress timeline (không cho sửa)

### 3.5 Duplicate & tái phạm (chỉ xem)

| API | UI |
|-----|-----|
| `GET /v1/reports/duplicate-candidates` | List tab “Nghi trùng lặp” |
| `GET /v1/reports/{id}/duplicate-candidate-detail` | Side-by-side 2 báo cáo |
| `GET /v2/reports/duplicate-groups` | Nhóm theo báo cáo gốc (optional) |
| `GET /v1/reports/violation-recurrence-candidates` | List tab “Nghi tái phạm” |
| `GET /v1/reports/{id}/violation-recurrence-comparison` | So sánh với case Closed trước |

⚠️ **Không gọi** `POST .../confirm-duplicate`, `POST .../dismiss-duplicate`, `POST .../dismiss-violation-recurrence` — trả **403** với DEO.

---

## 4. Màn hình Công ty DVMT — `/deo/companies`

DEO **có quyền ghi** trên module công ty.

### 4.1 Danh sách

**API:** `GET /v1/companies`

| Query | |
|-------|---|
| `page`, `pageSize`, `search`, `status`, `sortBy`, `sortDesc` | |

**UI columns:** Tên, Mã HĐ, Trạng thái, Số phường, Hết hạn HĐ, KPI score (link detail).

Badge trạng thái: `PendingActivation`, `Active`, `Suspended`, `Expired`, `Terminated`.

### 4.2 Tạo công ty — wizard

**Bước 1 — Thông tin:** `POST /v1/companies`

```json
{
  "name": "CITENCO TP.HCM",
  "departmentId": "uuid-sở",
  "contractNumber": "HD-2026-001",
  "taxCode": "0123456789",
  "contractStartDate": "2026-01-01",
  "contractEndDate": "2027-01-01",
  "wardCodes": ["27145", "27146"],
  "managerEmail": "cm@citenco.vn",
  "managerFullName": "Nguyễn Văn A"
}
```

- `managerEmail` optional — có thể tạo CM sau qua `POST /v1/companies/{id}/manager`
- Response có `tempPassword` **1 lần** + email gửi tự động (BR-NTF-002)

**Bước 2 — Xác nhận:** hiển thị temp password + hướng dẫn CM đổi MK lần đầu.

### 4.3 Chi tiết công ty — `/deo/companies/:id`

| Tab | API |
|-----|-----|
| Thông tin | `GET /v1/companies/{id}` |
| Địa bàn | `GET/PUT /v1/companies/{id}/service-areas` |
| KPI | `GET /v1/companies/{id}/kpi?from=&to=` hoặc `period=ThisMonth` |
| Lịch sử HĐ | `GET /v1/companies/{id}/contract-history` |
| Hành động | suspend / terminate / reactivate / renew / delete |

| Hành động | API |
|-----------|-----|
| Tạm ngưng | `POST /v1/companies/{id}/suspend` `{ reason }` |
| Chấm dứt | `POST /v1/companies/{id}/terminate` `{ reason }` |
| Kích hoạt lại | `POST /v1/companies/{id}/reactivate` |
| Gia hạn HĐ | `POST /v1/companies/{id}/renew-contract` |
| Tạo CM | `POST /v1/companies/{id}/manager` |
| Reset MK CM | `POST /v1/companies/{id}/manager/{userId}/reset-password` |
| Xóa (soft) | `DELETE /v1/companies/{id}` (chỉ khi Terminated) |

---

## 5. Màn hình Văn phòng MT — `/deo/offices`

| API | Mô tả |
|-----|--------|
| `GET /v1/departments/my-offices` | Offices thuộc sở DEO (search, filter onboard) |
| `GET /v1/offices` | List offices (Admin/DEO/LEO) |
| `GET /v1/offices/{id}` | Chi tiết + teams + LEO phụ trách |

**UI:** Bảng phường/xã, LEO assigned, số team, trạng thái onboard. Click office → filter báo cáo `assignedOfficeId`.

---

## 6. Màn hình Hiệu suất LEO — `/deo/officers`

| API | Khi nào dùng |
|-----|--------------|
| `GET /v1/dashboard/deo/officer-performance` | Bảng xếp hạng trên dashboard (xem §2.4) |
| `GET /v1/reports/officer-kpi?officerId={uuid}` | Drill-down 1 LEO (preset `period` hoặc `from`/`to`) |

**UI drill-down:** Click tên LEO → drawer KPI chi tiết (verified count, avg hours, SLA rate).

**Khác với bảng công ty:** LEO chỉ xuất hiện khi đã verify ≥ 1 báo cáo trong range — không có dòng “placeholder” score 0 như `company-performance`.

---

## 7. Hồ sơ xử phạt (Inspection) — optional tab

| API | Mô tả |
|-----|--------|
| `GET /v1/inspections/officer-queue` | Hàng đợi inspection trong phạm vi sở |

DEO **xem** queue, không approve/reject (LEO/Admin).

---

## 8. Auth & session

| API | |
|-----|---|
| `POST /v1/auth/login` | Đăng nhập |
| `POST /v1/auth/refresh` | Refresh token |
| `POST /v1/auth/logout` | Đăng xuất |
| `GET /v1/auth/me` | Profile + role |

---

## 9. Catalog API đầy đủ cho DEO

### 9.1 Dashboard analytics (mới)

| # | Method | Path | Ghi chú |
|---|--------|------|---------|
| 1 | GET | `/v1/dashboard/deo/overview` | KPI tổng |
| 2 | GET | `/v1/dashboard/deo/alerts` | Cảnh báo |
| 3 | GET | `/v1/dashboard/deo/report-status` | Donut status |
| 4 | GET | `/v1/dashboard/deo/report-trend` | Line chart |
| 5 | GET | `/v1/dashboard/deo/pollution-analytics` | Bar category |
| 6 | GET | `/v1/dashboard/deo/geographic` | Heatmap + markers |
| 7 | GET | `/v1/dashboard/deo/report-funnel` | Funnel |
| 8 | GET | `/v1/dashboard/deo/company-performance` | Bảng công ty (luôn list công ty Sở, §2.4) |
| 9 | GET | `/v1/dashboard/deo/officer-performance` | Bảng LEO verify KPI (§2.4) |
| 10 | GET | `/v1/dashboard/deo/queue-aging` | Tuổi pending |
| 11 | GET | `/v1/dashboard/deo/resolution-distribution` | Histogram XL |
| 12 | GET | `/v1/dashboard/deo/recent-activities` | Timeline status history (§2.4) |

### 9.2 Báo cáo (read + export)

| # | Method | Path | Ghi chú |
|---|--------|------|---------|
| 13 | GET | `/v1/departments/my/reports` | Master table |
| 14 | GET | `/v1/reports/{id}` | Chi tiết |
| 15 | GET | `/v1/reports/{id}/history` | Timeline |
| 16 | GET | `/v1/reports/{id}/progress` | Tiến độ + GPS |
| 17 | GET | `/v1/reports/export` | CSV/XLSX |
| 18 | GET | `/v1/reports/officer-kpi` | KPI LEO (`officerId` bắt buộc với DEO) |
| 19 | GET | `/v1/reports/duplicate-candidates` | Read-only |
| 20 | GET | `/v1/reports/{id}/duplicate-candidate-detail` | Read-only |
| 21 | GET | `/v2/reports/duplicate-groups` | Read-only |
| 22 | GET | `/v1/reports/violation-recurrence-candidates` | Read-only |
| 23 | GET | `/v1/reports/{id}/violation-recurrence-comparison` | Read-only |
| 24 | GET | `/v1/reports/{id}/inspections` | Inspection liên quan |

### 9.3 Công ty DVMT (read + write)

| # | Method | Path |
|---|--------|------|
| 25 | POST | `/v1/companies` |
| 26 | GET | `/v1/companies` |
| 27 | GET | `/v1/companies/{id}` |
| 28 | POST | `/v1/companies/{id}/manager` |
| 29 | POST | `/v1/companies/{id}/manager/{userId}/reset-password` |
| 30 | POST | `/v1/companies/{id}/suspend` |
| 31 | POST | `/v1/companies/{id}/terminate` |
| 32 | POST | `/v1/companies/{id}/reactivate` |
| 33 | DELETE | `/v1/companies/{id}` |
| 34 | GET/PUT | `/v1/companies/{id}/service-areas` |
| 35 | POST | `/v1/companies/{id}/renew-contract` |
| 36 | GET | `/v1/companies/{id}/contract-history` |
| 37 | GET | `/v1/companies/{id}/kpi` |

### 9.4 Tổ chức & cấu trúc

| # | Method | Path |
|---|--------|------|
| 38 | GET | `/v1/departments` |
| 39 | GET | `/v1/departments/{id}` |
| 40 | GET | `/v1/departments/my-offices` |
| 41 | GET | `/v1/offices` |
| 42 | GET | `/v1/offices/{id}` |
| 43 | GET | `/v1/teams` (community teams trong phạm vi sở) |
| 44 | GET | `/v1/teams/{id}` |

### 9.5 Inspection

| # | Method | Path |
|---|--------|------|
| 45 | GET | `/v1/inspections/officer-queue` |

### 9.6 API DEO **không còn** dùng (403)

| Method | Path | Lý do |
|--------|------|-------|
| GET | `/v1/reports/queue` | Hàng đợi xử lý LEO |
| POST | `/v1/reports/{id}/confirm-duplicate` | LEO action |
| POST | `/v1/reports/{id}/dismiss-duplicate` | LEO action |
| POST | `/v1/reports/{id}/dismiss-violation-recurrence` | LEO action |
| POST | `/v1/comments/{id}/hide` | Moderation LEO |
| POST | `/v1/reports/{id}/verify` | LEO workflow |
| POST | `/v1/reports/{id}/reject` | LEO workflow |
| POST | `/v1/reports/{id}/assign-*` | Dispatch |

---

## 10. Chiến lược FE kỹ thuật

1. **Lazy load widgets:** Gọi `overview` + `alerts` trước; chart/map load khi scroll vào viewport.
2. **Shared date filter:** Context `DeoDateRange` sync `from`/`to` cho mọi widget dashboard.
3. **Empty department:** Nếu bất kỳ widget nào trả `404 DEPARTMENT_NOT_FOUND` → hiển thị “Chưa được gán Sở TNMT, liên hệ Admin”.
4. **Báo cáo không có `AssignedDepartmentId`:** Không xuất hiện trong analytics DEO (edge case Admin cần assign office).
5. **Widget công ty vs LEO:** `company-performance` luôn trả đủ công ty Sở (KPI 0 nếu chưa dispatch); `officer-performance` chỉ LEO có verify trong range.
6. **Timeline:** `recent-activities` paginate bằng `page`/`pageSize`; không có `totalCount` — FE “load more” tăng `page` khi `data.length === pageSize`.
7. **Swagger:** Tag `🔍 DEO Dashboard` trên Swagger UI — schema OpenAPI bổ sung.

---

## 11. Checklist triển khai FE

- [ ] Route guard role `DEO`
- [ ] Dashboard 12 widget + date filter
- [ ] Reports table + export (no action buttons)
- [ ] Report detail read-only
- [ ] Companies CRUD + contract flows
- [ ] Offices list + link filter reports
- [ ] Officer KPI drill-down
- [ ] Ẩn/disable mọi endpoint 403 ở mục 9.6
- [ ] Alert deep links

---

**Cập nhật:** 2026-08-13 · Backend: `DeoDashboardController`, `DepartmentContextResolver`, `GetDeoCompanyPerformanceQueryHandler`, `GetDeoRecentActivitiesQueryHandler`, `LegacyReportStatusValueConverter`
