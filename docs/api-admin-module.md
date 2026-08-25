# Administration Module — API Reference

> **Base URL:** `/v1/admin`
> **Authorization:** `Bearer <JWT>` — Role = `Admin`
> **Content-Type:** `application/json`
> **Wrapper:** Mọi response đều wrap trong `ApiResponse<T>`:
> ```json
> { "success": true, "message": "...", "data": { ... } }
> ```

---

## Mục lục

| # | Endpoint | Method | Mô tả |
|---|---|---|---|
| 1 | `/penalty-frameworks` | GET | Danh sách khung tiền phạt |
| 2 | `/penalty-frameworks` | POST | Tạo khung tiền phạt |
| 3 | `/penalty-frameworks/{id}` | PUT | Cập nhật khung tiền phạt |
| 4 | `/penalty-frameworks/{id}/toggle` | PATCH | Bật/tắt khung tiền phạt |
| 5 | `/audit-logs` | GET | Danh sách audit log |
| 6 | `/audit-logs/export` | GET | Export audit log CSV |
| 7 | `/audit-logs/stats` | GET | Thống kê audit log |
| 8 | `/audit-logs/{id}` | GET | Chi tiết audit log |
| 9 | `/reports/{id}/hide` | POST | Ẩn báo cáo vi phạm |
| 10 | `/reports/{id}/unhide` | POST | Hiện lại báo cáo |
| 11 | `/spam-suspects` | GET | Spam dashboard |
| 10 | `/gamification-configs` | GET | Cấu hình điểm gamification |
| 11 | `/gamification-configs/{id}` | PUT | Cập nhật điểm |
| 12 | `/notification-templates` | GET | Danh sách template thông báo |
| 13 | `/notification-templates` | POST | Tạo template thông báo |
| 14 | `/notification-templates/{id}/publish` | PATCH | Publish / Unpublish template |
| 15 | `/notification-templates/{id}/test` | POST | Test gửi template |
| 16 | `/blocked-words` | GET | Danh sách từ bị chặn (profanity) |
| 17 | `/blocked-words` | POST | Thêm từ bị chặn |
| 18 | `/blocked-words/{id}` | PUT | Cập nhật từ bị chặn |
| 19 | `/blocked-words/{id}` | DELETE | Vô hiệu hóa từ bị chặn |

> Chi tiết mục 16–19: [`api-admin-blocked-words.md`](./api-admin-blocked-words.md)

---

## 1. Penalty Framework — Khung mức phạt

> **Business Rule:** BR-ADM-008 — Admin quản lý khung mức tiền phạt theo từng loại ô nhiễm + cấp vi phạm. Unique constraint: chỉ 1 active entry cho mỗi cặp `(CategoryId, ViolationLevel)`.

### Enum `ViolationLevel`

| Value | Mô tả |
|---|---|
| `Minor` | Nhẹ — cảnh cáo |
| `Moderate` | Trung bình |
| `Severe` | Nặng |
| `Critical` | Đặc biệt nghiêm trọng |

---

### 1.1. `GET /v1/admin/penalty-frameworks`

Danh sách khung mức phạt có phân trang, hỗ trợ lọc theo category, violation level, trạng thái active.

**Query Parameters:**

| Param | Type | Default | Mô tả |
|---|---|---|---|
| `page` | int | 1 | Trang hiện tại |
| `pageSize` | int | 20 | Số bản ghi/trang |
| `categoryId` | Guid? | — | Lọc theo loại ô nhiễm |
| `violationLevel` | string? | — | Lọc theo cấp vi phạm: `Minor`, `Moderate`, `Severe`, `Critical` |
| `isActive` | bool? | — | Lọc theo trạng thái hoạt động |

**Response `200 OK`:**

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "categoryId": "7a1b2c3d-...",
        "categoryNameVi": "Rác thải sinh hoạt",
        "violationLevel": "Moderate",
        "minAmount": 5000000,
        "maxAmount": 20000000,
        "currency": "VND",
        "effectiveFrom": "2026-01-01T00:00:00Z",
        "effectiveTo": null,
        "isActive": true,
        "createdAt": "2026-07-10T08:00:00Z"
      }
    ],
    "pagination": {
      "currentPage": 1,
      "pageSize": 20,
      "totalCount": 12,
      "totalPages": 1
    }
  }
}
```

**cURL:**

```bash
curl -X GET "https://api.greenlens.vn/v1/admin/penalty-frameworks?page=1&pageSize=10&isActive=true" \
  -H "Authorization: Bearer <token>"
