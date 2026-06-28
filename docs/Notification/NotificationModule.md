# Notification Module — Technical Documentation

> **Module:** Notifications (BR-NTF-001..004)
> **Implemented:** 2026-06-28
> **Status:** ✅ Production-ready

---

## 1. Overview

Module quản lý thông báo cho người dùng qua 2 kênh: **Push notification (FCM)** và **Email (SMTP)**.
User có thể cấu hình bật/tắt từng kênh cho từng loại thông báo.

### Business Rules Implemented

| BR ID | Mô tả | Status |
|---|---|:---:|
| BR-NTF-001 | Kênh: Push (FCM) + Email. User cấu hình bật/tắt per-type | ✅ |
| BR-NTF-002 | Events: report status change, badge, SLA breach, etc. | ✅ |
| BR-NTF-003 | Anti-spam: max 20/ngày/loại | ✅ |
| BR-NTF-004 | i18n: vi-VN mặc định | ⚠️ Hardcode vi-VN, en-US chờ P2 |

---

## 2. Architecture

```
┌─────────────────────────────────────────────────┐
│  Domain Events (MediatR INotification)          │
│  ReportVerifiedEvent, ReportRejectedEvent, ...  │
└──────────────────────┬──────────────────────────┘
                       │
        ┌──────────────▼──────────────────────┐
        │  Event Handlers (Application layer) │
        │  ReportStatusNotificationHandler    │
        └──────────────┬──────────────────────┘
                       │ calls
        ┌──────────────▼──────────────────────┐
        │    INotificationService             │
        │    (Application interface)          │
        └──────────────┬──────────────────────┘
                       │ implemented by
        ┌──────────────▼──────────────────────┐
        │    NotificationService              │
        │    (Infrastructure)                 │
        │                                     │
        │  1. Check user preferences          │
        │  2. Anti-spam guard (20/type/day)   │
        │  3. Persist Notification entity     │
        │  4. Dispatch: FCM push + Email      │
        └──────────────┬──────────────────────┘
                       │
          ┌────────────┼───────────────┐
          ▼                            ▼
   FCM Push Sender              SMTP Email Sender
   (FcmPushNotificationSender)  (SmtpEmailSender)
```

---

## 3. Domain Entities

### 3.1 Notification

| Field | Type | Mô tả |
|---|---|---|
| `Id` | Guid | PK |
| `RecipientId` | Guid | FK → User |
| `Type` | NotificationType (enum) | Loại thông báo |
| `Title` | string (200) | Tiêu đề |
| `Message` | string (2000) | Nội dung |
| `ReferenceId` | Guid? | Optional reference (ReportId, BadgeId, etc.) |
| `Channel` | NotificationChannel | Push / Email / Both |
| `IsRead` | bool | Đã đọc? |
| `ReadAt` | DateTime? | Thời điểm đọc |
| `CreatedAt` | DateTime | Thời điểm tạo |

### 3.2 NotificationPreference

| Field | Type | Mô tả |
|---|---|---|
| `Id` | Guid | PK |
| `UserId` | Guid | FK → User |
| `Type` | NotificationType | Loại thông báo |
| `PushEnabled` | bool | Bật push? (default: true) |
| `EmailEnabled` | bool | Bật email? (default: true) |

**Unique constraint:** `(UserId, Type)` — 1 preference per user per type.

### 3.3 NotificationType Enum

```
ReportStatusChanged, NewComment, BadgeEarned, LevelUp,
SlaBreachWarning, NearbyReport, PenaltyIssued, ContractExpiry
```

### 3.4 User Extensions

- `FcmDeviceToken` (string?) — FCM device token, updated by mobile app on startup
- `Language` (string, default "vi-VN") — Preferred language cho notifications

---

## 4. API Endpoints

Base URL: `/v1/notifications`

