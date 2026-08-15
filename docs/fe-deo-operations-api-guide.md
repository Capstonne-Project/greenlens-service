# DEO Portal — Operations API Guide (Web FE)

> **Đối tượng:** Frontend DEO Portal (vận hành ngoài widget giám sát)  
> **Prefix:** `/v1` · **Envelope:** `{ code, message, status, data }`  
> **Role:** `DEO` (Điều phối viên cấp Tỉnh/TP — Sở TNMT)  
> **Swagger tag:** `🔍 DEO Dashboard` (và các API liên quan DEO gọi từ portal)

**Tài liệu liên quan:**

| Tài liệu | Nội dung |
|----------|----------|
| [fe-deo-dashboard-api-guide.md](./fe-deo-dashboard-api-guide.md) | 12 widget giám sát `/v1/dashboard/deo/*`, wireframe dashboard, KPI charts |
| [00_API_CONVENTIONS.md](../00_API_CONVENTIONS.md) | Envelope, pagination, rate limit, error format |

**Phạm vi tài liệu này:** API **vận hành** — báo cáo (read-only), văn phòng MT, công ty DVMT (CRUD), export, duplicate/tái phạm (xem), KPI LEO drill-down. **Không** lặp lại 12 endpoint analytics dashboard.

---

## 0. Nguyên tắc scope & quyền

### 0.1 Cơ chế scope (BE tự resolve — FE không gửi `departmentId`)

| Module | Cách BE xác định phạm vi |
|--------|---------------------------|
| Báo cáo list | `users.department_id` từ JWT → lọc `reports.assigned_department_id` |
| Báo cáo chi tiết | `ValidateReportAccess`: DEO chỉ xem báo cáo cùng `assignedDepartmentId` |
| Duplicate / tái phạm | Scope department; duplicate còn check primary cùng department |
| Công ty | `environmental_service_companies.department_id` = department DEO |
| Văn phòng | `local_offices.department_id` = department DEO |
| Export | Toàn tỉnh (không PII citizen) |

### 0.2 DEO được / không được (2026-08)

| DEO **được** | DEO **không được** (403) |
|---|---|
| Xem list + chi tiết báo cáo toàn tỉnh | `GET /v1/reports/queue` (hàng đợi LEO) |
| Export CSV/Excel (ẩn PII) | Verify, reject, assign, dispatch |
| Xem cờ duplicate / tái phạm + so sánh | `POST confirm/dismiss duplicate`, `POST dismiss-violation-recurrence` |
| CRUD công ty DVMT + hợp đồng | `GET /v1/reports/{id}/progress` (LEO-only ở controller) |
| Quản lý CM (tạo / reset MK) | `GET /v1/reports/{id}/inspections` (LEO/Inspector/Admin) |
| Xem văn phòng MT + LEO phụ trách | Moderation comment |

**UI:** Mọi nút hành động workflow trên báo cáo → **ẩn** hoặc hiển thị “LEO xử lý tại văn phòng MT”.

### 0.3 Headers chuẩn

```
Authorization: Bearer {accessToken}
Accept-Language: vi-VN
Content-Type: application/json   // POST/PUT
```

---

## 1. Navigation → API map

```
DEO Portal (operations)
├── 📋 Báo cáo           → §2  GET /v1/departments/my/reports + §3
├── 🏢 Công ty DVMT      → §5  /v1/companies/*
├── 🏛 Văn phòng MT      → §4  /v1/departments/my-offices + /v1/offices/*
├── 👮 Hiệu suất LEO     → §6  officer-kpi drill-down
└── 📥 Export            → §2.3 GET /v1/reports/export
```

---

## 2. Module Báo cáo (read-only)

### 2.1 Danh sách báo cáo toàn tỉnh

**`GET /v1/departments/my/reports`** · Tag: `🔍 DEO Dashboard` · Auth: `DEO`

Master table cho màn `/deo/reports`.

#### Query parameters