```

---

### 1.2. `POST /v1/admin/penalty-frameworks`

Tạo khung mức phạt mới. `MinAmount ≤ MaxAmount`. Không tạo được nếu đã tồn tại active entry cho cùng category + level.

**Request Body:**

```json
{
  "categoryId": "7a1b2c3d-4e5f-6789-abcd-ef0123456789",
  "violationLevel": "Moderate",
  "minAmount": 5000000,
  "maxAmount": 20000000,
  "effectiveFrom": "2026-01-01T00:00:00Z",
  "effectiveTo": null
}
```

| Field | Type | Required | Validation |
|---|---|---|---|
| `categoryId` | Guid | ✅ | Phải tồn tại trong bảng `pollution_categories` |
| `violationLevel` | string | ✅ | Enum: `Minor`, `Moderate`, `Severe`, `Critical` |
| `minAmount` | decimal | ✅ | `> 0`, `≤ maxAmount` |
| `maxAmount` | decimal | ✅ | `> 0`, `≥ minAmount` |
| `effectiveFrom` | DateTime | ✅ | Ngày bắt đầu hiệu lực |
| `effectiveTo` | DateTime? | ❌ | Null = không giới hạn |

**Response `201 Created`:**

```json
{
  "success": true,
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "categoryId": "7a1b2c3d-...",
    "violationLevel": "Moderate",
    "minAmount": 5000000,
    "maxAmount": 20000000,
    "effectiveFrom": "2026-01-01T00:00:00Z"
  }
}
```

**Error `409 Conflict`** — Đã tồn tại active entry cho category + level này.

---

### 1.3. `PUT /v1/admin/penalty-frameworks/{id}`

Cập nhật mức min/max và ngày hiệu lực. Không ảnh hưởng quyết định đã ban hành trước đó.

**Path:** `id` — Guid của PenaltyFramework

**Request Body:**

```json
{
  "minAmount": 7000000,
  "maxAmount": 25000000,
  "effectiveFrom": "2026-07-01T00:00:00Z",
  "effectiveTo": "2027-01-01T00:00:00Z"
}
```

**Response `204 No Content`** (wrapped):

```json
{ "success": true, "message": "Đã cập nhật khung tiền phạt." }
```

**Error `404 Not Found`** — PenaltyFramework không tồn tại.

---

### 1.4. `PATCH /v1/admin/penalty-frameworks/{id}/toggle`

Bật hoặc tắt khung phạt. Khung bị tắt sẽ không được sử dụng cho quyết định phạt mới.

**Request Body:**

```json
{ "activate": true }
```

| Field | Type | Required | Mô tả |
|---|---|---|---|
| `activate` | bool | ✅ | `true` = kích hoạt, `false` = vô hiệu hóa |

**Response `204 No Content`** (wrapped):

```json
{ "success": true, "message": "Đã thay đổi trạng thái khung phạt." }
```

---

## 2. Audit Log — Nhật ký hành động

> **Business Rule:** BR-ADM-010 — Ghi nhận mọi hành động nhạy cảm (đổi role, ban user, force update status, suspend/terminate company…). Immutable — không chỉnh sửa hoặc xóa được.
>
> Tự động ghi log khi Command implement `IAuditable` được xử lý thành công (qua `AuditLogBehavior`), hoặc ghi thủ công trong handler (Create*, BlockedWords, Officer/Inspection workflow, Admin Update Phase 3).

### Commands được audit (phase hiện tại)

| Command / Action | Entity Type | Cơ chế |
|---|---|---|
| `CreateAccount` | User | Manual `IAuditLogger` |
| `UpdateUser` | User | Manual (old/new snapshot) |
| `DeleteUser` | User | `IAuditable` |
| `UpdateUserRole` | User | `IAuditable` |
| `ToggleBanUser` | User | `IAuditable` |
| `ForceUpdateReportStatus` | Report | `IAuditable` |
| `HideReport` / `UnhideReport` | Report | `IAuditable` |
| `VerifyReport` / `RejectReport` | Report | Manual (status transition) |
| `AssignTeam` / `ReassignTeam` / `EscalateReport` | Report | Manual |
| `DispatchToCompany` / `AssignCompanyTeam` | Report | Manual |
| `ConfirmDuplicate` / `DismissDuplicate` | Report | Manual |
| `ApproveReopenRequest` / `RejectReopenRequest` | Report | Manual |
| `DeleteReport` | Report | Manual |
| `CreateInspectionReport` … `DeclineInspection` | InspectionReport | Manual |
| `RecordPayment` | InspectionReport | Manual |
| `DeletePenaltyPayment` | PenaltyPayment | Manual |
| `DeleteViolatingEntity` | ViolatingEntity | Manual |
| `CreateCategory` … `ArchiveCategory` | PollutionCategory | Manual / `IAuditable` |
| `UpdateCategory` / `UpdateWasteTag` | PollutionCategory / WasteTag | Manual (old/new) |
| `CreateWasteTag` … `DeleteWasteTag` | WasteTag | Manual / `IAuditable` |
| `CreatePenaltyFramework` … `DeactivatePenaltyFramework` | PenaltyFramework | Manual / `IAuditable` |
| `UpdatePenaltyFramework` | PenaltyFramework | Manual (old/new) |
| `CreateNotificationTemplate` … `DeleteNotificationTemplate` | NotificationTemplate | Manual / `IAuditable` |
| `UpdateNotificationTemplate` | NotificationTemplate | Manual (old/new) |
| `UpdateGamificationConfig` | GamificationConfig | `IAuditable` |
| `BlockedWord.Create/Update/Delete` | BlockedWord | Manual (có oldValues) |
| `CreateCompany`, `RenewContract`, `ReactivateCompany`, … | Company | Manual / `IAuditable` |
| `UpdateCompanyServiceAreas` | Company | Manual (old/new) |
| `SuspendCompany` / `TerminateCompany` | Company | Manual (old/new) |
| `ResetCompanyManagerPassword` | User | `IAuditable` |
| `IssuePenalty` | InspectionReport | `IAuditable` |

### Admin user ban

`PUT /v1/admin/users/{id}/ban` — toggle `IsBanned`, ghi audit `ToggleBanUser`.

---

### 2.1. `GET /v1/admin/audit-logs`

Danh sách audit log phân trang, lọc theo user, entity type, entity id, action, khoảng thời gian.

**Query Parameters:**

| Param | Type | Default | Mô tả |
|---|---|---|---|
| `page` | int | 1 | Trang hiện tại |
| `pageSize` | int | 20 | Số bản ghi/trang (max **100**) |
| `userId` | Guid? | — | Lọc theo ID người thực hiện |
| `actorRole` | UserRole? | — | Lọc theo role người thực hiện (`Admin`, `LEO`, `DEO`, …) |
| `entityType` | string? | — | Lọc theo loại entity (`User`, `Report`, `Company`…) |
| `entityId` | string? | — | Lọc theo ID entity đích (exact match) |
| `action` | string? | — | Lọc theo tên action (`UpdateUserRole`…) |
| `fromDate` | DateTime? | — | Từ ngày |
| `toDate` | DateTime? | — | Đến ngày |

**Response `200 OK`:**

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "...",
        "userId": "admin-user-guid",
        "userEmail": "admin@greenlens.vn",
        "actorRole": "Admin",
        "action": "UpdateUserRole",
        "entityType": "User",
        "entityId": "target-user-guid",
        "ipAddress": "103.1.2.3",
        "userAgent": "Mozilla/5.0 ...",
        "createdAt": "2026-07-10T09:30:00Z"
      }
    ],
    "pagination": {
      "currentPage": 1,
      "pageSize": 20,
      "totalCount": 156,
      "totalPages": 8
    }
  }
}
```

