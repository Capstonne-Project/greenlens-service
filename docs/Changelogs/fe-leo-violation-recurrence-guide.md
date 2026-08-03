# FE Guide — LEO Violation Recurrence (BR-REP-034)

> **Audience:** LEO Web App (chính) · DEO Web App (giám sát toàn tỉnh + fallback queue)  
> **Backend branch:** `develop`  
> **Related:** Duplicate flag (BR-REP-030) — **loại trừ lẫn nhau**; một báo cáo chỉ có tối đa một trong hai cờ

## Vai trò

| Actor | Phạm vi | Hành động chính |
|-------|---------|-----------------|
| **LEO** | Báo cáo thuộc phường (`AssignedOfficeId`) | Xem queue, so sánh, bác cờ, verify/assign |
| **DEO** | Toàn department (`GET /departments/my/reports`) + fallback queue (chưa dispatch phường) | Giám sát, export, so sánh/bác cờ khi cần |

DEO **không** nhận notification cho mọi case — chỉ khi báo cáo rơi queue cấp sở (không có `AssignedOfficeId`).

## Khi nào cờ bật?

Citizen submit báo cáo mới → BE so sánh với báo cáo **Closed** trong **30 ngày**, **cùng category**, **≤ 25m** → chọn Closed **mới nhất**.

**Không gắn cờ tái phát** khi:
- Báo cáo đã được gắn cờ **nghi trùng** (`isPossibleDuplicate`), hoặc
- Trong **≤ 25m** (cùng category) đang có báo cáo **Verified**, **InProgress** hoặc **Reopened** (đang xử lý / dọn dẹp) — khi đó chỉ áp dụng cờ trùng lặp nếu có anchor active.

## API xử lý (LEO / DEO / Admin)

| Method | Route | Mô tả |
|--------|-------|-------|
| GET | `/v1/reports/{id}` | `isSuspectedViolationRecurrence`, `suspectedRecurrenceOfReportId`, `priorClosedReport` |
| GET | `/v1/reports/{id}/violation-recurrence-comparison` | So sánh side-by-side current vs prior |
| POST | `/v1/reports/{id}/dismiss-violation-recurrence` | Bác cờ (không cần lý do) |

## API giám sát / lọc danh sách

### LEO — hàng đợi phường

`GET /v1/reports/queue`

| Query param | Kiểu | Mô tả |
|-------------|------|-------|
| `page`, `pageSize` | int | Phân trang (default 1 / 20) |
| `search` | string | Code, address, category name/code |
| `status`, `severity`, `categoryId`, `wardCode` | filter | |
| `fromDate`, `toDate` | datetime | Khoảng `createdAt` |
| `slaBreached` | bool | SLA verify hoặc resolve đã quá hạn |
| `isPossibleDuplicate` | bool | Lọc cờ trùng (BR-REP-030) |
| **`isSuspectedViolationRecurrence`** | **bool** | **Lọc cờ tái phát** |
| `hasPendingReopenRequest` | bool | |
| `sortBy` | enum | `PriorityScore` (default), `CreatedAt`, `Severity`, `SlaVerifyDueAt`, `SlaResolveDueAt` |
| `sortDir` | enum | `Asc` / `Desc` (default `Desc`) |

Response item gồm: `isSuspectedViolationRecurrence`, `suspectedRecurrenceOfReportId`, `suspectedRecurrenceOfReportCode`.

Ví dụ LEO lọc nghi tái phát:

```
GET /v1/reports/queue?isSuspectedViolationRecurrence=true&sortBy=PriorityScore&sortDir=Desc
```

### DEO — toàn tỉnh (dashboard chính)

`GET /v1/departments/my/reports`

| Query param | Kiểu | Mô tả |
|-------------|------|-------|
| `page`, `pageSize` | int | Phân trang |
| `search` | string | Code, mô tả, địa chỉ, category name/code |
| `status`, `categoryId`, `severity`, `wardCode` | filter | |
| `assignedOfficeId` | guid | Lọc theo phường/office |
| `fromDate`, `toDate` | datetime | |
| `slaBreached` | bool | |
| `isPossibleDuplicate` | bool | |
| **`isSuspectedViolationRecurrence`** | **bool** | **Lọc cờ tái phát** |
| `hasPendingReopenRequest` | bool | |
| `sortBy` | string | `code`, `status`, `severity`, `priority`, `createdAt`, `verifiedAt`, `slaVerifyDueAt`, `slaResolveDueAt` |
| `sortDesc` | bool | default `false` |

Response item **mới** (mỗi row):

```json
{
  "isPossibleDuplicate": false,
  "possibleDuplicateOfReportId": null,
  "possibleDuplicateOfReportCode": null,
  "isSuspectedViolationRecurrence": true,
  "suspectedRecurrenceOfReportId": "uuid-prior-closed",
  "suspectedRecurrenceOfReportCode": "REP-20260701-0042"
}
```

Ví dụ DEO giám sát tái phát toàn tỉnh:

```
GET /v1/departments/my/reports?isSuspectedViolationRecurrence=true&page=1&pageSize=20
```

### Export CSV / Excel (LEO / DEO / Admin)

`GET /v1/reports/export`

| Query param | Mô tả |
|-------------|-------|
| `format` | `Csv` hoặc `Excel` (bắt buộc) |
| `status`, `severity`, `categoryId`, `wardCode`, `from`, `to` | Lọc |
| `isPossibleDuplicate` | bool |
| **`isSuspectedViolationRecurrence`** | **bool** |

Cột file **mới**: `IsSuspectedViolationRecurrence`, `SuspectedRecurrenceOfReportCode` (kèm cột duplicate tương ứng).

Ví dụ DEO export case nghi tái phát tháng này:

```
GET /v1/reports/export?format=Excel&isSuspectedViolationRecurrence=true&from=2026-07-01&to=2026-07-31
```

### KPI — không đổi

`GET /v1/reports/officer-kpi` — metrics hiệu suất officer (verify/resolve/SLA). **Không** có counter riêng cho tái phát; không cần đổi FE cho BR-REP-034.

## UI gợi ý

1. Badge **"Nghi tái phát"** trên queue/detail/bảng DEO (tách badge **"Nghi trùng"**).
2. Nút **So sánh** → comparison API, 2 cột ảnh/mô tả/timeline.
3. Nút **Bác cờ** → POST dismiss (mirror dismiss duplicate).
4. DEO dashboard: tab/filter **"Nghi tái phát"** gọi `/departments/my/reports?isSuspectedViolationRecurrence=true`.
5. Cờ **không bắt buộc** tạo InspectionReport — LEO vẫn có thể tạo inspection thủ công.

## Notification

- **LEO** (phường): push/email `ViolationRecurrenceReviewNeeded` khi cờ gắn lúc submit.
- **DEO**: chỉ khi báo cáo chưa có `AssignedOfficeId`.

## Submit response (Citizen)

`POST /v1/reports` response thêm:

```json
{
  "isSuspectedViolationRecurrence": true,
  "suspectedRecurrenceOfReportId": "uuid-prior-closed"
}
```

Citizen **không** cần hiển thị cờ này.