| Param | Type | Default | Mô tả |
|-------|------|---------|-------|
| `page` | int | `1` | Trang (≥ 1) |
| `pageSize` | int | `20` | 1–100 |
| `search` | string | — | Mã báo cáo, mô tả, địa chỉ, tên/mã category |
| `status` | enum | — | `Submitted`, `Verified`, `InProgress`, `Resolved`, `Closed`, `Rejected`, `Duplicate`, `Reopened` |
| `categoryId` | uuid | — | Lọc loại ô nhiễm |
| `severity` | enum | — | `Low`, `Medium`, `High`, `Critical` |
| `wardCode` | string | — | Mã phường 5 chữ số |
| `assignedOfficeId` | uuid | — | Văn phòng MT xử lý |
| `fromDate` | datetime | — | UTC, lọc `createdAt >= fromDate` |
| `toDate` | datetime | — | UTC, lọc `createdAt <= toDate` |
| `slaBreached` | bool | — | `true` = có breach verify **hoặc** resolve |
| `isPossibleDuplicate` | bool | — | Cờ nghi trùng (BR-REP-030/031) |
| `isSuspectedViolationRecurrence` | bool | — | Cờ nghi tái phạm (BR-REP-034) |
| `hasPendingReopenRequest` | bool | — | Citizen đang chờ LEO duyệt reopen |
| `sortBy` | string | `createdAt` | `code`, `status`, `severity`, `priority`, `createdAt`, `verifiedAt`, `slaVerifyDueAt`, `slaResolveDueAt` |
| `sortDesc` | bool | `true` | `true` = giảm dần |

#### Response 200

```json
{
  "code": "SUCCESS",
  "message": "Operation completed",
  "status": 200,
  "data": {
    "departmentId": "550e8400-e29b-41d4-a716-446655440000",
    "departmentName": "Sở TNMT TP.HCM",
    "items": [
      {
        "id": "uuid",
        "code": "RPT-260814-A1B2C3",
        "categoryCode": "TRASH",
        "categoryName": "Rác thải sinh hoạt",
        "severity": "Medium",
        "status": "Verified",
        "latitude": 10.7626,
        "longitude": 106.6602,
        "address": "123 Hoàng Hữu Nam, P. Long Bình",
        "wardCode": "26808",
        "wardName": "Phường Long Bình",
        "reporterId": "uuid",
        "reporterName": "Nguyễn Văn A",
        "assignedOfficeId": "uuid",
        "assignedOfficeName": "VP MT Phường Long Bình",
        "assignmentCount": 1,
        "priorityScore": 12.5,
        "reporterCount": 1,
        "reopenedCount": 0,
        "createdAt": "2026-08-14T03:00:00Z",
        "verifiedAt": "2026-08-14T05:00:00Z",
        "startedAt": null,
        "resolvedAt": null,
        "closedAt": null,
        "slaVerifyDueAt": "2026-08-15T03:00:00Z",
        "slaResolveDueAt": "2026-08-21T05:00:00Z",
        "firstImageUrl": "https://cdn.example.com/reports/abc.jpg",
        "isPossibleDuplicate": false,
        "possibleDuplicateOfReportId": null,
        "possibleDuplicateOfReportCode": null,
        "isSuspectedViolationRecurrence": false,
        "suspectedRecurrenceOfReportId": null,
        "suspectedRecurrenceOfReportCode": null
      }
    ],
    "pagination": {
      "page": 1,
      "pageSize": 20,
      "totalItems": 1200,
      "totalPages": 60,
      "hasNext": true,
      "hasPrev": false
    }
  }
}
```

#### FE gợi ý

- Cột **Cờ:** badge `Dup` / `Tái phạm` / `Reopen` từ 3 boolean flags.
- Row action: chỉ **Xem chi tiết** → `/deo/reports/:id`.
- Deep link từ dashboard alerts: `?isPossibleDuplicate=true`, `?slaBreached=true`, …

#### Lỗi

| HTTP | `code` | Khi nào |
|------|--------|---------|
| 401 | `UNAUTHORIZED` | Token invalid |
| 403 | `FORBIDDEN` | Không phải DEO |
| 404 | `DEPARTMENT_NOT_FOUND` | User DEO chưa gán `department_id` |

---

### 2.2 Chi tiết báo cáo

**`GET /v1/reports/{id}`** · Auth: bất kỳ user đăng nhập · DEO: scope department

#### Response 200 (trích yếu — full schema xem Swagger)

```json
{
  "code": "SUCCESS",
  "status": 200,
  "data": {
    "id": "uuid",
    "code": "RPT-260814-A1B2C3",
    "reporterId": "uuid",
    "reporterName": "Nguyễn Văn A",
    "categoryCode": "TRASH",
    "categoryName": "Rác thải sinh hoạt",
    "severity": "Medium",
    "status": "Verified",
    "description": "Đống rác lớn",
    "latitude": 10.7626,
    "longitude": 106.6602,
    "address": "123 Hoàng Hữu Nam",
    "wardCode": "26808",
    "provinceCode": "79",
    "media": [{ "id": "uuid", "mediaType": "Image", "url": "...", "mimeType": "image/jpeg", "sizeBytes": 102400 }],
    "assignments": [],
    "currentAssignment": null,
    "assignmentHistory": [],
    "isSuspectedViolationRecurrence": false,
    "suspectedRecurrenceOfReportId": null,
    "priorClosedReport": null,
    "isSuspicious": false,
    "suspiciousReasons": null,
    "hasPendingReopenRequest": false
  }
}
```