**cURL:**

```bash
curl -X GET "https://api.greenlens.vn/v1/admin/audit-logs?entityType=User&fromDate=2026-07-01" \
  -H "Authorization: Bearer <token>"
```

---

### 2.2. `GET /v1/admin/audit-logs/{id}`

Chi tiết 1 bản ghi audit, bao gồm `OldValues` và `NewValues` dạng JSON.

**Path:** `id` — Guid của AuditLog

**Response `200 OK`:**

```json
{
  "success": true,
  "data": {
    "id": "...",
    "userId": "admin-user-guid",
    "userEmail": "admin@greenlens.vn",
    "actorRole": "Admin",
    "action": "ToggleBanUser",
    "entityType": "User",
    "entityId": "banned-user-guid",
    "oldValues": "{\"IsBanned\": false}",
    "newValues": "{\"IsBanned\": true, \"Reason\": \"Spam reports\"}",
    "ipAddress": "103.1.2.3",
    "userAgent": "Mozilla/5.0 ...",
    "createdAt": "2026-07-10T09:30:00Z"
  }
}
```

**Error `404 Not Found`** — AuditLog không tồn tại.

---

### 2.3. `GET /v1/admin/audit-logs/export`

Export CSV audit log trong khoảng thời gian. **Bắt buộc** `fromDate` và `toDate` (UTC). Tối đa **90 ngày**.

**Query Parameters:** `fromDate`, `toDate` (required); `userId`, `actorRole`, `entityType`, `action` (optional filters).

