# FE — Admin Audit Log API Guide

> **Base URL:** `/v1/admin/audit-logs`  
> **Authorization:** `Bearer <JWT>` — Role = `Admin`  
> **Business Rule:** BR-ADM-010 (audit log nhạy cảm, retention ≥ 12 tháng)  
> **Envelope:** `{ code, message, status, data }`

Màn **Audit Log** giúp Admin tra cứu ai đã làm gì, trên entity nào, lúc nào, từ IP/User-Agent nào. Bản ghi **immutable** — chỉ đọc, không sửa/xóa qua API.

---

## Mục lục

| # | Endpoint | Method | Mô tả |
|---|----------|--------|-------|
| 1 | `/audit-logs` | GET | Danh sách phân trang + filter |
| 2 | `/audit-logs/{id}` | GET | Chi tiết 1 bản ghi (có JSON diff) |
| 3 | `/audit-logs/export` | GET | Export CSV (date range bắt buộc) |
| 4 | `/audit-logs/stats` | GET | Thống kê dashboard |
| 5 | `/users/{id}/ban` | PUT | Cấm/bỏ cấm user (ghi audit) |

---

## 1. Tổng quan luồng FE

```text
Audit Log Page
  ├─ Filter panel: userId, entityType, entityId, action, fromDate, toDate
  ├─ Table: action, entityType, entityId, userEmail, actorRole, createdAt
  └─ Row click → Drawer/Modal
        └─ GET /audit-logs/{id} → hiển thị oldValues/newValues JSON, IP, UA
```

**Deep link gợi ý**

- Từ trang User detail → `/admin/audit-logs?entityType=User&entityId={userId}`
- Từ trang Company detail → `?entityType=Company&entityId={companyId}`
- Lọc theo admin thực hiện → `?userId={adminId}`

---

## 2. `GET /v1/admin/audit-logs`

Danh sách audit log phân trang.

### Query parameters

| Param | Default | Max | Mô tả |
|-------|---------|-----|-------|
| `page` | 1 | — | Trang (1-based) |
| `pageSize` | 20 | **100** | Số bản ghi/trang |
| `userId` | — | — | Lọc theo ID người thực hiện (actor) |
| `actorRole` | — | — | Lọc theo role người thực hiện (`Admin`, `LEO`, `DEO`, …) |
| `entityType` | — | — | Loại entity đích (xem bảng catalog bên dưới) |
| `entityId` | — | — | ID entity đích (exact match, Guid string) |
| `action` | — | — | Tên action/command (contains, không phân biệt hoa thường) |
| `fromDate` | — | — | Từ ngày (UTC) |
| `toDate` | — | — | Đến ngày (UTC); phải ≥ `fromDate` |

### Response 200

```json
{
  "code": "SUCCESS",
  "message": "OK",
  "status": 200,
  "data": {
    "items": [
      {
        "id": "a1b2c3d4-...",
        "userId": "admin-guid",
        "userEmail": "admin@greenlens.vn",
        "actorRole": "Admin",
        "action": "UpdateUserRole",
        "entityType": "User",
        "entityId": "target-user-guid",
        "ipAddress": "103.1.2.3",
        "userAgent": "Mozilla/5.0 ...",
        "createdAt": "2026-07-30T09:30:00Z"
      }
    ],
    "pagination": {
      "page": 1,
      "pageSize": 20,
      "totalItems": 156,
      "totalPages": 8,
      "hasNext": true,
      "hasPrev": false
    }
  }
}
```

### Lỗi thường gặp

| Code | HTTP | Khi nào |
|------|------|---------|
| `VALIDATION_ERROR` | 400 | `pageSize` > 100, `fromDate` > `toDate` |
| `UNAUTHORIZED` | 401 | Thiếu/hết hạn token |
| `FORBIDDEN` | 403 | Không phải role Admin |

---

## 3. `GET /v1/admin/audit-logs/{id}`

Chi tiết 1 bản ghi, bao gồm payload JSON.

### Response 200

```json
{
  "code": "SUCCESS",
  "message": "OK",
  "status": 200,
  "data": {
    "id": "a1b2c3d4-...",
    "userId": "admin-guid",
    "userEmail": "admin@greenlens.vn",
    "actorRole": "Admin",
    "action": "ToggleBanUser",
    "entityType": "User",
    "entityId": "banned-user-guid",
    "oldValues": null,
    "newValues": "{\"userId\":\"...\"}",
    "ipAddress": "103.1.2.3",
    "userAgent": "Mozilla/5.0 ...",
    "createdAt": "2026-07-30T09:30:00Z"
  }
}
```

