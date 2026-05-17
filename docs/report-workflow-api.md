# GreenLens — API Documentation v1.1

> **Base URL**: `/v1` | **Auth**: Bearer JWT  
> **Response format**: `ApiResponse<T>` — `{ code, message, status, data }`

---

## Mục lục

1. [DepartmentsController — `/v1/departments`](#1-departmentscontroller)
2. [LocalOfficesController — `/v1/offices`](#2-localofficescontroller)
3. [TeamsController — `/v1/teams`](#3-teamscontroller)
4. [ReportsController — `/v1/reports`](#4-reportscontroller)
5. [AdminController — `/v1/admin`](#5-admincontroller)
6. [State Machine & Luồng hoàn chỉnh](#6-state-machine--luồng-hoàn-chỉnh)
7. [Roles & Quyền hạn](#7-roles--quyền-hạn)
8. [Error Codes](#8-error-codes)

---

## 1. DepartmentsController

> Route: `/v1/departments` — Quản lý Sở TNMT cấp Tỉnh/TP

| # | Method | Route | Summary | Role |
|---|--------|-------|---------|------|
| 1 | `GET` | `/` | Danh sách departments | Admin, DEO |
| 2 | `GET` | `/{id}` | Chi tiết department (kèm offices) | Admin, DEO |
| 3 | `POST` | `/` | Tạo department | Admin |
| 4 | `PUT` | `/{id}` | Cập nhật department | Admin |
| 5 | `DELETE` | `/{id}` | Vô hiệu hóa (soft-delete) | Admin |

### GET `/v1/departments`
**Query Params**: `page`, `pageSize`, `isActive` (optional)

### GET `/v1/departments/{id}`
**Response** gồm: thông tin department + danh sách offices trực thuộc (tên, wardCode, officer, số team).

### POST `/v1/departments`
```json
{ "name": "Sở TNMT TP.HCM", "provinceCode": "79" }
```

### PUT `/v1/departments/{id}`
```json
{ "name": "Sở TNMT TP.HCM (Updated)" }
```

---

## 2. LocalOfficesController

> Route: `/v1/offices` — Quản lý Văn phòng MT cấp Xã/Phường

| # | Method | Route | Summary | Role |
|---|--------|-------|---------|------|
| 1 | `GET` | `/` | Danh sách offices | Admin, DEO, LEO |
| 2 | `GET` | `/{id}` | Chi tiết office (kèm teams, officer) | Admin, DEO, LEO |
| 3 | `POST` | `/` | Tạo office | Admin |
| 4 | `PUT` | `/{id}` | Cập nhật office | Admin |
| 5 | `PUT` | `/{id}/officer` | Gán LEO cho office | Admin |

### GET `/v1/offices`
**Query Params**: `page`, `pageSize`, `departmentId`, `isOnboarded` (optional)

### GET `/v1/offices/{id}`
**Response** gồm: thông tin office + danh sách teams (tên, loại, active, số thành viên).

### POST `/v1/offices`
```json
{ "departmentId": "guid", "name": "VP MT Phường Bến Nghé", "wardCode": "26740" }
```

### PUT `/v1/offices/{id}/officer`
```json
{ "userId": "guid" }
```

---

## 3. TeamsController

> Route: `/v1/teams` — Quản lý Đội MT (Cleanup / Inspection)

| # | Method | Route | Summary | Role |
|---|--------|-------|---------|------|
| 1 | `GET` | `/` | Danh sách teams | Admin, LEO, DEO |
| 2 | `GET` | `/{id}` | Chi tiết team (kèm members) | Admin, LEO, DEO |
| 3 | `POST` | `/` | Tạo team | Admin |
| 4 | `PUT` | `/{id}` | Cập nhật team | Admin |
| 5 | `POST` | `/{id}/members` | Thêm thành viên | Admin |
| 6 | `DELETE` | `/{id}/members/{userId}` | Xóa thành viên | Admin |

### GET `/v1/teams`
**Query Params**: `page`, `pageSize`, `localOfficeId`, `teamType` (`Cleanup`/`Inspection`), `isActive`

### GET `/v1/teams/{id}`
**Response** gồm: thông tin team + danh sách members (tên, email, isLeader, ngày tham gia).

### POST `/v1/teams`
```json
{ "localOfficeId": "guid", "name": "Đội dọn dẹp 01", "teamType": "Cleanup" }
```

### POST `/v1/teams/{id}/members`
```json
{ "userId": "guid", "isLeader": false }
```

---

## 4. ReportsController

> Route: `/v1/reports` — CRUD + Workflow lifecycle + Queries

### 4.1 CRUD & Queries

| # | Method | Route | Summary | Role |
|---|--------|-------|---------|------|
| 1 | `POST` | `/` | Tạo báo cáo ô nhiễm | Citizen / Anonymous |
| 2 | `GET` | `/` | Danh sách báo cáo | Auth |
| 3 | `GET` | `/{id}` | Chi tiết báo cáo (media, assignments) | Auth |
| 4 | `GET` | `/my` | Báo cáo của tôi | Citizen |
| 5 | `GET` | `/{id}/history` | Lịch sử status | Auth |
| 6 | `GET` | `/queue` | Hàng đợi officer | LEO, DEO |

### POST `/v1/reports`
```json
{
  "categoryId": "guid", "severity": "Medium",
  "description": "Rác thải tràn ngập...",
  "latitude": 10.7626, "longitude": 106.6602,
  "address": "123 Đường ABC", "wardCode": "26740", "provinceCode": "79",
  "isAnonymous": false,
  "images": [{ "url": "https://...", "mimeType": "image/jpeg", "sizeBytes": 204800 }]
}
```

### GET `/v1/reports`
**Query Params**: `page`, `pageSize`, `status`, `categoryId`, `wardCode`, `severity`

### GET `/v1/reports/{id}`
**Response** gồm: full report info + media list + assignments list (teamId, teamName, status, timestamps).

### GET `/v1/reports/my`
**Query Params**: `page`, `pageSize`, `status`

### GET `/v1/reports/{id}/history`
**Response**: timeline status changes (fromStatus → toStatus, changedBy, reason, timestamp).

### GET `/v1/reports/queue`
**Query Params**: `page`, `pageSize`, `status`

---

### 4.2 Officer Workflow

| # | Method | Route | Summary | Role |
|---|--------|-------|---------|------|
| 7 | `PUT` | `/{id}/verify` | Xác minh báo cáo | LEO, DEO |
| 8 | `PUT` | `/{id}/reject` | Từ chối báo cáo | LEO, DEO |
| 9 | `POST` | `/{id}/assign` | Phân công team(s) | LEO, DEO |
| 10 | `PUT` | `/{id}/reassign` | Chuyển giao team | LEO, DEO |

### PUT `/v1/reports/{id}/verify`
```json
{ "overrideSeverity": "High", "overrideCategoryId": "guid" }
```
> Cả 2 field optional. Chuyển Submitted → Verified.

### PUT `/v1/reports/{id}/reject`
```json
{ "reason": "Ảnh không phản ánh ô nhiễm thực tế..." }
```
> `reason` ≥ 20 ký tự. Chuyển Submitted → Rejected.

### POST `/v1/reports/{id}/assign`
```json
{
  "teams": [
    { "teamId": "guid-a", "note": "Khu vực phía Bắc" },
    { "teamId": "guid-b", "note": "Khu vực phía Nam" }
  ]
}
```
> Tất cả team ngang hàng. Chuyển Verified → InProgress.

### PUT `/v1/reports/{id}/reassign`
```json
{ "oldTeamId": "guid", "newTeamId": "guid", "reason": "Team A quá tải..." }
```
> `reason` ≥ 20 ký tự. Chỉ chuyển giữa team cùng loại.

---

### 4.3 Team Workflow

| # | Method | Route | Summary | Role |
|---|--------|-------|---------|------|
| 11 | `PUT` | `/{id}/resolve` | Hoàn thành phần việc | Cleanup |
| 12 | `PUT` | `/{id}/penalty` | Xử phạt vi phạm | Inspector |
| 13 | `PUT` | `/{id}/close-no-violation` | Đóng — không vi phạm | Inspector |
| 14 | `PUT` | `/{id}/decline` | Từ chối task | Cleanup, Inspector |

### PUT `/v1/reports/{id}/resolve`
```json
{ "teamId": "guid", "afterImageUrls": ["url1", "url2"] }
```
> ≥ 2 ảnh. Mark team completed → khi ALL team completed → InProgress → Resolved.

### PUT `/v1/reports/{id}/penalty`
```json
{ "teamId": "guid" }
```
> Khi ALL team completed → InProgress → PenaltyIssued.

### PUT `/v1/reports/{id}/close-no-violation`
```json
{ "reason": "Sau khi khảo sát hiện trường, không phát hiện vi phạm..." }
```
> `reason` ≥ 50 ký tự. InProgress → ClosedNoViolation.

### PUT `/v1/reports/{id}/decline`
```json
{ "teamId": "guid", "reason": "Khu vực ngoài phạm vi..." }
```
> `reason` ≥ 20 ký tự. Chỉ trong 2h đầu sau khi được gán.

---

### 4.4 Citizen Workflow

| # | Method | Route | Summary | Role |
|---|--------|-------|---------|------|
| 15 | `PUT` | `/{id}/close` | Đóng báo cáo | Citizen / Auto |
| 16 | `PUT` | `/{id}/reopen` | Mở lại báo cáo | Citizen |

### PUT `/v1/reports/{id}/close`
> Chuyển Resolved/PenaltyIssued → Closed. Không cần body.

### PUT `/v1/reports/{id}/reopen`
> Tối đa 2 lần. Chuyển Resolved → InProgress. Không cần body.

## 5. AdminController

> Route: `/v1/admin` — Admin Dashboard APIs (Require `Admin` role)

### 5.1 Users — `/v1/admin/users`

| # | Method | Route | Summary |
|---|--------|-------|---------|
| 1 | `POST` | `/users` | Tạo tài khoản |
| 2 | `GET` | `/users/all` | Toàn bộ users (không phân trang) |
| 3 | `GET` | `/users` | Danh sách users (phân trang) |
| 4 | `GET` | `/users/{id}` | Chi tiết user |
| 5 | `PUT` | `/users/{id}` | Cập nhật user |
| 6 | `DELETE` | `/users/{id}` | Xóa user (soft-delete) |
| 7 | `PUT` | `/users/{id}/role` | Đổi role user |

### PUT `/v1/admin/users/{id}/role`
```json
{ "newRole": "LEO" }
```

### 5.2 Reports — `/v1/admin/reports`

| # | Method | Route | Summary |
|---|--------|-------|---------|
| 8 | `GET` | `/reports` | Danh sách báo cáo (admin view, full metadata) |
| 9 | `GET` | `/reports/{id}` | Chi tiết báo cáo |
| 10 | `PUT` | `/reports/{id}/status` | Force cập nhật status (bypass state machine) |

### GET `/v1/admin/reports`
**Query Params**: `page`, `pageSize`, `status`, `categoryId`, `wardCode`, `provinceCode`, `search`

### PUT `/v1/admin/reports/{id}/status`
```json
{ "newStatus": "Resolved", "reason": "Admin override — data correction" }
```
> Bypass state machine. Chỉ dùng cho trường hợp đặc biệt. Ghi audit trail.

### 5.3 Pollution Categories — `/v1/admin/pollution-categories`

| # | Method | Route | Summary |
|---|--------|-------|---------|
| 11 | `POST` | `/pollution-categories` | Tạo danh mục ô nhiễm |
| 12 | `PUT` | `/pollution-categories/{id}` | Cập nhật danh mục |
| 13 | `DELETE` | `/pollution-categories/{id}` | Xóa danh mục (deactivate) |
| 14 | `PUT` | `/pollution-categories/{id}/archive` | Archive/Unarchive |

### POST `/v1/admin/pollution-categories`
```json
{ "code": "NOISE", "nameVi": "Ô nhiễm tiếng ồn", "nameEn": "Noise Pollution", "iconUrl": "..." }
```

### PUT `/v1/admin/pollution-categories/{id}/archive`
```json
{ "archive": true }
```

### 5.4 Roles & Permissions

| # | Method | Route | Summary |
|---|--------|-------|---------|
| 15 | `GET` | `/roles` | Danh sách roles hệ thống |
| 16 | `GET` | `/permissions` | Ma trận phân quyền theo role |

> Roles là enum cố định (`Citizen`, `DEO`, `LEO`, `Cleanup`, `Inspector`, `Admin`).

---

## 6. State Machine & Luồng hoàn chỉnh

```
                         ┌─── Rejected
                         │
Submitted ──► Verified ──┼──► InProgress ──┬──► Resolved ──┬──► Closed
    │                    │                 │               │
    │                    │                 ├──► PenaltyIssued ──► Closed
    │                    │                 │
    └── Duplicate        │                 └──► ClosedNoViolation
                         │
                         └ (Resolved → InProgress: reopen, max 2 lần)
```

### Luồng Cleanup
```
1. Citizen:  POST /v1/reports              → Submitted
2. LEO:      PUT  /{id}/verify             → Verified
3. LEO:      POST /{id}/assign             → InProgress (N team ngang hàng)
4. Team(s):  PUT  /{id}/resolve            → mỗi team mark completed
                                            → ALL done → Resolved
5. Citizen:  PUT  /{id}/close              → Closed
```

### Luồng Inspection
```
1. Citizen:  POST /v1/reports              → Submitted
2. LEO:      PUT  /{id}/verify             → Verified
3. LEO:      POST /{id}/assign             → InProgress
4a. Team(s): PUT  /{id}/penalty            → ALL done → PenaltyIssued
4b. Team:    PUT  /{id}/close-no-violation → ClosedNoViolation
5. Citizen:  PUT  /{id}/close              → Closed
```

---

## 7. Roles & Quyền hạn

| Role | Mô tả | Endpoints |
|------|--------|-----------|
| `Citizen` | Người dân | Submit, GET my, Close, Reopen |
| `LEO` | Officer cấp Xã | Verify, Reject, Assign, Reassign, Queue, GET offices/teams |
| `DEO` | Officer cấp Tỉnh | Như LEO + GET departments |
| `Cleanup` | Đội dọn dẹp | Resolve, Decline |
| `Inspector` | Đội thanh tra | Penalty, CloseNoViolation, Decline |
| `Admin` | Quản trị | Tất cả APIs |

---

## 8. Error Codes

| Code | HTTP | Mô tả |
|------|------|--------|
| `REPORT_NOT_FOUND` | 404 | Không tìm thấy báo cáo |
| `INVALID_STATUS_TRANSITION` | 422 | Không thể chuyển trạng thái |
| `CONFLICT_OF_INTEREST` | 422 | Không xử lý báo cáo do mình tạo |
| `TEAM_TYPE_MISMATCH` | 422 | Loại team không khớp loại ô nhiễm |
| `TEAM_WORKLOAD_EXCEEDED` | 422 | Team đạt giới hạn 10 In-Progress |
| `AT_LEAST_ONE_TEAM` | 400 | Phải phân công ít nhất 1 team |
| `REASON_TOO_SHORT` | 400 | Lý do phải ≥ 20 ký tự |
| `REOPEN_LIMIT_REACHED` | 422 | Đã hết 2 lần mở lại |
| `DECLINE_WINDOW_EXPIRED` | 422 | Quá 2h để từ chối |
| `ASSIGNMENT_NOT_FOUND` | 404 | Không tìm thấy assignment |
| `DEPARTMENT_NOT_FOUND` | 404 | Không tìm thấy department |
| `DEPARTMENT_ALREADY_EXISTS` | 409 | Tỉnh đã có department |
| `OFFICE_NOT_FOUND` | 404 | Không tìm thấy office |
| `LOCAL_OFFICE_ALREADY_EXISTS` | 409 | Ward đã có office |
| `TEAM_NOT_FOUND` | 404 | Không tìm thấy team |
| `MEMBER_NOT_FOUND` | 404 | Không tìm thấy thành viên |
| `MEMBER_ALREADY_IN_TEAM` | 409 | User đã trong team |
| `INVALID_ROLE_FOR_OFFICER` | 422 | Phải có role LEO |
| `INVALID_ROLE_FOR_TEAM_MEMBER` | 422 | Phải có role Cleanup/Inspector |
| `INSUFFICIENT_AFTER_IMAGES` | 400 | Cần ≥ 2 ảnh after |