**Response `200 OK`:** File CSV (`text/csv`). Cột: `Id`, `UserId`, `Action`, `EntityType`, `EntityId`, `IpAddress`, `CreatedAtUtc` — không export email/GPS/oldValues.

**Error `400 Bad Request`:** Thiếu date hoặc range > 90 ngày.

---

### 2.4. `GET /v1/admin/audit-logs/stats`

Thống kê audit log cho dashboard.

**Query Parameters:** `fromDate`, `toDate` (required, max 90 ngày).

**Response `200 OK`:**

```json
{
  "success": true,
  "data": {
    "totalCount": 420,
    "byAction": [{ "action": "VerifyReport", "count": 85 }],
    "byDay": [{ "date": "2026-07-28", "count": 45 }]
  }
}
```

---

## 3. Content Moderation — Kiểm duyệt nội dung

> **Business Rule:** BR-ADM-006 — Admin có thể ẩn báo cáo vi phạm khỏi công chúng (reversible soft-hide ≠ soft-delete).
>
> **Side effects:** Các public query tự động filter `IsHidden`:
> - `GET /v1/reports` — ẩn khỏi danh sách
> - `GET /v1/reports/{id}` — trả 404 nếu bị ẩn (trừ admin view)
> - `GET /v1/map/reports` — ẩn khỏi map pins
> - `GET /v1/map/summary` — không tính vào thống kê

---

### 3.1. `POST /v1/admin/reports/{id}/hide`

Ẩn báo cáo khỏi công chúng. Đánh dấu `IsHidden = true`, lưu lý do và thời gian. Hành động được audit log.

**Path:** `id` — Guid của Report

**Request Body:**

```json
{
  "reason": "Nội dung xúc phạm, không liên quan đến ô nhiễm môi trường."
}
```

| Field | Type | Required | Validation |
|---|---|---|---|
| `reason` | string | ✅ | Tối thiểu 10 ký tự, tối đa 500 ký tự |

**Response `204 No Content`** (wrapped):

```json
{ "success": true, "message": "Đã ẩn báo cáo." }
```

**Error `404 Not Found`** — Report không tồn tại.

**cURL:**

```bash
curl -X POST "https://api.greenlens.vn/v1/admin/reports/3fa85f64-.../hide" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"reason": "Nội dung vi phạm quy định cộng đồng."}'
```

---

### 3.2. `POST /v1/admin/reports/{id}/unhide`

Bỏ ẩn — hiện lại báo cáo cho công chúng. Đặt `IsHidden = false`. Hành động được audit log.

**Path:** `id` — Guid của Report

**Request Body:** Không có.

**Response `204 No Content`** (wrapped):

```json
{ "success": true, "message": "Đã hiện lại báo cáo." }
```

**Error `404 Not Found`** — Report không tồn tại.

---

## 4. Spam Dashboard — Bảng theo dõi spam

> **Business Rule:** BR-ADM-007 — Dashboard hiển thị tài khoản nghi spam dựa trên heuristic rules.
>
> **Heuristic rules (configurable qua query params):**
> 1. Submit ≥ ngưỡng/giờ (`minReportsPerHour`, hoặc `submit_max_per_hour` từ system settings khi param bỏ trống — seed mặc định **5**)
> 2. ≥ 3 báo cáo bị rejected trong 7 ngày gần nhất
> 3. ≥ 2 báo cáo bị AI flag là `IrrelevantOrSuspectedAbusive`

> **Lưu ý (PR-9):** `minReportsPerHour` **không còn** hardcode default `5` ở API layer. Admin đổi `submit_max_per_hour` qua `PATCH /v1/admin/system-settings/rate_limits` → spam dashboard (khi không truyền param) dùng ngưỡng mới ngay.

---

### 4.1. `GET /v1/admin/spam-suspects`

Trả về danh sách tài khoản nghi spam.

**Query Parameters:**

| Param | Type | Default | Mô tả |
|---|---|---|---|
| `page` | int | 1 | Trang hiện tại |
| `pageSize` | int | 20 | Số bản ghi/trang |
| `minReportsPerHour` | int? | *(null)* | Ngưỡng submit/giờ. **Bỏ trống** → lấy `submit_max_per_hour` từ system settings (seed **5**). Truyền explicit vẫn override one-shot. |
| `minRejected7Days` | int | 3 | Ngưỡng rejected trong 7 ngày |
| `minAiFlagged` | int | 2 | Ngưỡng AI flagged |