### Lỗi

| Code | HTTP | Khi nào |
|------|------|---------|
| `AUDIT_LOG_NOT_FOUND` | 404 | ID không tồn tại |

### Gợi ý UI detail drawer

1. Parse `newValues` / `oldValues` bằng `JSON.parse` (fallback raw text nếu invalid).
2. Hiển thị side-by-side hoặc accordion khi `oldValues` có giá trị (BlockedWord update có diff đầy đủ).
3. **`oldValues`** có diff đầy đủ cho workflow commands (VerifyReport, UpdateUser, BlockedWord…) và Admin Update handlers Phase 3.
4. Không log PII nhạy cảm ở client console ở môi trường production.

---

## 4. `PUT /v1/admin/users/{id}/ban`

Toggle cấm/bỏ cấm tài khoản. Mỗi lần gọi đảo trạng thái `IsBanned`.

**Không có request body.**

### Response 200

```json
{
  "code": "SUCCESS",
  "message": "OK",
  "status": 200,
  "data": {
    "userId": "...",
    "isBanned": true,
    "message": "Đã cấm tài khoản thành công."
  }
}
```

### Lỗi

| Code | HTTP | Khi nào |
|------|------|---------|
| `CANNOT_BAN_SELF` | 422 | Admin tự cấm chính mình |
| `USER_NOT_FOUND` | 404 | User không tồn tại |

Sau khi ban thành công, có thể refresh audit list với `entityType=User&entityId={id}` để xem log `ToggleBanUser`.

---

## 5. Catalog `entityType` / `action` (filter dropdown)

### entityType

| Giá trị | Ý nghĩa |
|---------|---------|
| `User` | Tài khoản, role, ban, reset password |
| `Report` | Force status, hide/unhide, **Officer workflow** (verify, reject, assign, duplicate, reopen) |
| `Company` | Tạo, gia hạn, suspend, terminate, service areas |
| `PollutionCategory` | CRUD danh mục ô nhiễm |
| `WasteTag` | CRUD tag loại rác |
| `PenaltyFramework` | Khung mức phạt |
| `NotificationTemplate` | Template thông báo |
| `GamificationConfig` | Cấu hình điểm |
| `BlockedWord` | Từ bị chặn (manual audit, có oldValues) |
| `InspectionReport` | Inspection lifecycle, issue penalty, record payment |
| `PenaltyPayment` | Xóa bản ghi nộp phạt |
| `ViolatingEntity` | Xóa đối tượng vi phạm |

### action (một số giá trị phổ biến)

| Action | Mô tả ngắn |
|--------|------------|
| `CreateAccount` | Admin tạo user |
| `UpdateUser` | Admin sửa profile user |
| `DeleteUser` | Admin soft-delete user |
| `UpdateUserRole` | Đổi role |
| `ToggleBanUser` | Cấm/bỏ cấm |
| `ForceUpdateReportStatus` | Override status báo cáo |
| `HideReport` / `UnhideReport` | Ẩn/hiện báo cáo |
| `VerifyReport` / `RejectReport` | LEO xác minh / từ chối báo cáo |
| `AssignTeam` / `ReassignTeam` | Giao / đổi đội xử lý |
| `DispatchToCompany` / `AssignCompanyTeam` | Giao công ty / đội công ty |
| `ConfirmDuplicate` / `DismissDuplicate` | Xác nhận / bỏ cờ trùng lặp |
| `ApproveReopenRequest` / `RejectReopenRequest` | Duyệt / từ chối yêu cầu mở lại |
| `DeleteReport` | Citizen soft-delete báo cáo |
| `CreateInspectionReport` / `AssignInspectionTeam` | Tạo / giao đội thanh tra |
| `CloseInspection` / `CloseNoViolation` / `DeclineInspection` | Đóng / không vi phạm / từ chối |
| `RecordPayment` / `DeletePenaltyPayment` | Ghi nhận / xóa nộp phạt |
| `DeleteViolatingEntity` | Xóa đối tượng vi phạm |
| `CreateCompany` / `RenewContract` | Vòng đời công ty |
| `IssuePenalty` | Ban hành QĐ xử phạt |
| `BlockedWord.Create` / `.Update` / `.Delete` | Quản lý từ cấm |

