# DEO Dashboard — API & UI Guide (Web FE)

> **Prefix:** `/v1` · **Envelope:** `{ code, message, status, data }`  
> **Role:** `DEO` (Điều phối viên cấp Tỉnh/TP — Sở TNMT)  
> **Phạm vi dữ liệu:** Mọi query báo cáo/analytics lọc theo `AssignedDepartmentId` = department của DEO đăng nhập.

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

**`company-performance`** / **`officer-performance`**: cùng shape Admin dashboard (xem Swagger tag `📊 DEO Dashboard`).

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
| `GET /v1/dashboard/deo/officer-performance` | Bảng xếp hạng trên dashboard |
| `GET /v1/reports/officer-kpi?officerId={uuid}` | Drill-down 1 LEO (preset `period` hoặc `from`/`to`) |

**UI drill-down:** Click tên LEO → drawer KPI chi tiết (verified count, avg hours, SLA rate).

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
| 8 | GET | `/v1/dashboard/deo/company-performance` | Bảng công ty |
| 9 | GET | `/v1/dashboard/deo/officer-performance` | Bảng LEO |
| 10 | GET | `/v1/dashboard/deo/queue-aging` | Tuổi pending |
| 11 | GET | `/v1/dashboard/deo/resolution-distribution` | Histogram XL |
| 12 | GET | `/v1/dashboard/deo/recent-activities` | Timeline |

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
3. **Empty department:** Nếu `overview` trả 404 department → hiển thị “Chưa được gán Sở TNMT, liên hệ Admin”.
4. **Báo cáo không có `AssignedDepartmentId`:** Không xuất hiện trong analytics DEO (edge case Admin cần assign office).
5. **Swagger:** Tag `📊 DEO Dashboard` trên Swagger UI — source of truth cho schema chi tiết.

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

**Cập nhật:** 2026-08-12 · Backend: `DeoDashboardController`, `DepartmentContextResolver`