**Response `200 OK`:**

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "userId": "suspect-user-guid",
        "fullName": "Nguyễn Văn A",
        "email": "user@example.com",
        "isBanned": false,
        "reportsLastHour": 7,
        "rejectedLast7Days": 4,
        "aiFlaggedCount": 3,
        "suspectReasons": "HighVolume, FrequentRejection, AiFlagged"
      }
    ],
    "totalCount": 3
  }
}
```

**`suspectReasons`** — Chuỗi phân tách bằng `, ` liệt kê lý do:

| Reason | Mô tả |
|---|---|
| `HighVolume` | Submit ≥ ngưỡng/giờ |
| `FrequentRejection` | Rejected ≥ ngưỡng/7 ngày |
| `AiFlagged` | AI flagged ≥ ngưỡng |

**cURL:**

```bash
# Dùng ngưỡng từ system settings (submit_max_per_hour)
curl -X GET "https://api.greenlens.vn/v1/admin/spam-suspects?page=1&pageSize=20" \
  -H "Authorization: Bearer <token>"

# Override one-shot ngưỡng submit/giờ
curl -X GET "https://api.greenlens.vn/v1/admin/spam-suspects?minReportsPerHour=3&minRejected7Days=2" \
  -H "Authorization: Bearer <token>"
```

---

## 5. Gamification Config — Cấu hình điểm thưởng/phạt

> **Business Rule:** BR-ADM-005 — Admin chỉnh số điểm cho mỗi hành động gamification. Có thể bật/tắt từng hành động.
>
> **Seed data mặc định:**
>
> | ActionType | Points | Mô tả |
> |---|---|---|
> | `ReportVerified` | +10 | Báo cáo được xác minh |
> | `ReportResolved` | +20 | Báo cáo được xử lý xong |
> | `PenaltyIssued` | +20 | Quyết định phạt được ban hành |
> | `DuplicateReport` | +5 | Báo cáo trùng được merge |
> | `ReportRejected` | −5 | Báo cáo bị từ chối |
> | `FraudPenalty` | −100 | Vi phạm gian lận |

---

### 5.1. `GET /v1/admin/gamification-configs`

Danh sách tất cả cấu hình điểm (không phân trang — số lượng nhỏ, hiện tại 6 bản ghi).

**Response `200 OK`:**

```json
{
  "success": true,
  "data": [
    {
      "id": "config-guid-1",
      "actionType": "ReportVerified",
      "points": 10,
      "description": "Báo cáo được xác minh bởi LEO",
      "isActive": true,
      "createdAt": "2026-07-10T08:00:00Z",
      "updatedAt": null
    },
    {
      "id": "config-guid-2",
      "actionType": "FraudPenalty",
      "points": -100,
      "description": "Vi phạm gian lận — trừ toàn bộ điểm batch",
      "isActive": true,
      "createdAt": "2026-07-10T08:00:00Z",
      "updatedAt": null
    }
  ]
}
```

---

### 5.2. `PUT /v1/admin/gamification-configs/{id}`

Cập nhật điểm, mô tả, và trạng thái bật/tắt cho 1 hành động. Hành động được audit log.

**Path:** `id` — Guid của GamificationConfig

**Request Body:**

```json
{
  "points": 15,
  "description": "Tăng thưởng xác minh báo cáo lên 15 điểm",
  "isActive": true
}
```

| Field | Type | Required | Mô tả |
|---|---|---|---|
| `points` | int | ✅ | Số điểm (dương = thưởng, âm = phạt) |
| `description` | string | ✅ | Mô tả hành động |
| `isActive` | bool | ✅ | `false` = tạm tắt, event handler sẽ skip |

**Response `204 No Content`** (wrapped):

```json
{ "success": true, "message": "Đã cập nhật cấu hình điểm." }
```

**Error `404 Not Found`** — GamificationConfig không tồn tại.

---

## 5.3. Badges — Ngưỡng huy hiệu (BR-ADM-005, BR-GAM-004)

> **Lưu ý:** Ngưỡng badge **không** nằm trong `system_settings`. Lưu trên bảng `badges` (`required_points`, `required_report_count`, `required_streak_days`, `required_action_count`). Admin gửi **một số `threshold`** duy nhất — backend map theo `code` của badge.

### Mapping ngưỡng theo loại badge

| Nhóm | `code` | Trục ngưỡng | Cột DB được cập nhật |
|------|--------|-------------|----------------------|
| Milestone | `first_report`, `eco_warrior`, … | Số báo cáo đã verify | `requiredReportCount` |
| Streak | `streak_7d`, `streak_30d` | Số ngày streak | `requiredStreakDays` |
| Level | `rising_star`, `eco_expert`, `green_legend` | Tổng điểm | `requiredPoints` |
| Community | `duplicate_finder`, `community_voice`, `cleanup_hero` | Action count | `requiredActionCount` |

### 5.3.1. `GET /v1/admin/badges`

Danh sách badge (phân trang). Response mỗi item có 4 cột nullable — **chỉ một cột có giá trị** tương ứng loại badge.

### 5.3.2. `PATCH /v1/admin/badges/{id}/thresholds`

Cập nhật ngưỡng eligibility. Chỉ ảnh hưởng user **chưa** được cấp badge; badge đã earn giữ nguyên.

**Request:**

```json
{ "threshold": 15 }
```

| Field | Type | Range | Mô tả |
|-------|------|-------|--------|
| `threshold` | int | 1 – 1_000_000 | Ngưỡng mới |

**Response `204`** — `"Đã cập nhật ngưỡng huy hiệu."`

**Ví dụ:** `eco_warrior` mặc định 10 → PATCH `{ "threshold": 15 }` → cần 15 báo cáo verified mới nhận badge.

**cURL:**

```bash
curl -X PATCH "https://api.greenlens.vn/v1/admin/badges/{badgeId}/thresholds" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{ "threshold": 15 }'
```

### 5.3.3. Các endpoint badge khác

| Method | Path | Mô tả |
|--------|------|--------|
| `PUT` | `/v1/admin/badges/{id}` | Sửa tên/mô tả/icon (không đổi ngưỡng) |
| `PATCH` | `/v1/admin/badges/{id}/toggle` | Bật/tắt badge |

---

## 6. Notification Templates — Quản lý template thông báo

> **Business Rule:** BR-ADM-004 — Admin quản lý template thông báo với placeholder. Template phải được publish trước khi hệ thống sử dụng. Admin có thể test gửi thử trước khi publish.

### Enum `NotificationChannel`

| Value | Mô tả |
|---|---|
| `Push` | Push notification (FCM) |
| `Email` | Email |
| `Both` | Cả push lẫn email |

### Enum `NotificationType`

| Value | Mô tả |
|---|---|
| `ReportStatusChanged` | Báo cáo đổi trạng thái |
| `NewComment` | Có comment mới |
| `BadgeEarned` | Nhận badge mới |
| `LevelUp` | Lên cấp |
| `SlaBreachWarning` | Cảnh báo vi phạm SLA |
| `NearbyReport` | Báo cáo gần vị trí |
| `PenaltyIssued` | Quyết định phạt |
| `ContractExpiry` | Hợp đồng sắp hết hạn |
| `ReportOverdue` | Báo cáo quá hạn > 72h |
| `ReportUnassigned` | Báo cáo xác minh chưa giao > 24h |
| `ReportAutoClosed` | Báo cáo tự đóng sau 7 ngày |

### Allowed Placeholders

Template body/title chỉ được sử dụng các placeholder sau:

| Placeholder | Mô tả |
|---|---|
| `{user_name}` | Tên người dùng |
| `{report_id}` | ID báo cáo (GUID) |
| `{report_code}` | Mã báo cáo (VD: RPT-2026-001) |
| `{priority}` | Mức độ ưu tiên |
| `{status}` | Trạng thái hiện tại |
| `{time}` | Thời gian sự kiện |
| `{penalty_amount}` | Số tiền phạt (VND) |
| `{ward_name}` | Tên phường/xã |
| `{company_name}` | Tên công ty môi trường |
| `{category_name}` | Loại ô nhiễm |
| `{severity}` | Mức độ nghiêm trọng |
| `{officer_name}` | Tên cán bộ xử lý |
| `{team_name}` | Tên đội dọn dẹp/thanh tra |

---

### 6.1. `GET /v1/admin/notification-templates`

Danh sách template thông báo có phân trang.

**Query Parameters:**

| Param | Type | Default | Mô tả |
|---|---|---|---|
| `page` | int | 1 | Trang hiện tại |
| `pageSize` | int | 20 | Số bản ghi/trang |
| `channel` | string? | — | Lọc theo channel: `Push`, `Email`, `Both` |
| `isPublished` | bool? | — | Lọc theo trạng thái publish |

**Response `200 OK`:**

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "template-guid",
        "templateKey": "report_verified",
        "titleVi": "Báo cáo #{report_code} đã được xác minh",
        "channel": "Push",
        "type": "ReportStatusChanged",
        "isPublished": true,
        "isActive": true,
        "createdAt": "2026-07-10T08:00:00Z",
        "updatedAt": "2026-07-10T09:00:00Z"
      }
    ],
    "totalCount": 8
  }
}
```

