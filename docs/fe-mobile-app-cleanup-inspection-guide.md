# Mobile App — API Reference (Cleanup & Inspection Update)

> **Version:** 2.0 — cập nhật sau khi implement BR-CLN-002..008, BR-INS-003/004/030..032  
> **Roles trên Mobile:** `Citizen`, `Cleaner`, `Inspector`, `CompanyStaff`  
> **Prefix:** `/v1` · **Envelope:** `{ code, message, status, data }`  
> **Auth:** JWT Bearer (`Authorization: Bearer <token>`)

---

## Mục lục

1. [Auth (tất cả roles)](#1-auth-tất-cả-roles)
2. [Citizen](#2-citizen)
3. [Cleaner (Đội cộng đồng)](#3-cleaner-đội-cộng-đồng)
4. [CompanyStaff (Đội công ty)](#4-companystaff-đội-công-ty)
5. [Inspector (Đội xử phạt)](#5-inspector-đội-xử-phạt)
6. [Shared Endpoints](#6-shared-endpoints-tất-cả-roles)
7. [Enums & Constants](#7-enums--constants)
8. [Error Codes](#8-error-codes-quan-trọng)

---

## 1. Auth (tất cả roles)

### 1.1 Đăng ký

```http
POST /v1/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "phoneNumber": "0901234567",
  "password": "P@ssw0rd!",
  "confirmPassword": "P@ssw0rd!",
  "fullName": "Nguyễn Văn A",
  "acceptTerms": true
}
```

**Response:** `201` → `{ data: { userId, email } }`

### 1.2 Xác thực OTP

```http
POST /v1/auth/verify-otp
{ "email": "user@example.com", "otp": "123456" }
```

### 1.3 Đăng nhập

```http
POST /v1/auth/login
{ "emailOrPhone": "user@example.com", "password": "P@ssw0rd!" }
```

**Response:** `{ data: { accessToken, refreshToken, user: { id, email, role, fullName } } }`

### 1.4 Refresh Token

```http
POST /v1/auth/refresh-token
{ "refreshToken": "..." }
```

### 1.5 Đổi mật khẩu

```http
PUT /v1/auth/change-password
Authorization: Bearer <token>
{ "currentPassword": "...", "newPassword": "...", "confirmNewPassword": "..." }
```

### 1.6 Quên mật khẩu

```http
POST /v1/auth/forgot-password
{ "email": "user@example.com" }

POST /v1/auth/reset-password
{ "email": "...", "otp": "123456", "newPassword": "...", "confirmNewPassword": "..." }
```

---

## 2. Citizen

### 2.1 Submit Report

```http
POST /v1/reports
Authorization: Bearer <token>
Content-Type: application/json

{
  "latitude": 10.7621,
  "longitude": 106.6603,
  "address": "123 Nguyễn Huệ, Q.1, TP.HCM",
  "description": "Bãi rác tự phát trên vỉa hè",
  "categoryId": "<guid>",
  "severity": "Medium"
}
```

**Response:** `201` → `{ data: { reportId, reportCode } }`

### 2.2 Upload ảnh/video

```http
POST /v1/reports/{reportId}/images
Authorization: Bearer <token>
Content-Type: multipart/form-data
(field: file, max 5 ảnh, mỗi ảnh ≤ 10MB)

POST /v1/reports/{reportId}/video
Content-Type: multipart/form-data
(field: file, 1 video mp4/mov ≤ 100MB/60s)
```

### 2.3 Draft (lưu nháp)

```http
POST /v1/reports/drafts                    # Lưu nháp (max 3)
GET  /v1/reports/drafts                    # Danh sách nháp
GET  /v1/reports/drafts/{id}               # Chi tiết nháp
PUT  /v1/reports/drafts/{id}               # Cập nhật nháp
DELETE /v1/reports/drafts/{id}             # Xóa nháp
POST /v1/reports/drafts/{id}/submit        # Submit nháp → Report
```

### 2.4 Xem báo cáo của tôi

```http
GET /v1/reports/my-reports?page=1&pageSize=20&status=Submitted
```

**Query params:** `page`, `pageSize`, `status` (optional), `search` (optional)

### 2.5 Chi tiết báo cáo

```http
GET /v1/reports/{id}
```

**Response bao gồm:** media (images/video), assignments, statusHistory, `canRate`, `canReopen`, `canDelete`

### 2.6 Xóa báo cáo (chỉ khi Submitted)

```http
DELETE /v1/reports/{id}
```

### 2.7 Đánh giá sau Resolved (BR-REP-018)

```http
POST /v1/reports/{id}/rate
{ "rating": 5, "comment": "Xử lý rất tốt!" }
```

Rating: 1–5 sao, 1 lần duy nhất/report, chỉ khi Resolved hoặc Closed.

### 2.8 Xác nhận hoặc mở lại (BR-REP-015)

```http
PUT /v1/reports/{id}/close       # Citizen hài lòng → Closed (không body)
PUT /v1/reports/{id}/reopen      # Chưa hài lòng → InProgress (max 2 lần, 7 ngày, không body)
```

Chi tiết + đánh giá sao: [`fe-citizen-satisfaction-api-guide.md`](./Changelogs/fe-citizen-satisfaction-api-guide.md)

### 2.9 Bản đồ công khai

```http
GET /v1/map/reports?minLat=10.7&maxLat=10.8&minLng=106.6&maxLng=106.7&status=Verified&categoryId=<guid>
```

**Note:** GPS được round 10m cho public (BR-MAP-004).

### 2.10 Danh mục ô nhiễm

```http
GET /v1/categories                         # 3 loại: Rác thải, Nước thải, Hóa chất
```

### 2.11 Gamification

```http
GET /v1/gamification/my-points             # Điểm & level hiện tại
GET /v1/gamification/my-badges             # Huy hiệu đã đạt
GET /v1/gamification/leaderboard?period=ThisMonth  # Bảng xếp hạng
```

### 2.12 Notifications

```http
GET    /v1/notifications?page=1&pageSize=20              # Danh sách thông báo
PUT    /v1/notifications/{id}/read                        # Đánh dấu đã đọc
PUT    /v1/notifications/read-all                         # Đánh dấu tất cả
GET    /v1/notifications/preferences                      # Cài đặt thông báo
PUT    /v1/notifications/preferences                      # Cập nhật
PUT    /v1/notifications/device-token                     # FCM token
{ "deviceToken": "...", "platform": "iOS" }               # platform: iOS | Android
```

### 2.13 Profile & Account

```http
GET    /v1/users/me                                       # Thông tin cá nhân
PUT    /v1/users/me                                       # Cập nhật profile
POST   /v1/users/me/consent                               # Đồng ý điều khoản (bắt buộc lần đầu)
GET    /v1/users/me/data-export?format=json                # Xuất dữ liệu cá nhân
POST   /v1/users/me/delete                                 # Yêu cầu xóa tài khoản (90 ngày)
POST   /v1/users/me/restore                                # Khôi phục tài khoản
```

---

## 3. Cleaner (Đội cộng đồng)

> **Role JWT:** `Cleaner`  
> **Ai:** Thành viên đội MT cộng đồng do LEO tạo.  
> **Team Leader vs Member:** Chỉ leader mới accept/decline/progress/resolve/check-in/escalate.

### 3.1 Profile team

```http
GET /v1/teams/my-profile
```

**Response:** `teamId`, `name`, `members[]`, `type`, `localOfficeId`

### 3.2 Danh sách task

```http
GET /v1/teams/my-tasks?page=1&pageSize=20&assignmentStatus=InProgress
```

**assignmentStatus filter:** `Assigned`, `InProgress`, `Completed`, `Declined`, `Escalated`

### 3.3 Chi tiết task

```http
GET /v1/teams/my-tasks/{reportId}
```

**Response bao gồm:** report info, before images, progress hiện tại, SLA deadline, và action flags:

```json
{
  "data": {
    "reportId": "...",
    "reportCode": "RPT-2026-001234",
    "assignmentStatus": "InProgress",
    "progressPercent": 60,
    "canDecline": false,
    "canCheckIn": false,
    "canUpdateProgress": true,
    "canResolve": true,
    "canEscalate": true,
    "slaDeadline": "2026-07-18T00:00:00Z"
  }
}
```

### 3.4 Chấp nhận task (Leader only)

```http
PUT /v1/teams/my-tasks/{reportId}/accept
```

### 3.5 Từ chối task (Leader only, 24h window) — BR-CLN-007

```http
PUT /v1/teams/my-tasks/{reportId}/decline
{ "teamId": "<guid>", "reason": "Thiếu trang thiết bị xử lý chất thải..." }
```

> **Lưu ý:** `reason` ≥ 20 ký tự. Chỉ được từ chối trong **24 giờ** kể từ lúc được gán.

### 3.6 ⭐ Check-in hiện trường (Leader only, PostGIS) — BR-CLN-002/003 [MỚI]

```http
POST /v1/teams/my-tasks/{reportId}/check-in
{
  "teamId": "<guid>",
  "latitude": 10.7621,
  "longitude": 106.6603,
  "note": "Đã đến hiện trường, bắt đầu dọn dẹp"
}
```

> **Validation:**
>
> - GPS phải ≤ **200 mét** so với tọa độ báo cáo (PostGIS `ST_Distance`).
> - Assignment phải ở status `Assigned` hoặc `InProgress`.
> - Chuyển assignment → `InProgress`, ghi nhận GPS + timestamp.
> - Nếu `note` được cung cấp, bypass distance check (lý do hợp lệ).

**Errors:**

- `422` `CLEANUP.TOO_FAR_FROM_REPORT` — Quá xa vị trí báo cáo (> 200m).
- `422` `CLEANUP.ASSIGNMENT_WRONG_STATUS` — Assignment không ở trạng thái Assigned.

### 3.7 ⭐ Cập nhật tiến độ (Leader only) — BR-CLN-004 [MỚI]

```http
PUT /v1/teams/my-tasks/{reportId}/progress
{
  "teamId": "<guid>",
  "percent": 60,
  "note": "Đã dọn được 60%, còn khu vực phía sau"
}
```

> **Validation:**
>
> - `percent`: 0–100.
> - Assignment phải ở status `InProgress`.
> - **Phải update ≥ 1 lần/ngày** khi InProgress.
> - Nếu > 24h không update → hệ thống cảnh báo. > 48h → escalate LEO.

### 3.8 ⭐ Escalate lên LEO (Leader only) — BR-CLN-006 [MỚI]

```http
POST /v1/teams/my-tasks/{reportId}/escalate
{
  "teamId": "<guid>",
  "reason": "Vượt khả năng xử lý, cần đội chuyên trách..."
}
```

> **Validation:**
>
> - `reason` ≥ 20 ký tự.
> - Assignment phải ở status `InProgress`.
> - Chuyển assignment → `Escalated`.
> - Nếu **tất cả** team đều escalate → Report quay về `Verified` để LEO phân công lại.

### 3.9 Hoàn thành (Resolve) — BR-CLN-005

```http
PUT /v1/reports/{reportId}/resolve
Authorization: Bearer <token>
```

> **Trước khi gọi**, phải upload:
>
> - ≥ 1 ảnh **before** (`POST /v1/reports/{reportId}/before-images`)
> - ≥ 2 ảnh **after** (`POST /v1/reports/{reportId}/after-images`) — không áp dụng kiểm tra góc chụp.

### 3.10 Lịch sử tiến độ team (Leader only)

```http
GET /v1/teams/my-progress?page=1&pageSize=20&assignmentStatus=Completed
```

### 3.11 Invitation (nhận lời mời)

```http
GET  /v1/users/me/invitations              # Danh sách lời mời của tôi
PUT  /v1/users/me/invitations/{id}/accept   # Chấp nhận
PUT  /v1/users/me/invitations/{id}/decline  # Từ chối
```

---

## 4. CompanyStaff (Đội công ty)

> **Role JWT:** `CompanyStaff`  
> **Dùng chung API với Cleaner** qua `TeamsController`. Chỉ khác:
>
> - Assignment do **CompanyManager** phân công (không phải LEO trực tiếp).
> - Kiểm tra hiệu lực hợp đồng công ty (BR-CMP-005).

### API giống hệt Cleaner (§3)

| Endpoint                                      | Mô tả                       |
| --------------------------------------------- | --------------------------- |
| `GET /v1/teams/my-profile`                    | Profile team (company team) |
| `GET /v1/teams/my-tasks`                      | Danh sách task              |
| `GET /v1/teams/my-tasks/{reportId}`           | Chi tiết task               |
| `PUT /v1/teams/my-tasks/{reportId}/accept`    | Chấp nhận                   |
| `PUT /v1/teams/my-tasks/{reportId}/decline`   | Từ chối (24h)               |
| `POST /v1/teams/my-tasks/{reportId}/check-in` | ⭐ Check-in PostGIS         |
| `PUT /v1/teams/my-tasks/{reportId}/progress`  | ⭐ Cập nhật tiến độ         |
| `POST /v1/teams/my-tasks/{reportId}/escalate` | ⭐ Escalate                 |
| `PUT /v1/reports/{reportId}/resolve`          | Hoàn thành                  |
| `GET /v1/teams/my-progress`                   | Lịch sử tiến độ             |

> ⚠️ Nếu công ty bị **Suspended/Terminated/Expired**, tất cả API sẽ trả 403.

---

## 5. Inspector (Đội xử phạt)

> **Role JWT:** `Inspector`  
> **Ai:** Thành viên Đội xử phạt môi trường.  
> **Scope:** Chỉ thấy InspectionReport được gán cho team mình (BR-INS-002).

### 5.1 Danh sách hồ sơ xử phạt

```http
GET /v1/inspections/queue?page=1&pageSize=20&status=Draft
```

**status filter:** `Draft`, `InProgress`, `PenaltyIssued`, `Paid`, `PartiallyPaid`, `Overdue`, `Closed`, `ClosedNoViolation`

### 5.2 Chi tiết hồ sơ

```http
GET /v1/inspections/{id}
```

**Response bao gồm:**

```json
{
  "data": {
    "id": "...",
    "reportId": "...",
    "status": "Draft",
    "violationDescription": "...",
    "violatorName": "Công ty ABC",
    "violationLevel": "Moderate",
    "penaltyAmount": 50000000,
    "isRepeatOffender": false,
    "slaInspectionBreached": false,
    "slaInspectionDueAt": "2026-07-18T00:00:00Z",
    "checkedInAt": null,
    "progressPercent": 0,
    "assignedTeamId": "...",
    "assignedTeamName": "Đội xử phạt Q.1"
  }
}
```

### 5.3 ⭐ Từ chối hồ sơ (24h window) — BR-INS-003 [MỚI]

```http
POST /v1/inspections/{id}/decline
{ "reason": "Không thuộc phạm vi chuyên môn của đội..." }
```

> **Validation:**
>
> - `reason` ≥ 20 ký tự.
> - Chỉ được từ chối trong **24 giờ** kể từ lúc được gán.
> - Hồ sơ quay về `Draft` để LEO gán team khác.

### 5.4 ⭐ Check-in hiện trường (PostGIS) — BR-INS-004 [MỚI]

```http
POST /v1/inspections/{id}/check-in
{
  "latitude": 10.7621,
  "longitude": 106.6603,
  "note": "Đã đến nhà máy, bắt đầu kiểm tra"
}
```

> **Validation:**
>
> - GPS ≤ **200 mét** so với tọa độ báo cáo liên kết.
> - Chuyển hồ sơ: `Draft` → `InProgress`.
> - Nếu có `note`, bypass distance check.

### 5.5 Cập nhật biên bản — BR-INS-010

```http
PUT /v1/inspections/{id}/details
{
  "violationDescription": "Xả nước thải chưa qua xử lý ra sông...",
  "violatorName": "Công ty TNHH ABC",
  "violatorAddress": "123 Nguyễn Trãi, Q.1",
  "violatorIdentity": "MST: 0301234567"
}
```

> Cho phép khi status là `Draft` hoặc `InProgress`.

### 5.6 ⭐ Cập nhật tiến độ — BR-INS-031 [MỚI]

```http
PUT /v1/inspections/{id}/progress
{
  "percent": 50,
  "note": "Đã lấy mẫu nước, đang chờ kết quả phân tích"
}
```

> **Validation:**
>
> - `percent`: 0–100.
> - Status phải là `InProgress`.
> - **Phải update ≥ 1 lần/ngày.**

### 5.7 Ban hành QĐ xử phạt (Team Leader only) — BR-INS-012

```http
PUT /v1/inspections/{id}/issue-penalty
{
  "violationLevel": "Moderate",
  "penaltyAmount": 50000000,
  "decisionNumber": "QĐ-XP-2026-001",
  "paymentDueDays": 10,
  "additionalMeasures": "Đình chỉ hoạt động 3 tháng"
}
```

> - `violationLevel`: `Minor`, `Moderate`, `Severe`, `Critical`
> - `penaltyAmount` phải nằm trong khung (BR-ADM-008).
> - Hệ thống auto-check tái phạm (BR-INS-022): ≥ 2 biên bản trong 12 tháng → flag `isRepeatOffender`.
> - Chuyển: `Draft`/`InProgress` → `PenaltyIssued`.

### 5.8 Đóng hồ sơ — không đủ căn cứ (BR-INS-013)

```http
PUT /v1/inspections/{id}/close-no-violation
{ "reason": "Đã điều tra hiện trường, không tìm thấy căn cứ vi phạm..." }
```

> `reason` ≥ 50 ký tự.

### 5.9 Ghi nhận nộp phạt — BR-INS-020

```http
PUT /v1/inspections/{id}/record-payment
{ "paidAmount": 50000000 }
```

> Hệ thống tự tính: `Paid` (đã nộp đủ) vs `PartiallyPaid`.

### 5.10 Đóng hồ sơ sau nộp phạt

```http
PUT /v1/inspections/{id}/close
{ "reason": "Đã nộp phạt đầy đủ, kết thúc hồ sơ" }
```

### 5.11 ⭐ KPI Inspection Team — BR-INS-032 [MỚI]

```http
GET /v1/inspections/kpi?period=ThisMonth
GET /v1/inspections/kpi?from=2026-01-01&to=2026-06-30
```

**Query params:**

- `teamId` (optional — Inspector xem team mình, LEO/Admin chỉ định)
- `period`: `ThisMonth`, `LastMonth`, `ThisQuarter`, `LastQuarter`, `ThisYear`, `LastYear`
- `from`, `to`: custom date range (ưu tiên hơn period)

**Response:**

```json
{
  "data": {
    "teamId": "...",
    "teamName": "Đội xử phạt Q.1",
    "periodFrom": "2026-07-01",
    "periodTo": "2026-07-11",
    "totalInspections": 15,
    "penaltyIssuedCount": 10,
    "penaltyIssuedOnTime": 8,
    "penaltyIssuedOnTimePercent": 80.0,
    "closedNoViolationCount": 3,
    "totalPaid": 7,
    "paidOnTime": 6,
    "paidOnTimePercent": 85.7,
    "repeatOffenderCount": 2,
    "slaBreach": 1
  }
}
```

---

## 6. Shared Endpoints (tất cả roles)

| Method | Endpoint                         | Mô tả                             |
| ------ | -------------------------------- | --------------------------------- |
| `GET`  | `/v1/categories`                 | Danh mục ô nhiễm (3 loại)         |
| `GET`  | `/v1/map/reports`                | Bản đồ công khai (viewport query) |
| `GET`  | `/v1/notifications`              | Thông báo                         |
| `PUT`  | `/v1/notifications/{id}/read`    | Đánh dấu đã đọc                   |
| `PUT`  | `/v1/notifications/read-all`     | Đánh dấu tất cả                   |
| `GET`  | `/v1/notifications/preferences`  | Cài đặt thông báo                 |
| `PUT`  | `/v1/notifications/preferences`  | Cập nhật cài đặt                  |
| `PUT`  | `/v1/notifications/device-token` | Cập nhật FCM token                |
| `GET`  | `/v1/users/me`                   | Profile cá nhân                   |
| `PUT`  | `/v1/users/me`                   | Cập nhật profile                  |
| `POST` | `/v1/users/me/consent`           | Đồng ý điều khoản                 |
| `GET`  | `/v1/gamification/my-points`     | Điểm gamification                 |
| `GET`  | `/v1/gamification/my-badges`     | Huy hiệu                          |
| `GET`  | `/v1/gamification/leaderboard`   | Bảng xếp hạng                     |

---

## 7. Enums & Constants

### ReportStatus (Citizen sees)

| Value        | Hiển thị                     |
| ------------ | ---------------------------- |
| `Submitted`  | Đã gửi — chờ xác minh        |
| `Verified`   | Đã xác minh — đang phân công |
| `InProgress` | Đang xử lý                   |
| `Resolved`   | Đã xử lý — chờ xác nhận      |
| `Closed`     | Đã đóng                      |
| `Rejected`   | Bị từ chối                   |

### AssignmentStatus (Cleaner/CompanyStaff sees)

| Value        | Mô tả                        |
| ------------ | ---------------------------- |
| `Assigned`   | Đã phân công — chờ chấp nhận |
| `InProgress` | Đang thực hiện               |
| `Completed`  | Hoàn thành                   |
| `Declined`   | Đã từ chối                   |
| `Escalated`  | ⭐ Đã escalate lên LEO       |

### InspectionStatus (Inspector sees)

| Value               | Mô tả                           |
| ------------------- | ------------------------------- |
| `Draft`             | Mới tạo — chờ điều tra          |
| `InProgress`        | ⭐ Đang điều tra (sau check-in) |
| `PenaltyIssued`     | Đã ban hành QĐ xử phạt          |
| `Paid`              | Đã nộp phạt đầy đủ              |
| `PartiallyPaid`     | Nộp phạt một phần               |
| `Overdue`           | Quá hạn nộp phạt                |
| `Closed`            | Đã đóng                         |
| `ClosedNoViolation` | Đóng — không đủ căn cứ          |

### ViolationLevel

| Value      | Hiển thị              |
| ---------- | --------------------- |
| `Minor`    | Nhẹ (cảnh cáo)        |
| `Moderate` | Trung bình            |
| `Severe`   | Nặng                  |
| `Critical` | Đặc biệt nghiêm trọng |

### Severity (khi submit report)

| Value      | Hiển thị     |
| ---------- | ------------ |
| `Low`      | Thấp         |
| `Medium`   | Trung bình   |
| `High`     | Cao          |
| `Critical` | Nghiêm trọng |

### KpiPeriod

| Value         | Mô tả       |
| ------------- | ----------- |
| `ThisMonth`   | Tháng này   |
| `LastMonth`   | Tháng trước |
| `ThisQuarter` | Quý này     |
| `LastQuarter` | Quý trước   |
| `ThisYear`    | Năm nay     |
| `LastYear`    | Năm trước   |

---

## 8. Error Codes quan trọng

### Auth

| Code                        | HTTP | Mô tả              |
| --------------------------- | ---- | ------------------ |
| `AUTH.EMAIL_ALREADY_EXISTS` | 409  | Email đã tồn tại   |
| `AUTH.PHONE_ALREADY_EXISTS` | 409  | SĐT đã tồn tại     |
| `AUTH.INVALID_CREDENTIALS`  | 401  | Sai email/mật khẩu |
| `AUTH.ACCOUNT_BANNED`       | 403  | Tài khoản bị khóa  |
| `AUTH.COMPANY_EXPIRED`      | 403  | Công ty hết hạn HĐ |

### Reports

| Code                            | HTTP | Mô tả                      |
| ------------------------------- | ---- | -------------------------- |
| `REPORTS.NOT_FOUND`             | 404  | Không tìm thấy báo cáo     |
| `REPORTS.CANNOT_DELETE`         | 422  | Chỉ xóa được khi Submitted |
| `REPORTS.REOPEN_LIMIT`          | 422  | Đã mở lại tối đa 2 lần     |
| `REPORTS.REOPEN_WINDOW_EXPIRED` | 422  | Quá 7 ngày                 |
| `REPORTS.ASSIGNMENT_NOT_FOUND`  | 404  | Không tìm thấy assignment  |

### Cleanup [MỚI]

| Code                              | HTTP | Mô tả                  |
| --------------------------------- | ---- | ---------------------- |
| `CLEANUP.TOO_FAR_FROM_REPORT`     | 422  | Quá xa (> 200m)        |
| `CLEANUP.ASSIGNMENT_WRONG_STATUS` | 422  | Status không phù hợp   |
| `CLEANUP.NOT_TEAM_LEADER`         | 403  | Không phải team leader |

### Inspection [MỚI]

| Code                                    | HTTP | Mô tả                          |
| --------------------------------------- | ---- | ------------------------------ |
| `INSPECTIONS.NOT_FOUND`                 | 404  | Không tìm thấy hồ sơ           |
| `INSPECTIONS.INVALID_STATUS_TRANSITION` | 422  | Chuyển trạng thái không hợp lệ |
| `INSPECTIONS.DECLINE_WINDOW_EXPIRED`    | 422  | Quá 24h để từ chối             |
| `INSPECTIONS.TOO_FAR_FROM_REPORT`       | 422  | Quá xa (> 200m)                |
| `INSPECTIONS.NOT_ASSIGNED_TO_YOUR_TEAM` | 403  | Không thuộc team của bạn       |
| `INSPECTIONS.TEAM_NOT_FOUND`            | 404  | Team không tồn tại             |

---

## Luồng tổng quát (cho FE hiểu)

### Citizen Flow

```
Register → Login → Submit Report (+ ảnh/video) → Track Status → Rate/Confirm/Reopen
```

### Cleaner/CompanyStaff Flow

```
Login → View Tasks → Accept → Check-in (GPS ≤ 200m)
      → Update Progress (≥ 1/ngày) → Upload After Images → Resolve
                                     ↓ (nếu vượt khả năng)
                                     Escalate → LEO phân công lại
```

### Inspector Flow

```
Login → View Queue → [Accept or Decline 24h]
      → Check-in (GPS ≤ 200m) → InProgress
      → Update Details (biên bản) → Update Progress
      → Issue Penalty (QĐ xử phạt) or Close No Violation
      → Record Payment → Close
```

---

> **Tham chiếu:**
>
> - [MOBILE_AUTH_INTEGRATION.md](./MOBILE_AUTH_INTEGRATION.md)
> - [REPORT_LIFECYCLE.md](./REPORT_LIFECYCLE.md)
> - [fe-team-workflow-guide.md](./fe-team-workflow-guide.md)
> - [fe-inspection-api-guide.md](./fe-inspection-api-guide.md)
> - [fe-company-staff-api-guide.md](./fe-company-staff-api-guide.md)