**DEO-only fields:** `isSuspicious`, `suspiciousReasons` (cảnh báo EXIF — BR-REP-011, chỉ tham khảo).

#### Lỗi scope

| HTTP | `code` | Khi nào |
|------|--------|---------|
| 403 | `OUTSIDE_JURISDICTION` | Báo cáo không thuộc department DEO |
| 404 | `NOT_FOUND` | Không tồn tại / soft-deleted |

---

### 2.3 Lịch sử trạng thái

**`GET /v1/reports/{id}/history`** · Auth: đăng nhập · DEO: scope department

#### Response 200

```json
{
  "code": "SUCCESS",
  "status": 200,
  "data": {
    "items": [
      {
        "id": "uuid",
        "fromStatus": "Submitted",
        "toStatus": "Verified",
        "changedBy": "uuid",
        "changedByName": "Trần Văn LEO",
        "reason": null,
        "createdAt": "2026-08-14T05:00:00Z",
        "metadata": null
      }
    ]
  }
}
```

**`metadata`:** JSON string cho event không đổi status (vd. `{"eventType":"ReopenRequested"}`). FE parse để render timeline đúng nhánh.

---

### 2.4 Export

**`GET /v1/reports/export`** · Auth: `LEO,DEO,Admin` · DEO = toàn tỉnh, **không** cột PII

#### Query parameters

| Param | Type | Mô tả |
|-------|------|-------|
| `format` | enum | **`Csv`** hoặc **`Excel`** (bắt buộc) |
| `status` | enum | Lọc trạng thái |
| `severity` | enum | Lọc mức độ |
| `categoryId` | uuid | Lọc category |
| `wardCode` | string | Lọc phường |
| `from` | datetime | UTC |
| `to` | datetime | UTC |
| `isPossibleDuplicate` | bool | Lọc cờ trùng |
| `isSuspectedViolationRecurrence` | bool | Lọc cờ tái phạm |

#### Response 200

- **Không** dùng envelope JSON — trả file binary trực tiếp.
- Headers: `Content-Type: text/csv` hoặc Excel MIME, `Content-Disposition: attachment; filename="reports-....csv"`.
- FE: `window.open` hoặc `fetch` + `blob` download.

File gồm cột duplicate + violation recurrence (BR-OFF-022).

---

### 2.5 Duplicate — chỉ xem (BR-REP-031)

> Duplicate detection yêu cầu **cùng ward + province + category + ≤25m** (BR-REP-030). Hai báo cáo hai phường khác nhau sẽ **không** bị gắn cờ.

#### 2.5.1 Danh sách nghi trùng

**`GET /v1/reports/duplicate-candidates`** · Auth: `LEO,DEO,Admin`

| Param | Mô tả |
|-------|--------|
| `page`, `pageSize` | Phân trang |
| `status`, `severity`, `categoryId`, `wardCode` | Lọc |
| `fromDate`, `toDate` | Khoảng `createdAt` |
| `search` | Code, address, category |
| `duplicateDetectionSource` | `geo_category` (Tier 1) hoặc `geo_category_ai` (Tier 2) |
| `minAiSimilarityScore` | 0.0–1.0, chỉ Tier 2 |
| `sortBy` | `CreatedAt`, `Severity`, `AiSimilarityScore`, `PriorityScore` |
| `sortDir` | `Asc` / `Desc` |

**Response item:**

```json
{
  "id": "uuid",
  "code": "RPT-NEW",
  "categoryName": "Rác thải",
  "severity": "Medium",
  "status": "Submitted",
  "latitude": 10.7627,
  "longitude": 106.6603,
  "address": "...",
  "createdAt": "2026-08-14T10:00:00Z",
  "duplicateDetectionSource": "geo_category",
  "aiSimilarityScore": null,
  "media": [{ "id": "uuid", "url": "...", "thumbnailUrl": "...", "mimeType": "image/jpeg" }],
  "primary": {
    "id": "uuid-primary",
    "code": "RPT-PRIMARY",
    "address": "...",
    "createdAt": "2026-08-13T08:00:00Z",
    "media": []
  }
}
```

#### 2.5.2 So sánh side-by-side

**`GET /v1/reports/{id}/duplicate-candidate-detail`**