---

### 6.2. `POST /v1/admin/notification-templates`

Tạo template mới ở trạng thái **draft** (chưa publish). Template key phải là `snake_case` và unique cho mỗi channel.

**Request Body:**

```json
{
  "templateKey": "report_verified",
  "titleVi": "Báo cáo #{report_code} đã được xác minh",
  "bodyVi": "Xin chào {user_name}, báo cáo #{report_code} về {category_name} tại {ward_name} đã được xác minh bởi {officer_name}.",
  "titleEn": "Report #{report_code} has been verified",
  "bodyEn": "Hello {user_name}, your report #{report_code} about {category_name} at {ward_name} has been verified by {officer_name}.",
  "channel": "Push",
  "type": "ReportStatusChanged"
}
```

| Field | Type | Required | Validation |
|---|---|---|---|
| `templateKey` | string | ✅ | `snake_case`, max 100 chars, regex: `^[a-z][a-z0-9_]*$` |
| `titleVi` | string | ✅ | Max 500 chars |
| `bodyVi` | string | ✅ | Max 4000 chars, chỉ chứa allowed placeholders |
| `titleEn` | string? | ❌ | Max 500 chars |
| `bodyEn` | string? | ❌ | Max 4000 chars, chỉ chứa allowed placeholders |
| `channel` | string | ✅ | Enum: `Push`, `Email`, `Both` |
| `type` | string | ✅ | Enum: xem bảng `NotificationType` ở trên |

