# FE Guide — Cleanup Team Activity Notifications

> **Phiên bản:** 2026-08-06 · **Backend:** GreenLens API v1 · **Branch:** `develop`  
> **Business rules:** BR-CLN-001, BR-CLN-004, BR-CLN-005, BR-CLN-007, BR-NTF-002  
> **Audience:** LEO Web · CompanyManager Web · Cleaner Mobile (đọc để hiểu luồng)  
> **Liên quan:** [`fe-inspection-notifications-guide.md`](./fe-inspection-notifications-guide.md) · [`fe-async-notifications-auth-otp-guide.md`](./fe-async-notifications-auth-otp-guide.md)

Tài liệu mô tả **4 loại notification mới** khi **đội dọn dẹp** (community hoặc công ty) phản hồi nhiệm vụ được giao — LEO/CM biết **đội nào** accept/decline/cập nhật tiến độ/hoàn thành **báo cáo nào**.

---

## 1. Tóm tắt nhanh

| `type` (JSON) | Ai nhận | App | Khi nào gửi |
|---------------|---------|-----|-------------|
| `CleanupTaskAccepted` | Cán bộ đã **phân công** (`ReportAssignment.AssignedById`) | **LEO Web** hoặc **CompanyManager Web** | Team leader `accept` assignment |
| `CleanupTaskDeclined` | Cán bộ đã phân công | **LEO Web** hoặc **CM Web** | Team `decline` (Assigned, trong 24h, lý do ≥20 ký tự) |
| `CleanupProgressUpdated` | Cán bộ đã phân công | **LEO Web** hoặc **CM Web** | Team leader cập nhật tiến độ (%) |
| `CleanupTaskCompleted` | Cán bộ đã phân công | **LEO Web** hoặc **CM Web** | Team leader `resolve` (upload ≥2 ảnh After) |

**Đã có từ trước (không lặp lại ở đây):**

| `type` | Ai nhận | Khi nào |
|--------|---------|---------|
| `CleanupTaskAssigned` | Mọi member active của team | LEO/CM gán team |
| `CleanupProgressStale` | LEO | Job SLA — không cập nhật >48h |
| `ReportStatusChanged` (Resolved) | Citizen reporter | **Tất cả** đội active hoàn thành → báo cáo Resolved |

**Chung:**

- `referenceId` = **`reportId`** (Guid báo cáo umbrella).
- Template tiếng Việt có `{team_name}` — tên đội thực hiện.
- Push FCM/email async 1–5 giây sau DB write.

---

## 2. API đọc notification

Giống [`fe-inspection-notifications-guide.md`](./fe-inspection-notifications-guide.md) §2:

```http
GET /v1/notifications?page=1&pageSize=20&isRead=false
Authorization: Bearer {token}
```

**Ví dụ item `CleanupProgressUpdated`:**

```json
{
  "id": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
  "type": "CleanupProgressUpdated",
  "title": "Đội cập nhật tiến độ dọn dẹp",
  "message": "Đội Dọn Xanh Phường 1 cập nhật tiến độ 65% cho báo cáo R-2026-0042 tại Phường Bến Nghé.",
  "referenceId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "isRead": false,
  "createdAt": "2026-08-06T09:30:00Z",
  "categoryName": "Rác thải sinh hoạt",
  "thumbnailUrl": "https://cdn.example.com/reports/thumb.jpg"
}
```

SignalR hub: `/hubs/notifications` · event: `ReceiveNotification`

---

## 3. Ma trận chi tiết theo loại

### 3.1 `CleanupTaskAccepted`

| | |
|--|--|
| **Template key** | `cleanup_task_accepted` |
| **Người nhận** | User id của người đã gán team (`AssignedById` trên assignment) |
| **Trigger API** | `PUT /v1/teams/reports/{reportId}/accept` (team leader) |

**Body (vi):** `Đội {team_name} đã chấp nhận nhiệm vụ dọn dẹp báo cáo {report_code} tại {ward_name}.`

**FE LEO/CM Web:** Toast → mở **Progress board** hoặc report detail `GET /v1/reports/{referenceId}`.