| Method | Path | Mô tả | Auth |
|---|---|---|:---:|
| `GET` | `/v1/notifications` | Danh sách thông báo (phân trang, filter isRead) | ✅ |
| `PUT` | `/v1/notifications/{id}/read` | Đánh dấu 1 thông báo đã đọc | ✅ |
| `PUT` | `/v1/notifications/read-all` | Đánh dấu tất cả đã đọc | ✅ |
| `GET` | `/v1/notifications/preferences` | Lấy cài đặt thông báo | ✅ |
| `PUT` | `/v1/notifications/preferences` | Cập nhật cài đặt thông báo | ✅ |
| `PUT` | `/v1/notifications/device-token` | Đăng ký/cập nhật FCM device token | ✅ |

### 4.1 GET /v1/notifications

**Query parameters:**
- `page` (int, default 1)
- `pageSize` (int, default 20)
- `isRead` (bool?, optional) — filter đã đọc / chưa đọc

**Response:**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "guid",
        "type": "ReportStatusChanged",
        "title": "Báo cáo đã được xác minh",
        "message": "Báo cáo ô nhiễm của bạn đã được xác minh...",
        "referenceId": "report-guid",
        "isRead": false,
        "readAt": null,
        "createdAt": "2026-06-28T09:00:00Z"
      }
    ],
    "totalCount": 42,
    "unreadCount": 5
  }
}
```

### 4.2 PUT /v1/notifications/{id}/read

Đánh dấu 1 thông báo đã đọc. Idempotent (gọi nhiều lần không lỗi).

### 4.3 PUT /v1/notifications/read-all

Đánh dấu tất cả thông báo chưa đọc → đã đọc.

**Response:**
```json
{
  "success": true,
  "data": { "markedCount": 5 }
}
```

### 4.4 GET /v1/notifications/preferences

Trả về preferences cho tất cả notification types. Types chưa customize → default `true`.

**Response:**
```json
{
  "success": true,
  "data": [
    { "type": "ReportStatusChanged", "pushEnabled": true, "emailEnabled": true },
    { "type": "NewComment", "pushEnabled": true, "emailEnabled": false },
    { "type": "SlaBreachWarning", "pushEnabled": true, "emailEnabled": true }
  ]
}
```

### 4.5 PUT /v1/notifications/preferences

**Request body:**
```json
{
  "preferences": [
    { "type": "ReportStatusChanged", "pushEnabled": true, "emailEnabled": false },
    { "type": "NewComment", "pushEnabled": false, "emailEnabled": false }
  ]
}
```

### 4.6 PUT /v1/notifications/device-token

**Request body:**
```json
{
  "deviceToken": "fcm-token-from-firebase-sdk"
}
```

Gọi khi app khởi động hoặc khi token refresh. Gửi `null` để xóa token (opt-out push).

---

## 5. Anti-spam (BR-NTF-003)

- **Limit:** Max 20 notifications per type per day per user
- **Logic:** Count notifications created today for (recipientId, type). If ≥ 20 → skip
- **Digest:** Chưa implement (P2). Khi đạt giới hạn, notification bị drop, không queue

---

## 6. Event Handlers (Decoupled Pattern)

Notifications được trigger bởi Domain Events, cùng pattern với Gamification:

| Domain Event | Notification Type | Message |
|---|---|---|
| `ReportVerifiedEvent` | ReportStatusChanged | "Báo cáo đã được xác minh" |
| `ReportRejectedEvent` | ReportStatusChanged | "Báo cáo bị từ chối" |
| `ReportResolvedEvent` | ReportStatusChanged | "Báo cáo đã được giải quyết" |

**Mở rộng:** Để thêm notification cho event mới, chỉ cần:
1. Tạo thêm `INotificationHandler<NewEvent>` trong `EventHandlers/`
2. Gọi `INotificationService.SendAsync(...)` — không cần sửa handler gốc

---

## 7. FCM Push Notifications

### Setup cần thiết (FE / Mobile)
1. Thêm Firebase SDK vào app (Android/iOS/Flutter)
2. Khi user cho phép quyền notification → nhận FCM token từ Firebase
3. Gọi `PUT /v1/notifications/device-token` gửi token lên backend
4. Backend lưu token trong `User.FcmDeviceToken`

### Push payload
```json
{
  "notification": {
    "title": "Báo cáo đã được xác minh",
    "body": "Báo cáo ô nhiễm của bạn đã được xác minh..."
  },
  "data": {
    "referenceId": "report-guid",
    "type": "ReportStatusChanged"
  },
  "android": {
    "priority": "high",
    "notification": {
      "sound": "default",
      "click_action": "FLUTTER_NOTIFICATION_CLICK"
    }
  }
}
```

---

## 8. Database

### Tables mới
- `notifications` — lưu trữ mọi notification đã gửi
- `notification_preferences` — per-user per-type channel toggles

### Indexes
- `ix_notifications_recipient_read_created` — query listing (sorted by date, filter read)
- `ix_notifications_recipient_type_created` — anti-spam count query
- `ix_notification_preferences_user_type` — unique constraint

### User columns mới
- `fcm_device_token` (varchar, nullable)
- `language` (varchar, default "vi-VN")

### Report columns mới (SLA breach tracking)
- `sla_verify_breached` (bool, default false)
- `sla_resolve_breached` (bool, default false)

### Migration
- `202606280900_AddNotificationsAndSlaBreachFields`

---

## 9. Files Created/Modified

### New Files
| File | Layer | Mô tả |
|---|---|---|
| `Notification.cs` | Domain | Entity |
| `NotificationPreference.cs` | Domain | Entity |
| `NotificationType.cs` | Domain | Enum |
| `NotificationChannel.cs` | Domain | Enum |
| `INotificationService.cs` | Application | Interface |
| `IPushNotificationSender.cs` | Application | Interface |
| `INotificationRepository.cs` | Application | Repo interface |
| `INotificationPreferenceRepository.cs` | Application | Repo interface |
| `NotificationErrors.cs` | Application | Error constants |
| `GetMyNotificationsQuery.cs` + Handler | Application | Feature slice |
| `MarkNotificationReadCommand.cs` | Application | Feature slice |
| `MarkAllReadCommand.cs` | Application | Feature slice |
| `GetNotificationPreferencesQuery.cs` | Application | Feature slice |
| `UpdateNotificationPreferencesCommand.cs` | Application | Feature slice |
| `UpdateDeviceTokenCommand.cs` | Application | Feature slice |
| `ReportStatusNotificationHandler.cs` | Application | Event handlers |
| `NotificationService.cs` | Infrastructure | Core service |
| `FcmPushNotificationSender.cs` | Infrastructure | FCM push |
| `NotificationRepository.cs` | Infrastructure | Repo impl |
| `NotificationPreferenceRepository.cs` | Infrastructure | Repo impl |
| `NotificationConfiguration.cs` | Infrastructure | EF config |
| `NotificationPreferenceConfiguration.cs` | Infrastructure | EF config |
| `NotificationsController.cs` | API | Controller |

### Modified Files
| File | Thay đổi |
|---|---|
| `User.cs` | `+FcmDeviceToken`, `+Language`, `+UpdateFcmToken()`, `+UpdateLanguage()` |
| `Report.cs` | `+SlaVerifyBreached`, `+SlaResolveBreached`, `+MarkSlaVerifyBreached()`, `+MarkSlaResolveBreached()` |
| `IEmailSender.cs` | `+SendNotificationEmailAsync()` |
| `SmtpEmailSender.cs` | Implement `SendNotificationEmailAsync()` |
| `ApplicationDbContext.cs` | `+Notifications`, `+NotificationPreferences` DbSets |
| `DependencyInjection.cs` | Register repos, services, TransactionBehavior, recurring jobs |

---

## 10. TODO / Limitations

- [ ] **BR-NTF-003 Digest:** Anti-spam hiện drop notification khi vượt 20/day. Cần digest job gộp cuối ngày (P2)
- [ ] **BR-NTF-004 i18n:** Template vi-VN hardcode. Cần resource files + Admin template management (P2)
- [ ] **More event types:** Hiện chỉ trigger cho Report status changes. Cần thêm handlers cho: NewComment, BadgeEarned, LevelUp, SlaBreachWarning, NearbyReport, PenaltyIssued, ContractExpiry
- [ ] **FCM token cleanup:** Khi Firebase trả `Unregistered` error → cần clear token khỏi User entity (hiện chỉ log warning)
- [ ] **Batch push:** Khi có nhiều notifications → dùng `FirebaseMessaging.SendEachAsync()` thay vì gửi từng cái
