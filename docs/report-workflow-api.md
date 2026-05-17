# GreenLens — API Quản lý Tổ chức & Luồng Xử lý Báo cáo Ô nhiễm

> **Version**: 1.1 | **Base URL**: `/v1` | **Auth**: Bearer JWT  
> **Response format**: `ApiResponse<T>` — `{ code, message, status, data }`

---

## Mục lục

1. [Tổng quan kiến trúc tổ chức](#1-tổng-quan-kiến-trúc-tổ-chức)
2. [API Quản lý Tổ chức (Admin)](#2-api-quản-lý-tổ-chức-admin)
3. [API Luồng Báo cáo (Report Workflow)](#3-api-luồng-báo-cáo-report-workflow)
4. [State Machine & Luồng hoàn chỉnh](#4-state-machine--luồng-hoàn-chỉnh)
5. [Roles & Quyền hạn](#5-roles--quyền-hạn)
6. [Error Codes](#6-error-codes)

---

## 1. Tổng quan kiến trúc tổ chức

```
Department (cấp Tỉnh/TP)        ← DEO quản lý
 └── LocalOffice (cấp Xã/Phường) ← LEO phụ trách
      ├── Cleanup Team (1..N)     ← Xử lý: Rác, Nước thải, Hóa chất
      └── Inspection Team (1..N)  ← Xử phạt: Tiếng ồn, Không khí
```

**Routing tự động khi submit**: FE gửi `wardCode` + `provinceCode` → BE tự gán report cho LocalOffice (nếu ward đã onboard) hoặc Department Queue (nếu chưa).

---

## 2. API Quản lý Tổ chức (Admin)

> **Controller**: `OrganizationController` — Route: `/v1/organization`  
> **Auth**: `Admin` only

### 2.1 Tạo Department

```
POST /v1/organization/departments
```

Tạo Sở Tài nguyên & Môi trường cấp Tỉnh/TP. Mỗi tỉnh chỉ có 1 Department.

**Request Body:**
```json
{
  "name": "Sở TNMT TP.HCM",
  "provinceCode": "79"
}
```

**Response** `201`:
```json
{
  "code": "SUCCESS",
  "status": 201,
  "data": { "id": "guid", "name": "...", "provinceCode": "79" }
}
```

| Error | Mô tả |
|-------|--------|
| `409` | Tỉnh đã có Department |
| `404` | Province code không tồn tại |

---

### 2.2 Tạo Local Office

```
POST /v1/organization/offices
```

Onboard 1 văn phòng cấp xã/phường. Sau khi tạo, báo cáo trong ward đó sẽ tự động route đến office này.

**Request Body:**
```json
{
  "departmentId": "guid",
  "name": "VP MT Phường Bến Nghé",
  "wardCode": "26740"
}
```

**Response** `201`: trả về `{ id, name, wardCode, departmentId }`

| Error | Mô tả |
|-------|--------|
| `409` | Ward đã có office |
| `404` | Department không tồn tại |

---

### 2.3 Gán LEO cho Office

```
PUT /v1/organization/offices/{officeId}/officer
```

Gán 1 user có role `LEO` làm người phụ trách office.

**Request Body:**
```json
{ "userId": "guid" }
```

**Response** `204 No Content`

| Error | Mô tả |
|-------|--------|
| `404` | Office hoặc User không tồn tại |
| `422` | User không có role LEO |

---

### 2.4 Tạo Team

```
POST /v1/organization/teams
```

Tạo team Cleanup hoặc Inspection dưới 1 LocalOffice.

**Request Body:**
```json
{
  "localOfficeId": "guid",
  "name": "Đội dọn dẹp Phường BN - 01",
  "teamType": "Cleanup"
}
```
`teamType`: `"Cleanup"` | `"Inspection"`

**Response** `201`: trả về `{ id, name, teamType, localOfficeId }`

---

### 2.5 Thêm Team Member

```
POST /v1/organization/teams/{teamId}/members
```

**Request Body:**
```json
{
  "userId": "guid",
  "isLeader": false
}
```

**Validation**: User có role `Cleanup` chỉ vào team Cleanup, role `Inspector` chỉ vào team Inspection.

**Response** `201`: trả về `{ id, teamId, userId, isLeader }`

---

## 3. API Luồng Báo cáo (Report Workflow)

> **Controllers**: `PollutionReportsController` + `ReportWorkflowController`

### 3.1 Citizen: Submit báo cáo

```
POST /v1/pollution-reports
```
**Auth**: Không bắt buộc (hỗ trợ anonymous)

Tạo báo cáo mới. Hệ thống tự động:
- Gán status = `Submitted`
- Set SLA verify = 24h
- **Auto-route** theo `wardCode`:
  - Ward đã onboard → gán `AssignedOfficeId`
  - Ward chưa onboard → gán `AssignedDepartmentId` (common queue)

**Request Body:**
```json
{
  "categoryId": "guid",
  "severity": "Medium",
  "description": "Rác thải tràn ngập trên đường...",
  "latitude": 10.7626,
  "longitude": 106.6602,
  "address": "123 Đường ABC, Q1",
  "wardCode": "26740",
  "provinceCode": "79",
  "isAnonymous": false,
  "images": [
    { "url": "https://...", "mimeType": "image/jpeg", "sizeBytes": 204800 }
  ]
}
```

**Response** `201`: Trả về full report info + code (VD: `RPT-260517-A3F2B1`)

---

### 3.2 Officer: Xác minh báo cáo (Verify)

```
PUT /v1/reports/{id}/verify
```
**Auth**: `LEO`, `DEO`, `Admin`

LEO kiểm tra thông tin báo cáo, có thể điều chỉnh loại ô nhiễm và mức độ. Chuyển status: `Submitted → Verified`.

**Business Rules:**
- BR-OFF-004: Không được verify báo cáo do chính mình tạo (conflict of interest)
- BR-OFF-003: Có thể override severity và category

**Request Body:**
```json
{
  "overrideSeverity": "High",
  "overrideCategoryId": "guid"
}
```
> Cả 2 field đều optional. Nếu không gửi → giữ nguyên giá trị gốc.

**Response** `204 No Content`

---

### 3.3 Officer: Từ chối báo cáo (Reject)

```
PUT /v1/reports/{id}/reject
```
**Auth**: `LEO`, `DEO`, `Admin`

Từ chối báo cáo không hợp lệ. Chuyển status: `Submitted → Rejected`.

**Request Body:**
```json
{ "reason": "Ảnh không phản ánh ô nhiễm thực tế, không đủ bằng chứng" }
```
> `reason` phải ≥ 20 ký tự (BR-REP-022)

**Response** `204 No Content`

---

### 3.4 Officer: Phân công Team (Assign)

```
POST /v1/reports/{id}/assign
```
**Auth**: `LEO`, `DEO`, `Admin`

Phân công 1 hoặc **nhiều team** cùng xử lý 1 báo cáo. Tất cả team ngang hàng (không phân biệt chính/phụ). Chuyển status: `Verified → InProgress`.

**Business Rules:**
- BR-ORG-013: Team type phải khớp loại ô nhiễm (Cleanup cho Rác/Nước/Hóa chất, Inspection cho Tiếng ồn/Không khí)
- BR-OFF-013: Mỗi team tối đa 10 báo cáo In-Progress cùng lúc
- Mỗi assignment có status riêng: `Assigned → InProgress → Completed`
- Report chỉ chuyển sang Resolved/PenaltyIssued khi **TẤT CẢ** team đều Completed

**Request Body:**
```json
{
  "teams": [
    { "teamId": "guid-team-a", "note": "Khu vực phía Bắc" },
    { "teamId": "guid-team-b", "note": "Khu vực phía Nam" }
  ]
}
```

**Response** `204 No Content`

---

### 3.5 Officer: Chuyển giao Team (Reassign)

```
PUT /v1/reports/{id}/reassign
```
**Auth**: `LEO`, `DEO`, `Admin`

Chuyển 1 assignment từ team cũ sang team mới. Chỉ chuyển giữa team **cùng loại** (Cleanup↔Cleanup, Inspection↔Inspection).

**Request Body:**
```json
{
  "oldTeamId": "guid",
  "newTeamId": "guid",
  "reason": "Team A đang quá tải, chuyển cho team B xử lý"
}
```
> `reason` ≥ 20 ký tự (BR-OFF-012)

**Response** `204 No Content`

---

### 3.6 Cleanup Team: Hoàn thành (Resolve)

```
PUT /v1/reports/{id}/resolve
```
**Auth**: `Cleanup`, `Admin`

Team Cleanup đánh dấu **phần việc của team mình** là hoàn thành. Khi tất cả team đều completed → report chuyển `InProgress → Resolved`.

**Request Body:**
```json
{
  "teamId": "guid",
  "afterImageUrls": [
    "https://storage.../after-1.jpg",
    "https://storage.../after-2.jpg"
  ]
}
```
> Phải có ≥ 2 ảnh after từ các góc khác nhau (BR-CLN-005)

**Response** `204 No Content`

**Logic multi-team:**
```
3 team được gán → Team A resolve → vẫn InProgress
                → Team B resolve → vẫn InProgress
                → Team C resolve → ALL done → Resolved ✅
```

---

### 3.7 Inspection Team: Xử phạt (Issue Penalty)

```
PUT /v1/reports/{id}/penalty
```
**Auth**: `Inspector`, `Admin`

Inspection Team Leader ban hành quyết định xử phạt. Logic tương tự Resolve — chỉ chuyển `InProgress → PenaltyIssued` khi tất cả team completed.

**Request Body:**
```json
{ "teamId": "guid" }
```

**Response** `204 No Content`

---

### 3.8 Inspection Team: Đóng — Không vi phạm

```
PUT /v1/reports/{id}/close-no-violation
```
**Auth**: `Inspector`, `Admin`

Sau khảo sát không đủ căn cứ vi phạm. Chuyển `InProgress → ClosedNoViolation`.

**Request Body:**
```json
{ "reason": "Sau khi khảo sát hiện trường, không phát hiện nguồn gây ô nhiễm không khí. Chỉ số AQI đo được trong ngưỡng cho phép." }
```
> `reason` ≥ 50 ký tự (BR-INS-013)

**Response** `204 No Content`

---

### 3.9 Citizen/Auto: Đóng báo cáo (Close)

```
PUT /v1/reports/{id}/close
```
**Auth**: Bất kỳ user đã đăng nhập

Citizen xác nhận hài lòng hoặc hệ thống auto-close sau 7 ngày. Chuyển `Resolved → Closed` hoặc `PenaltyIssued → Closed`.

**Response** `204 No Content`

---

### 3.10 Citizen: Mở lại báo cáo (Reopen)

```
PUT /v1/reports/{id}/reopen
```
**Auth**: Bất kỳ user đã đăng nhập

Citizen không hài lòng → mở lại. Tối đa **2 lần** reopen (BR-REP-015). Chuyển `Resolved → InProgress`.

**Response** `204 No Content`

| Error | Mô tả |
|-------|--------|
| `422 REOPEN_LIMIT_REACHED` | Đã hết 2 lần reopen |

---

### 3.11 Team: Từ chối Task (Decline)

```
PUT /v1/reports/{id}/decline
```
**Auth**: `Cleanup`, `Inspector`, `Admin`

Team từ chối task **trong vòng 2 giờ** sau khi được gán (BR-CLN-007, BR-INS-003).

**Request Body:**
```json
{
  "teamId": "guid",
  "reason": "Khu vực ngoài phạm vi hoạt động của đội"
}
```
> `reason` ≥ 20 ký tự. Quá 2h → lỗi `DECLINE_WINDOW_EXPIRED`.

**Response** `204 No Content`

---

### 3.12 Officer: Xem hàng đợi (Queue)

```
GET /v1/reports/queue?page=1&pageSize=20&status=Submitted
```
**Auth**: `LEO`, `DEO`, `Admin`

Trả về danh sách báo cáo trong phạm vi quản lý, sắp theo điểm ưu tiên giảm dần.

- **LEO**: chỉ thấy báo cáo trong xã/phường mình phụ trách
- **DEO**: thấy tất cả báo cáo trong tỉnh + department queue

**Query Params:**

| Param | Type | Default | Mô tả |
|-------|------|---------|--------|
| `page` | int | 1 | Trang |
| `pageSize` | int | 20 | Số item/trang |
| `status` | string? | null | Filter theo status (optional) |

**Response** `200`:
```json
{
  "code": "SUCCESS",
  "data": {
    "items": [
      {
        "id": "guid",
        "code": "RPT-260517-A3F2B1",
        "categoryCode": "TRASH",
        "categoryName": "Rác thải",
        "severity": "High",
        "status": "Submitted",
        "latitude": 10.7626,
        "longitude": 106.6602,
        "address": "123 ABC",
        "wardCode": "26740",
        "priorityScore": 15.5,
        "createdAt": "2026-05-17T08:00:00Z",
        "slaVerifyDueAt": "2026-05-18T08:00:00Z",
        "slaResolveDueAt": null
      }
    ],
    "totalCount": 42,
    "page": 1,
    "pageSize": 20
  }
}
```

---

## 4. State Machine & Luồng hoàn chỉnh

### State Machine

```
                         ┌─── Rejected
                         │
Submitted ──► Verified ──┼──► InProgress ──┬──► Resolved ──┬──► Closed
    │              │     │                 │               │
    │              │     │                 ├──► PenaltyIssued ──► Closed
    │              │     │                 │
    └── Duplicate  │     │                 └──► ClosedNoViolation
                   │     │
                   │     └ (Resolved → InProgress: reopen, max 2 lần)
```

### Luồng Cleanup (Rác / Nước thải / Hóa chất)

```
1. Citizen:  POST /v1/pollution-reports           → Submitted (auto-route)
2. LEO:      PUT  /v1/reports/{id}/verify         → Verified
3. LEO:      POST /v1/reports/{id}/assign         → InProgress (gán N team)
4. Team:     PUT  /v1/reports/{id}/resolve        → mỗi team mark completed
                                                   → khi ALL done → Resolved
5. Citizen:  PUT  /v1/reports/{id}/close          → Closed
   (hoặc)    PUT  /v1/reports/{id}/reopen         → InProgress (max 2 lần)
```

### Luồng Inspection (Tiếng ồn / Không khí)

```
1. Citizen:  POST /v1/pollution-reports           → Submitted (auto-route)
2. LEO:      PUT  /v1/reports/{id}/verify         → Verified
3. LEO:      POST /v1/reports/{id}/assign         → InProgress (gán N team)
4a. Team:    PUT  /v1/reports/{id}/penalty         → khi ALL done → PenaltyIssued
4b. Team:    PUT  /v1/reports/{id}/close-no-violation → ClosedNoViolation
5. Citizen:  PUT  /v1/reports/{id}/close          → Closed
```

### Luồng từ chối / chuyển giao

```
- LEO reject:      PUT /v1/reports/{id}/reject      → Rejected
- Team decline:    PUT /v1/reports/{id}/decline      → assignment Declined (2h window)
- LEO reassign:    PUT /v1/reports/{id}/reassign     → team cũ Declined, team mới Assigned
```

---

## 5. Roles & Quyền hạn

| Role | Mô tả | Quyền trên Report |
|------|--------|-------------------|
| `Citizen` | Người dân | Submit, Close, Reopen |
| `LEO` | Officer cấp Xã/Phường | Verify, Reject, Assign, Reassign, Queue |
| `DEO` | Officer cấp Tỉnh/TP | Verify, Reject, Assign, Reassign, Queue (toàn tỉnh) |
| `Cleanup` | Thành viên đội dọn dẹp | Resolve, Decline |
| `Inspector` | Thành viên đội thanh tra | Penalty, CloseNoViolation, Decline |
| `Admin` | Quản trị hệ thống | Tất cả + Organization APIs |

---

## 6. Error Codes

| Code | HTTP | Mô tả |
|------|------|--------|
| `REPORT_NOT_FOUND` | 404 | Không tìm thấy báo cáo |
| `INVALID_STATUS_TRANSITION` | 422 | Không thể chuyển trạng thái |
| `CONFLICT_OF_INTEREST` | 422 | Không thể xử lý báo cáo do mình tạo |
| `TEAM_TYPE_MISMATCH` | 422 | Loại team không khớp loại ô nhiễm |
| `TEAM_WORKLOAD_EXCEEDED` | 422 | Team đạt giới hạn 10 In-Progress |
| `AT_LEAST_ONE_TEAM` | 400 | Phải phân công ít nhất 1 team |
| `REASON_TOO_SHORT` | 400 | Lý do phải ≥ 20 ký tự |
| `REASON_TOO_SHORT_50` | 400 | Lý do phải ≥ 50 ký tự |
| `REOPEN_LIMIT_REACHED` | 422 | Đã hết 2 lần mở lại |
| `DECLINE_WINDOW_EXPIRED` | 422 | Quá 2h để từ chối task |
| `ASSIGNMENT_NOT_FOUND` | 404 | Không tìm thấy assignment cho team |
| `NOT_TEAM_MEMBER` | 422 | Không phải thành viên team |
| `REASSIGN_SAME_TEAM_TYPE` | 422 | Chỉ chuyển giữa team cùng loại |
| `INSUFFICIENT_AFTER_IMAGES` | 400 | Cần ≥ 2 ảnh after |
| `CATEGORY_NOT_FOUND` | 404 | Danh mục ô nhiễm không tồn tại |
| `INVALID_WARD_PROVINCE` | 400 | Ward/Province code không khớp |

---

> **Ghi chú**: Tài liệu này dựa trên Business Rules v1.1. Các luồng Penalty Decision chi tiết (biên bản, khung phạt, theo dõi nộp phạt) sẽ triển khai ở phase sau.