```json
{
  "data": {
    "report": { "id": "...", "code": "...", "status": "Submitted", "media": [] },
    "primaryReport": { "id": "...", "code": "...", "status": "Verified", "media": [] },
    "duplicateDetectionSource": "geo_category_ai",
    "aiSimilarityScore": 0.87,
    "distanceMeters": 12.4,
    "hoursSincePrimaryCreated": 26.5
  }
}
```

#### 2.5.3 Nhóm theo báo cáo gốc (optional UI)

**`GET /v2/reports/duplicate-groups`** · Auth: `LEO,DEO,Admin`

Cùng filter như 2.5.1 + `primaryReportId` (lọc 1 nhóm).

**Response group:**

```json
{
  "items": [
    {
      "primary": { "id": "...", "code": "...", "address": "...", "createdAt": "...", "media": [] },
      "duplicates": [{ "id": "...", "code": "...", "aiSimilarityScore": 0.9, "media": [] }],
      "duplicateCount": 3
    }
  ],
  "pagination": { "page": 1, "pageSize": 20, "totalItems": 5, "totalPages": 1, "hasNext": false, "hasPrev": false }
}
```

#### ⚠️ DEO không gọi

| Method | Path | Lý do |
|--------|------|-------|
| POST | `/v1/reports/{id}/confirm-duplicate` | LEO action |
| POST | `/v1/reports/{id}/dismiss-duplicate` | LEO action |

UI: hiển thị banner “Chờ LEO tại {assignedOfficeName} xử lý”.

---

### 2.6 Tái phạm vi phạm — chỉ xem (BR-REP-034)

Cùng điều kiện ward + province + category + ≤25m + case Closed trong 30 ngày.

#### 2.6.1 Danh sách

**`GET /v1/reports/violation-recurrence-candidates`** · Auth: `LEO,DEO,Admin`

| Param | Mô tả |
|-------|--------|
| `page`, `pageSize` | Phân trang |
| `status`, `severity`, `categoryId`, `wardCode`, `fromDate`, `toDate`, `search` | Lọc |
| `minDaysSincePriorClosed`, `maxDaysSincePriorClosed` | Lọc khoảng ngày từ case Closed trước |
| `sortBy` | `CreatedAt`, `Severity`, `PriorClosedAt`, `PriorityScore` |
| `sortDir` | `Asc` / `Desc` |

**Response item:**

```json
{
  "id": "uuid",
  "code": "RPT-NEW",
  "categoryName": "Rác thải",
  "severity": "High",
  "status": "Submitted",
  "address": "...",
  "createdAt": "2026-08-14T10:00:00Z",
  "media": [],
  "priorClosedReport": {
    "id": "uuid-closed",
    "code": "RPT-OLD",
    "address": "...",
    "status": "Closed",
    "closedAt": "2026-07-20T08:00:00Z",
    "daysSinceClosed": 25,
    "media": []
  }
}
```

#### 2.6.2 So sánh chi tiết

**`GET /v1/reports/{id}/violation-recurrence-comparison`**

```json
{
  "data": {
    "currentReport": {
      "id": "...", "code": "...", "status": "Submitted",
      "hadPriorInspection": true, "priorInspectionId": "uuid", "hasInspection": false
    },
    "priorClosedReport": {
      "id": "...", "code": "...", "status": "Closed", "closedAt": "2026-07-20T08:00:00Z",
      "hadPriorInspection": true, "priorInspectionFinalStatus": "PenaltyIssued"
    },
    "daysSincePriorClosed": 25,
    "distanceMeters": 8.2
  }
}
```

#### ⚠️ DEO không gọi

| POST | `/v1/reports/{id}/dismiss-violation-recurrence` |

---

### 2.7 API báo cáo DEO **không** dùng

| Method | Path | Lý do |
|--------|------|-------|
| GET | `/v1/reports/queue` | Hàng đợi xử lý LEO |
| GET | `/v1/reports/{id}/progress` | Controller `LEO,Admin` only |
| GET | `/v1/reports/{id}/inspections` | `LEO,Inspector,Admin` |
| POST | `/v1/reports/{id}/verify`, `/reject`, `/assign-*`, … | Workflow LEO |

**Thay thế tiến độ:** DEO xem timeline qua `history` + trạng thái/assignment trong `GET /v1/reports/{id}`.

---

## 3. Module Văn phòng MT

### 3.1 Danh sách văn phòng thuộc Sở

**`GET /v1/departments/my-offices`** · Tag: `🔍 DEO Dashboard` · Auth: `DEO`

Dùng cho màn `/deo/offices` và dropdown filter báo cáo.

#### Query parameters