---

### 3.2 `CleanupTaskDeclined`

| | |
|--|--|
| **Template key** | `cleanup_task_declined` |
| **Người nhận** | Cán bộ đã phân công (`AssignedById`) |
| **Trigger API** | `PUT /v1/teams/reports/{reportId}/decline` (body: `teamId`, `reason`) |

**Body (vi):** `Đội {team_name} đã từ chối nhiệm vụ dọn dẹp báo cáo {report_code} tại {ward_name}. Lý do: {decline_reason}. Vui lòng phân công lại.`

| Placeholder | Nguồn |
|-------------|--------|
| `{team_name}` | Tên Environmental Team từ chối |
| `{decline_reason}` | Body `reason` API |

**FE LEO Web:** CTA **Phân công lại** → `PUT /v1/reports/{id}/assign-team`.  
**FE CM Web:** CTA **Đổi đội công ty** → assign từ company queue.

---

### 3.3 `CleanupProgressUpdated`

| | |
|--|--|
| **Template key** | `cleanup_progress_updated` |
| **Người nhận** | Cán bộ đã phân công |
| **Trigger API** | `PUT /v1/reports/{reportId}/progress` (leader, JWT) **hoặc** `PUT /v1/teams/reports/{reportId}/progress` (body có `teamId`) |

**Body (vi):** `Đội {team_name} cập nhật tiến độ {progress_percent}% cho báo cáo {report_code} tại {ward_name}.`

**FE LEO/CM Web:** Badge trên progress board; tap → report detail tab tiến độ / timeline.

**Lưu ý:** Khác với `CleanupProgressStale` (job cảnh báo **không** cập nhật >48h). Loại này gửi **mỗi lần** team post progress.

---

### 3.4 `CleanupTaskCompleted`

| | |
|--|--|
| **Template key** | `cleanup_task_completed` |
| **Người nhận** | Cán bộ đã phân công |
| **Trigger API** | `PUT /v1/reports/{reportId}/resolve` (leader upload ≥2 ảnh After) |

**Body (vi):** `Đội {team_name} đã hoàn thành nhiệm vụ dọn dẹp báo cáo {report_code} tại {ward_name}.{resolution_note}`

| `{resolution_note}` | Điều kiện |
|---------------------|-----------|
| ` Báo cáo đã chuyển sang trạng thái Đã xử lý.` | Mọi assignment active (không Declined) đều Completed |
| *(rỗng)* | Còn đội khác đang xử lý (multi-team) |

**FE LEO/CM Web:** Nếu có `resolution_note` → highlight status **Resolved**; citizen nhận riêng `ReportStatusChanged`.

---

## 4. Phân biệt LEO vs CompanyManager

| Luồng | Ai gán team | Ai nhận 4 loại notification |
|-------|-------------|-------------------------------|
| Community cleanup (phường) | LEO | LEO đã bấm Assign |
| Company dispatch | LEO dispatch → CM assign team | **CM** đã bấm assign team công ty |

`AssignedById` luôn là user thực hiện thao tác assign — FE không cần suy luận thêm.

---

## 5. Checklist tích hợp FE

- [ ] Map 4 `type` mới vào notification center (Web LEO + Web CM).
- [ ] Deep link `referenceId` → report detail / progress board.
- [ ] Hiển thị `{team_name}` trong toast (đã render sẵn trong `message`).
- [ ] `CleanupTaskDeclined` → banner + CTA re-assign.
- [ ] `CleanupProgressUpdated` → refresh progress % trên board (poll hoặc SignalR).
- [ ] `CleanupTaskCompleted` + có `resolution_note` trong message → refresh report status Resolved.
- [ ] Không nhầm với `CleanupTaskAssigned` (gửi cho **member mobile**, không phải LEO).

---

## 6. Anti-spam (BR-NTF-003)

Mỗi loại notification được gom theo `NotificationType` — tối đa **20 notification cùng loại / user / ngày**. Progress update nhiều lần trong ngày vẫn gửi từng lần cho đến khi chạm ngưỡng.