Tên `action` từ auto-audit thường là tên command bỏ hậu tố `Command` (vd. `UpdateUserRole`).

---

## 6. Filter UI — checklist FE

- [ ] Date range picker bind `fromDate` / `toDate` (UTC ISO string)
- [ ] Dropdown `entityType` từ catalog §5
- [ ] Text input `entityId` khi drill-down từ detail page
- [ ] Autocomplete `userId` (admin actor) nếu có user search API
- [ ] `pageSize` select: 20 / 50 / 100 (max 100)
- [ ] Dropdown `actorRole` từ enum `UserRole` (Admin, LEO, DEO, …) — bind query param `actorRole`
- [ ] Hiển thị cột `actorRole` trong table
- [ ] Empty state khi `items.length === 0`
- [ ] Pagination dùng `pagination.hasNext` / `hasPrev`

---

## 7. Retention & giới hạn

- Backend giữ log **≥ 12 tháng** (`DataRetentionJob` xóa `audit_logs` cũ hơn 12 tháng).
- Officer/LEO + Inspection workflow commands ghi audit với **oldValues/newValues** (status transition).
- Export CSV và dashboard stats: xem §9.

---

## 9. Export CSV & Dashboard stats (Phase 3)

### `GET /v1/admin/audit-logs/export`

Tải file CSV audit log trong khoảng thời gian. **Bắt buộc** `fromDate` và `toDate` (UTC). Tối đa **90 ngày**.

| Param | Bắt buộc | Mô tả |
|-------|----------|-------|
| `fromDate` | Có | Từ ngày (UTC) |
| `toDate` | Có | Đến ngày (UTC) |
| `userId` | Không | Lọc actor |
| `actorRole` | Không | Lọc role actor (`Admin`, `LEO`, …) |
| `entityType` | Không | Lọc entity type |
| `action` | Không | Lọc action (contains) |

**Response:** `200` — `Content-Type: text/csv`, file download `audit_logs_YYYYMMDD_HHmm.csv`.

**Cột CSV:** `Id`, `UserId`, `Action`, `EntityType`, `EntityId`, `IpAddress`, `CreatedAtUtc` — **không** export email/GPS/oldValues/newValues (PII policy).

**Lỗi:** `VALIDATION_ERROR` 400 khi thiếu date hoặc range > 90 ngày.

### `GET /v1/admin/audit-logs/stats`

Thống kê cho dashboard widget.

| Param | Bắt buộc | Mô tả |
|-------|----------|-------|
| `fromDate` | Có | Từ ngày (UTC) |
| `toDate` | Có | Đến ngày (UTC), max 90 ngày |

**Response 200:**

```json
{
  "code": "SUCCESS",
  "data": {
    "totalCount": 420,
    "byAction": [
      { "action": "VerifyReport", "count": 85 },
      { "action": "ToggleBanUser", "count": 12 }
    ],
    "byDay": [
      { "date": "2026-07-28", "count": 45 },
      { "date": "2026-07-29", "count": 52 }
    ]
  }
}
```

`byAction` trả top 10 action theo count giảm dần.

### cURL export / stats

```bash
curl -G "https://api.greenlens.vn/v1/admin/audit-logs/export" \
  -H "Authorization: Bearer <token>" \
  --data-urlencode "fromDate=2026-07-01T00:00:00Z" \
  --data-urlencode "toDate=2026-07-30T23:59:59Z" \
  -o audit_logs.csv

curl -G "https://api.greenlens.vn/v1/admin/audit-logs/stats" \
  -H "Authorization: Bearer <token>" \
  --data-urlencode "fromDate=2026-07-01T00:00:00Z" \
  --data-urlencode "toDate=2026-07-30T23:59:59Z"
```

---

## 8. Ví dụ cURL

```bash
# List — lọc theo user bị tác động
curl -G "https://api.greenlens.vn/v1/admin/audit-logs" \
  -H "Authorization: Bearer <token>" \
  --data-urlencode "entityType=User" \
  --data-urlencode "entityId=<target-user-guid>" \
  --data-urlencode "page=1" \
  --data-urlencode "pageSize=20"

# Detail
curl "https://api.greenlens.vn/v1/admin/audit-logs/<audit-log-id>" \
  -H "Authorization: Bearer <token>"

# Ban user
curl -X PUT "https://api.greenlens.vn/v1/admin/users/<user-id>/ban" \
  -H "Authorization: Bearer <token>"
```