| Param | Default | Mô tả |
|-------|---------|-------|
| `page` | `1` | Phân trang |
| `pageSize` | `20` | 1–100 |
| `search` | — | Tên office, tên phường, tên LEO |
| `isOnboarded` | — | `true` / `false` |
| `sortBy` | `createdAt` | `name`, `wardName`, `officerName`, `teamCount`, `createdAt` |
| `sortDesc` | `false` | |

#### Response 200

```json
{
  "data": {
    "departmentId": "uuid",
    "departmentName": "Sở TNMT TP.HCM",
    "provinceCode": "79",
    "offices": [
      {
        "id": "uuid",
        "name": "VP MT Phường Long Bình",
        "wardCode": "26808",
        "wardName": "Phường Long Bình",
        "officerId": "uuid",
        "officerName": "Trần Văn LEO",
        "isOnboarded": true,
        "teamCount": 3,
        "createdAt": "2026-01-15T00:00:00Z"
      }
    ],
    "pagination": { "page": 1, "pageSize": 20, "totalItems": 24, "totalPages": 2, "hasNext": true, "hasPrev": false }
  }
}
```

**FE:** Click office → `/deo/reports?assignedOfficeId={id}`.

---

### 3.2 Danh sách / chi tiết office (bổ sung)

| Method | Path | Auth | Ghi chú |
|--------|------|------|---------|
| GET | `/v1/offices` | Admin, DEO, LEO | DEO tự lọc theo department; query `departmentId` optional |
| GET | `/v1/offices/{id}` | Admin, DEO, LEO | Chi tiết + teams + LEO |

**`GET /v1/offices` query:** `page`, `pageSize`, `departmentId`, `isOnboarded`.

DEO thường ưu tiên `my-offices` (có search/sort phong phú hơn). Dùng `/v1/offices/{id}` khi cần chi tiết team list.

---

## 4. Module Công ty DVMT (read + write)

Tất cả endpoint dưới đây tag **`🔍 DEO Dashboard`**, auth **`DEO,Admin`** (Admin xem toàn hệ thống).

### 4.1 State machine công ty (BR-CMP-004)

```
PendingActivation ──(CM đổi MK lần đầu)──► Active
Active ──suspend──► Suspended ──reactivate──► Active
Active/Suspended ──terminate──► Terminated
Active ──(hết hạn HĐ Bidding)──► Expired ──renew-contract──► Active
Terminated ──(soft delete)──► Deleted
```

| Status | Ý nghĩa UI |
|--------|------------|
| `PendingActivation` | Chờ CM kích hoạt |
| `Active` | Đang nhận dispatch |
| `Suspended` | Tạm ngưng — task đang giao bị hủy |
| `Expired` | Hết hạn tự nhiên (Bidding) |
| `Terminated` | Chấm dứt sớm — có thể soft-delete |

---

### 4.2 Tạo công ty

**`POST /v1/companies`**

#### Request body

```json
{
  "name": "CITENCO TP.HCM",
  "departmentId": "550e8400-e29b-41d4-a716-446655440000",
  "contractNumber": "HD-2026-001",
  "contractStartDate": "2026-01-01T00:00:00Z",
  "contractEndDate": "2027-01-01T00:00:00Z",
  "contractType": "Bidding",
  "taxCode": "0123456789",
  "address": "123 Nguyễn Văn Cừ, Q.5",
  "phone": "0281234567",
  "email": "contact@citenco.vn",
  "managerEmail": "cm@citenco.vn",
  "managerFullName": "Nguyễn Văn A",
  "wardCodes": ["26808", "26809"]
}
```

| Field | Required | Validation |
|-------|----------|------------|
| `name` | ✅ | ≤ 300 ký tự |
| `departmentId` | ✅ | UUID Sở TNMT (DEO lấy từ `GET /v1/auth/me` hoặc overview) |
| `contractNumber` | ✅ | ≤ 50, unique |
| `contractStartDate` | ✅ | ISO 8601 UTC |
| `contractType` | ✅ | `Subsidiary` \| `Bidding` |
| `contractEndDate` | Bidding: ✅ | Phải > startDate. `Subsidiary`: bỏ qua (vô thời hạn) |
| `managerEmail` + `managerFullName` | Optional | Phải đi cặp; bỏ trống → tạo CM sau |
| `wardCodes` | Optional | Mảng mã phường hợp lệ |

#### Response 200

```json
{
  "code": "SUCCESS",
  "message": "Đã tạo công ty thành công.",
  "status": 200,
  "data": {
    "companyId": "uuid",
    "companyName": "CITENCO TP.HCM",
    "contractNumber": "HD-2026-001",
    "contractType": "Bidding",
    "status": "PendingActivation",
    "managerUserId": "uuid",
    "managerEmail": "cm@citenco.vn",
    "tempPassword": "Xk9#mP2$vLq8"
  }
}
```