**Response `201 Created`:**

```json
{
  "success": true,
  "data": {
    "id": "new-template-guid",
    "templateKey": "report_verified",
    "isPublished": false
  }
}
```

**Error `409 Conflict`** — Template key đã tồn tại cho channel này.

**Error `422 Validation`** — Body chứa placeholder không hợp lệ.

**cURL:**

```bash
curl -X POST "https://api.greenlens.vn/v1/admin/notification-templates" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "templateKey": "report_verified",
    "titleVi": "Báo cáo #{report_code} đã được xác minh",
    "bodyVi": "Xin chào {user_name}, báo cáo #{report_code} đã xác minh.",
    "channel": "Push",
    "type": "ReportStatusChanged"
  }'
```

---

### 6.3. `PATCH /v1/admin/notification-templates/{id}/publish`

Publish hoặc Unpublish template. Chỉ template đã publish mới được hệ thống sử dụng để gửi thông báo.

**Path:** `id` — Guid của NotificationTemplate

**Request Body:**

```json
{ "publish": true }
```

| Field | Type | Default | Mô tả |
|---|---|---|---|
| `publish` | bool | `true` | `true` = publish, `false` = unpublish |

**Response `204 No Content`** (wrapped):

```json
{ "success": true, "message": "Đã publish template." }
```

hoặc

```json
{ "success": true, "message": "Đã unpublish template." }
```

**Error `404 Not Found`** — Template không tồn tại.

> [!NOTE]
> Khi template được **Update** (sửa nội dung), trạng thái tự động chuyển về `IsPublished = false` — admin phải publish lại sau khi review.

---

### 6.4. `POST /v1/admin/notification-templates/{id}/test`

Test gửi thử template đến admin hiện tại. Render placeholder bằng sample data, gửi notification thật với prefix `[TEST]`. **Không gửi cho user thật.**

**Path:** `id` — Guid của NotificationTemplate

**Request Body:** JSON object key-value, key = tên placeholder (không có dấu `{}`), value = giá trị mẫu.

```json
{
  "user_name": "Nguyễn Văn Test",
  "report_code": "RPT-2026-001",
  "category_name": "Rác thải sinh hoạt",
  "ward_name": "Phường Bến Nghé",
  "officer_name": "Trần Thị B"
}
```

**Response `200 OK`:**

```json
{
  "success": true,
  "data": {
    "renderedTitle": "[TEST] Báo cáo #RPT-2026-001 đã được xác minh",
    "renderedBody": "Xin chào Nguyễn Văn Test, báo cáo #RPT-2026-001 về Rác thải sinh hoạt tại Phường Bến Nghé đã được xác minh bởi Trần Thị B.",
    "sentTo": "admin@greenlens.vn"
  }
}
```

**Error `404 Not Found`** — Template không tồn tại.

> [!TIP]
> Placeholder không có trong `sampleData` sẽ giữ nguyên dạng `{placeholder_name}` trong kết quả render — giúp admin phát hiện thiếu data.

