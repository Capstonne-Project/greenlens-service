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
| 6 | `/audit-logs/{id}` | GET | Chi tiết audit log |
| 7 | `/reports/{id}/hide` | POST | Ẩn báo cáo vi phạm |
| 8 | `/reports/{id}/unhide` | POST | Hiện lại báo cáo |
| 9 | `/spam-suspects` | GET | Spam dashboard |
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
> Tự động ghi log khi Command implement `IAuditable` được xử lý thành công, thông qua `AuditLogBehavior` pipeline.

### Commands tự động được audit

| Command | Entity Type |
|---|---|
| `UpdateUserRoleCommand` | User |
| `ForceUpdateReportStatusCommand` | Report |
| `ToggleBanUserCommand` | User |
| `SuspendCompanyCommand` | Company |
| `TerminateCompanyCommand` | Company |
| `HideReportCommand` | Report |
| `UnhideReportCommand` | Report |
| `UpdateGamificationConfigCommand` | GamificationConfig |

---

### 2.1. `GET /v1/admin/audit-logs`

Danh sách audit log phân trang, lọc theo user, entity type, action, khoảng thời gian.

**Query Parameters:**

| Param | Type | Default | Mô tả |
|---|---|---|---|
| `page` | int | 1 | Trang hiện tại |
| `pageSize` | int | 20 | Số bản ghi/trang |
| `userId` | Guid? | — | Lọc theo ID người thực hiện |
| `entityType` | string? | — | Lọc theo loại entity (`User`, `Report`, `Company`…) |
| `action` | string? | — | Lọc theo tên command (`UpdateUserRoleCommand`…) |
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
        "action": "UpdateUserRoleCommand",
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
    "action": "ToggleBanUserCommand",
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
> 1. Submit ≥ 5 báo cáo / giờ
> 2. ≥ 3 báo cáo bị rejected trong 7 ngày gần nhất
> 3. ≥ 2 báo cáo bị AI flag là `IrrelevantOrSuspectedAbusive`

---

### 4.1. `GET /v1/admin/spam-suspects`

Trả về danh sách tài khoản nghi spam.

**Query Parameters:**

| Param | Type | Default | Mô tả |
|---|---|---|---|
| `page` | int | 1 | Trang hiện tại |
| `pageSize` | int | 20 | Số bản ghi/trang |
| `minReportsPerHour` | int | 5 | Ngưỡng submit/giờ |
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

**cURL:**

```bash
curl -X POST "https://api.greenlens.vn/v1/admin/notification-templates/template-guid/test" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"user_name": "Test User", "report_code": "RPT-001"}'
```

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