⚠️ **`tempPassword` chỉ trả 1 lần** — hiển thị modal copy + email tự gửi (BR-NTF-002).

#### Lỗi

| HTTP | `code` | Khi nào |
|------|--------|---------|
| 404 | `NOT_FOUND` | `departmentId` hoặc `wardCode` không tồn tại |
| 409 | `CONFLICT` | Trùng `contractNumber` hoặc email CM |
| 422 | `VALIDATION_ERROR` | Field invalid |

---

### 4.3 Danh sách công ty

**`GET /v1/companies`**

| Query | Default | Mô tả |
|-------|---------|-------|
| `page` | `1` | |
| `pageSize` | `20` | 1–100 |
| `status` | — | `PendingActivation`, `Active`, `Suspended`, `Expired`, `Terminated` |
| `search` | — | Tên, mã HĐ, MST |
| `sortBy` | — | `name`, `status`, `contractNumber` |
| `sortDesc` | `false` | |

**Response item:**

```json
{
  "id": "uuid",
  "name": "CITENCO TP.HCM",
  "contractNumber": "HD-2026-001",
  "contractType": "Bidding",
  "status": "Active",
  "contractStartDate": "2026-01-01T00:00:00Z",
  "contractEndDate": "2027-01-01T00:00:00Z",
  "taxCode": "0123456789",
  "phone": "0281234567",
  "email": "contact@citenco.vn",
  "serviceAreaCount": 12,
  "staffCount": 45,
  "createdAt": "2026-01-01T00:00:00Z"
}
```

---

### 4.4 Chi tiết công ty

**`GET /v1/companies/{id}`**

```json
{
  "data": {
    "id": "uuid",
    "name": "CITENCO TP.HCM",
    "contractNumber": "HD-2026-001",
    "contractType": "Bidding",
    "status": "Active",
    "contractStartDate": "2026-01-01T00:00:00Z",
    "contractEndDate": "2027-01-01T00:00:00Z",
    "taxCode": "0123456789",
    "address": "...",
    "phone": "...",
    "email": "...",
    "departmentId": "uuid",
    "departmentName": "Sở TNMT TP.HCM",
    "activatedAt": "2026-01-05T10:00:00Z",
    "serviceAreas": [
      { "id": "uuid", "wardCode": "26808", "wardName": "Phường Long Bình", "provinceCode": "79" }
    ],
    "staffCount": 45,
    "createdAt": "2026-01-01T00:00:00Z"
  }
}
```

---

### 4.5 Quản lý Company Manager

#### Tạo CM

**`POST /v1/companies/{id}/manager`**

```json
{ "managerEmail": "cm2@citenco.vn", "managerFullName": "Trần Thị B" }
```

Response: `{ managerUserId, managerEmail, managerFullName, tempPassword }`.

#### Reset MK CM

**`POST /v1/companies/{id}/manager/{userId}/reset-password`**

Response: `{ tempPassword }` — 1 lần duy nhất.

---

### 4.6 Trạng thái công ty

| Action | Method | Path | Body |
|--------|--------|------|------|
| Tạm ngưng | POST | `/v1/companies/{id}/suspend` | `{ "reason": "Vi phạm hợp đồng ≥ 20 ký tự" }` |
| Chấm dứt | POST | `/v1/companies/{id}/terminate` | `{ "reason": "..." }` |
| Kích hoạt lại | POST | `/v1/companies/{id}/reactivate` | — |
| Soft delete | DELETE | `/v1/companies/{id}` | Chỉ khi `Terminated` |

**Cascading (suspend/terminate):** Hủy task đang giao, báo cáo về `Verified`. FE confirm dialog mô tả impact.

Response suspend/terminate/reactivate/delete: `204` hoặc envelope `status: 200`, `data: null`.

---

### 4.7 Địa bàn phụ trách (service areas)

#### Xem

**`GET /v1/companies/{id}/service-areas`**

```json
{
  "data": {
    "companyId": "uuid",
    "companyName": "CITENCO TP.HCM",
    "serviceAreas": [
      {
        "id": "uuid",
        "wardCode": "26808",
        "wardName": "Phường Long Bình",
        "provinceCode": "79",
        "provinceName": "Thành phố Hồ Chí Minh",
        "createdAt": "2026-01-01T00:00:00Z"
      }
    ]
  }
}
```

#### Cập nhật (replace toàn bộ)

**`PUT /v1/companies/{id}/service-areas`**