> **Hành vi theo system settings (contract giữ nguyên):** Trước khi render, handler tự merge **`NotificationSystemSettingPlaceholders`** (vd. `{sla_verify_hours}`, `{overdue_pending_hours}`, `{auto_close_resolved_days}`) từ `system_settings` — admin **không** cần truyền các key này trong `sampleData`. Body request/response schema không đổi; `renderedTitle` / `renderedBody` có thể khác số sau khi admin PATCH settings.

**cURL:**

```bash
curl -X POST "https://api.greenlens.vn/v1/admin/notification-templates/template-guid/test" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"user_name": "Test User", "report_code": "RPT-001"}'
```

---

## Phụ lục A — API có sẵn: contract giữ nguyên, hành vi/nội dung thay đổi

> Các endpoint dưới đây **không đổi** route, query/body schema, hay shape JSON response. Chỉ **logic nghiệp vụ**, **message text**, hoặc **ngưỡng mặc định** thay đổi khi admin sửa `system_settings` hoặc ngưỡng badge.
>
> Danh sách đầy đủ consumer (Citizen / LEO / job / notification): xem [`admin-system-configuration.md`](./admin-system-configuration.md) §4.

### Trong phạm vi `/v1/admin` (tài liệu này)

| API | Thay đổi gì | Config liên quan |
|-----|-------------|------------------|
| `GET /v1/admin/spam-suspects` | Khi **không** truyền `minReportsPerHour`, ngưỡng HighVolume lấy từ settings thay vì hardcode `5` | `submit_max_per_hour` (module `rate_limits`) |
| `GET /v1/admin/badges` | Response thêm field `requiredActionCount`; sort thêm cột này — **đây là mở rộng schema**, không phải hành vi ngầm | Cột DB `badges` |
| `POST /v1/admin/notification-templates/{id}/test` | Render test merge placeholder số từ settings trước `sampleData` | 8 key SLA/lifecycle/nearby/… (xem `admin-system-configuration.md` Case B) |
| `PUT /v1/admin/badges/{id}` | Chỉ metadata; ngưỡng eligibility chuyển sang `PATCH .../thresholds` | — |
| Auto-award badge (không phải HTTP admin) | Ngưỡng đọc DB sau PATCH thresholds | `PATCH /v1/admin/badges/{id}/thresholds` |

### Admin dashboard (controller riêng — không nằm `/v1/admin`)

| API | Thay đổi gì | Config liên quan |
|-----|-------------|------------------|
| `GET /v1/dashboard/admin/alerts` | Message `OVERDUE_REPORTS` dùng số giờ động (vd. *"quá 72 giờ"*) | `overdue_pending_hours` |
| `GET /v1/dashboard/deo/alerts` | Tương tự + contract expiry horizon | `overdue_pending_hours`, `contract_warning_days` |

Schema `AlertItem` (`code`, `severity`, `message`) **giữ nguyên** — chỉ chuỗi `message` thay đổi theo config.

### FE Admin — cần làm gì?

- **Không** cần đổi TypeScript type cho alerts / test template (trừ `GET badges` thêm `requiredActionCount`).
- **Nên** hiển thị dialog xác nhận trước khi PATCH settings (xem `admin-system-configuration.md` §5.3 mục 8).
- Sau PATCH settings, refresh spam dashboard / alerts để thấy hành vi và message mới.

---

## Tổng hợp Error Codes

| HTTP Status | Khi nào |
|---|---|
| `200 OK` | Query thành công, hoặc command thành công (wrapped) |
| `201 Created` | Tạo resource mới thành công |
| `204 No Content` | Update/Delete thành công (wrapped trong `ApiResponse`) |
| `400 Bad Request` | Validation fail (FluentValidation) |
| `401 Unauthorized` | Thiếu hoặc sai JWT |
| `403 Forbidden` | User không có role Admin |
| `404 Not Found` | Entity không tồn tại |
| `409 Conflict` | Duplicate key (template key + channel, category + violation level) |
| `422 Unprocessable Entity` | Business rule violation |

---

## Entities & Database Tables

| Entity | Table (snake_case) | Mô tả | Đặc điểm |
|---|---|---|---|
| `PenaltyFramework` | `penalty_frameworks` | Khung mức phạt theo category + level | AuditableEntity, soft-toggle |
| `AuditLog` | `audit_logs` | Nhật ký hành động nhạy cảm | **Immutable** — không kế thừa AuditableEntity |
| `GamificationConfig` | `gamification_configs` | Cấu hình điểm thưởng/phạt | AuditableEntity, seeded 6 rows |
| `NotificationTemplate` | `notification_templates` | Template thông báo với placeholders | AuditableEntity, publish lifecycle |