```json
{ "wardCodes": ["26808", "26809", "26810"] }
```

- Gửi `[]` → xóa hết địa bàn.
- Response: `204 No Content`.

---

### 4.8 Hợp đồng — gia hạn & lịch sử

#### Gia hạn

**`POST /v1/companies/{id}/renew-contract`**

```json
{
  "newStartDate": "2027-01-01T00:00:00Z",
  "newEndDate": "2028-01-01T00:00:00Z",
  "newContractNumber": "HD-2027-001",
  "note": "Gia hạn năm thứ 2"
}
```

Response: `{ contractPeriodId, companyStatus }` — nếu công ty `Expired` → auto `Active`.

**Subsidiary** (vô thời hạn): endpoint trả 422 — không gia hạn được.

#### Lịch sử kỳ HĐ

**`GET /v1/companies/{id}/contract-history`**

```json
{
  "data": {
    "companyId": "uuid",
    "companyName": "CITENCO TP.HCM",
    "periods": [
      {
        "id": "uuid",
        "contractNumber": "HD-2027-001",
        "contractType": "Bidding",
        "startDate": "2027-01-01T00:00:00Z",
        "endDate": "2028-01-01T00:00:00Z",
        "renewedByUserId": "uuid",
        "renewedByName": "DEO Nguyễn Văn X",
        "note": "Gia hạn năm 2",
        "createdAt": "2026-12-15T00:00:00Z"
      }
    ]
  }
}
```

---

### 4.9 KPI công ty (chi tiết)

**`GET /v1/companies/{id}/kpi`**

| Query | Mô tả |
|-------|--------|
| `from`, `to` | UTC custom range |
| `period` | Preset: `ThisMonth`, `ThisQuarter`, `ThisYear`, `LastMonth`, `LastQuarter`, `LastYear` |

Ưu tiên: nếu có `period` → BE tính range; else `from`/`to`; else default 30 ngày.

```json
{
  "data": {
    "companyId": "uuid",
    "companyName": "CITENCO TP.HCM",
    "periodFrom": "2026-08-01T00:00:00Z",
    "periodTo": "2026-08-14T23:59:59Z",
    "totalAssigned": 12,
    "totalCompleted": 10,
    "totalDeclined": 1,
    "completedOnTime": 9,
    "slaComplianceRate": 90.0,
    "avgResolutionHours": 28.5
  }
}
```

**Khác widget dashboard:** `/v1/dashboard/deo/company-performance` trả **bảng tất cả công ty**; endpoint này drill-down **1 công ty**.

---

## 5. Module Hiệu suất LEO (drill-down)

### 5.1 Bảng xếp hạng (dashboard widget)

Xem [fe-deo-dashboard-api-guide.md §2.4](./fe-deo-dashboard-api-guide.md) — `GET /v1/dashboard/deo/officer-performance`.

### 5.2 KPI chi tiết 1 LEO

**`GET /v1/reports/officer-kpi`** · Auth: `LEO,DEO,Admin`

| Query | DEO | Ghi chú |
|-------|-----|---------|
| `officerId` | **Bắt buộc** | UUID LEO (từ bảng officer-performance) |
| `from`, `to` | Optional | UTC |
| `period` | Optional | Preset (cùng enum KPI công ty) |

LEO tự xem: bỏ `officerId` (BE lấy từ token).

```json
{
  "data": {
    "officerId": "uuid",
    "officerName": "Trần Văn LEO",
    "periodFrom": "2026-08-01T00:00:00Z",
    "periodTo": "2026-08-14T23:59:59Z",
    "totalVerified": 45,
    "verifiedOnTime": 44,
    "verifiedOnTimePercent": 97.8,
    "totalRejected": 2,
    "totalEscalated": 0,
    "totalResolved": 38,
    "totalClosed": 35,
    "resolvedRate": 84.4,
    "avgResponseTimeHours": 4.5
  }
}
```

**FE:** Click tên LEO trên dashboard → drawer/modal gọi API này.

---

## 6. Catalog API — tag `🔍 DEO Dashboard`

### 6.1 Đã cover ở tài liệu dashboard (không lặp chi tiết)

| # | Method | Path |
|---|--------|------|
| 1–12 | GET | `/v1/dashboard/deo/*` |

### 6.2 Operations (tài liệu này)

| # | Method | Path | § |
|---|--------|------|---|
| 13 | GET | `/v1/departments/my/reports` | 2.1 |
| 14 | GET | `/v1/departments/my-offices` | 3.1 |
| 15 | GET | `/v1/reports/{id}` | 2.2 |
| 16 | GET | `/v1/reports/{id}/history` | 2.3 |
| 17 | GET | `/v1/reports/export` | 2.4 |
| 18 | GET | `/v1/reports/duplicate-candidates` | 2.5 |
| 19 | GET | `/v1/reports/{id}/duplicate-candidate-detail` | 2.5 |
| 20 | GET | `/v2/reports/duplicate-groups` | 2.5 |
| 21 | GET | `/v1/reports/violation-recurrence-candidates` | 2.6 |
| 22 | GET | `/v1/reports/{id}/violation-recurrence-comparison` | 2.6 |
| 23 | GET | `/v1/reports/officer-kpi` | 5.2 |
| 24 | GET | `/v1/offices`, `/v1/offices/{id}` | 3.2 |
| 25 | POST | `/v1/companies` | 4.2 |
| 26 | GET | `/v1/companies` | 4.3 |
| 27 | GET | `/v1/companies/{id}` | 4.4 |
| 28 | POST | `/v1/companies/{id}/manager` | 4.5 |
| 29 | POST | `/v1/companies/{id}/manager/{userId}/reset-password` | 4.5 |
| 30 | POST | `/v1/companies/{id}/suspend` | 4.6 |
| 31 | POST | `/v1/companies/{id}/terminate` | 4.6 |
| 32 | POST | `/v1/companies/{id}/reactivate` | 4.6 |
| 33 | DELETE | `/v1/companies/{id}` | 4.6 |
| 34 | GET/PUT | `/v1/companies/{id}/service-areas` | 4.7 |
| 35 | POST | `/v1/companies/{id}/renew-contract` | 4.8 |
| 36 | GET | `/v1/companies/{id}/contract-history` | 4.8 |
| 37 | GET | `/v1/companies/{id}/kpi` | 4.9 |

---

## 7. Enum reference (FE)

### ReportStatus

`Submitted` · `Verified` · `InProgress` · `Resolved` · `Closed` · `Rejected` · `Duplicate` · `Reopened`

### Severity

`Low` · `Medium` · `High` · `Critical`

### CompanyStatus

`PendingActivation` · `Active` · `Suspended` · `Expired` · `Terminated`

### ContractType

`Subsidiary` (trực thuộc, vô thời hạn) · `Bidding` (đấu thầu, có ngày hết hạn)

### DuplicateDetectionSource

`geo_category` (Tier 1: GPS + ward + category) · `geo_category_ai` (Tier 2: + AI image compare)

### ExportFormat

`Csv` · `Excel`

### KpiPeriod

`ThisMonth` · `ThisQuarter` · `ThisYear` · `LastMonth` · `LastQuarter` · `LastYear`

---

## 8. Lỗi thường gặp

| HTTP | `code` | UI xử lý |
|------|--------|----------|
| 401 | `UNAUTHORIZED` | Redirect login |
| 403 | `FORBIDDEN` | Toast “Không có quyền” — ẩn nút action |
| 403 | `OUTSIDE_JURISDICTION` | “Báo cáo không thuộc phạm vi Sở” |
| 404 | `NOT_FOUND` | Empty state |
| 404 | `DEPARTMENT_NOT_FOUND` | Full-page: “Chưa được gán Sở TNMT, liên hệ Admin” |
| 409 | `CONFLICT` | Toast + highlight field (trùng HĐ, email) |
| 422 | `VALIDATION_ERROR` | Inline form errors |
| 422 | Business rule | VD: “Công ty chưa Terminated — phải chấm dứt trước khi xóa” |

---

## 9. Checklist triển khai FE

- [ ] Route guard role `DEO`
- [ ] `/deo/reports` — table + filters + export (§2)
- [ ] `/deo/reports/:id` — read-only detail + history tabs (§2.2–2.3)
- [ ] Tab duplicate / tái phạm — list + comparison, **không** nút confirm/dismiss (§2.5–2.6)
- [ ] `/deo/offices` — `my-offices` + link filter reports (§3)
- [ ] `/deo/companies` — CRUD wizard + contract flows + temp password modal (§4)
- [ ] Officer KPI drawer từ dashboard widget (§5)
- [ ] Xử lý `DEPARTMENT_NOT_FOUND` toàn portal
- [ ] Không gọi API mục §2.7 (403)
- [ ] Swagger tag `🔍 DEO Dashboard` — cross-check khi BE thêm endpoint

---

**Cập nhật:** 2026-08-14 · Backend: `DeoDashboardController`, `DepartmentsController`, `CompaniesController`, `ReportsController`, `ReportReviewCandidateFilters`, duplicate ward+province check (BR-REP-030/034)
