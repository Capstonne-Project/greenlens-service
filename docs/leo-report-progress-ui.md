# LEO — Màn hình xem tiến trình báo cáo

> **Endpoint đề xuất:** `GET /v1/reports/{id}/progress`
> **Role:** Officer (LEO — Local Environmental Officer)
> **Mục đích:** LEO theo dõi tiến trình xử lý của các team sau khi đã phân công

> **Tham chiếu:** [`fe-leo-duplicate-detection-guide.md`](./fe-leo-duplicate-detection-guide.md) (queue nghi ngờ trùng lặp)

---

## Luồng dẫn đến màn hình này

```
LEO Dashboard
└── Danh sách báo cáo (GET /v1/reports/queue)
    └── [Bấm vào báo cáo đang IN_PROGRESS]
        └── ★ Màn hình Chi tiết tiến trình (GET /v1/reports/{id}/progress)
```

---

## Wireframe — Màn hình chính

```
╔═════════════════════════════════════════════════════════════════════╗
║  ← Quay lại    RPT-260520-A3F9K2              🔵 ĐANG XỬ LÝ       ║
╚═════════════════════════════════════════════════════════════════════╝

┌─────────────────────────────────────────────────────────────────────┐
│  THÔNG TIN BÁO CÁO                                                  │
├─────────────────────────────────────────────────────────────────────┤
│  🗑️  Rác thải sinh hoạt              Mức độ:  🔴 CAO               │
│                                                                     │
│  📍  12 Nguyễn Văn Linh, P.Bình Thuận, Q.7, TP.HCM                │
│                                                                     │
│  👤  Người báo cáo: Ẩn danh                                        │
│  🕐  Gửi lúc: 20/05/2026 08:15                                     │
│  📋  Mô tả: Bãi rác tự phát ven kênh, mùi hôi nặng                │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│  SLA & TỔNG QUAN TIẾN ĐỘ                                           │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ⏰  Hạn xử lý:  25/05/2026 08:15       🔴  Còn 14 giờ 32 phút   │
│                                                                     │
│  Tiến độ tổng thể                                    2 / 3 teams   │
│  ████████████████████████░░░░░░░░░░  67%                           │
│                                                                     │
│  ┌──────────────┬──────────────┬──────────────┬──────────────┐     │
│  │ ✅ Hoàn thành│ 🔄 Đang làm  │ ⏳ Chờ nhận  │ ❌ Từ chối  │     │
│  │      1       │      1       │      1       │      0       │     │
│  └──────────────┴──────────────┴──────────────┴──────────────┘     │
│                                                                     │
│  Bắt đầu: 20/05/2026 10:05                                         │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│  PHÂN CÔNG TEAMS                          [+ Phân công thêm team]  │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌───────────────────────────────────────────── ✅ HOÀN THÀNH ───┐ │
│  │  Team 1                                                        │ │
│  │  🧹 Đội vệ sinh môi trường số 3                               │ │
│  │  👤 Leader: Nguyễn Văn A                                      │ │
│  │                                                                │ │
│  │  📅 Nhận: 20/05 09:10  →  ✅ Xong: 21/05 14:30              │ │
│  │                                                                │ │
│  │  Tiến độ:  ████████████████████████████████████  100%        │ │
│  │                                                                │ │
│  │  📸 Ảnh nghiệm thu (after)                                   │ │
│  │  ┌─────────┐  ┌─────────┐  ┌─────────┐                      │ │
│  │  │  [img1] │  │  [img2] │  │  [img3] │                      │ │
│  │  │ 21/05   │  │ 21/05   │  │ 21/05   │                      │ │
│  │  │ 14:20   │  │ 14:22   │  │ 14:28   │                      │ │
│  │  └─────────┘  └─────────┘  └─────────┘                      │ │
│  └────────────────────────────────────────────────────────────────┘ │
│                                                                     │
│  ┌───────────────────────────────────────────── 🔄 ĐANG LÀM ────┐ │
│  │  Team 2                                                        │ │
│  │  🧹 Đội vệ sinh môi trường số 7                               │ │
│  │  👤 Leader: Trần Thị B                                        │ │
│  │                                              [↩ Đổi team]     │ │
│  │  📅 Nhận: 20/05 09:10  →  Chấp nhận: 20/05 10:05            │ │
│  │                                                                │ │
│  │  Cập nhật lần cuối: 21/05 11:20 — bởi Trần Thị B            │ │
│  │  Tiến độ:  ██████████████████████░░░░░░░░░░░░  60%           │ │
│  │  💬 "Đã dọn sạch khu vực A, đang xử lý khu B"               │ │
│  │                                                                │ │
│  │  📸 Ảnh tiến trình                                           │ │
│  │  ┌─────────┐  ┌─────────┐                                    │ │
│  │  │  [img1] │  │  [img2] │                                    │ │
│  │  │ 21/05   │  │ 21/05   │                                    │ │
│  │  │ 09:30   │  │ 11:15   │                                    │ │
│  │  └─────────┘  └─────────┘                                    │ │
│  └────────────────────────────────────────────────────────────────┘ │
│                                                                     │
│  ┌───────────────────────────────────────────── ⏳ CHỜ NHẬN ────┐ │
│  │  Team 3                                                        │ │
│  │  🧹 Đội vệ sinh môi trường số 5                               │ │
│  │  👤 Leader: Lê Văn C                                          │ │
│  │                                              [↩ Đổi team]     │ │
│  │  📅 Phân công: 20/05 09:10    Chưa chấp nhận                 │ │
│  │                                                                │ │
│  │  ⚠️  Còn 1 giờ 28 phút để chấp nhận (hết hạn 2h)           │ │
│  └────────────────────────────────────────────────────────────────┘ │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│  LỊCH SỬ TRẠNG THÁI                                                 │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ✅  21/05 14:30  Team số 3 hoàn thành nhiệm vụ   (Nguyễn Văn A)  │
│  📝  21/05 11:20  Team số 7 cập nhật tiến độ 60%  (Trần Thị B)    │
│  ▶️   20/05 10:05  Team số 7 chấp nhận nhiệm vụ   (Trần Thị B)    │
│  📝  20/05 09:30  Team số 3 cập nhật tiến độ 30%  (Nguyễn Văn A)  │
│  👮  20/05 09:10  LEO phân công 3 teams            (leo.BinhThuan) │
│  📨  20/05 08:50  DEO chuyển về phường             (deo.Q7)        │
│  ✔️   20/05 08:20  DEO xác minh báo cáo            (deo.Q7)        │
│  📩  20/05 08:15  Công dân gửi báo cáo             (Ẩn danh)       │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Trường hợp đặc biệt: Team từ chối

```
  ┌───────────────────────────────────────────── ❌ TỪ CHỐI ─────┐
  │  Team 4 (đã bị thay thế)                                      │
  │  🧹 Đội vệ sinh số 2                                          │
  │  👤 Leader: Phạm Văn D                                        │
  │                                                               │
  │  📅 Phân công: 20/05 09:10  →  Từ chối: 20/05 10:45         │
  │  💬 Lý do: "Đội đang xử lý sự cố khẩn cấp tại KCN Tân Tạo"  │
  └───────────────────────────────────────────────────────────────┘
```

---

## Trường hợp đặc biệt: SLA đã breach

```
┌─────────────────────────────────────────────────────────────────────┐
│  SLA & TỔNG QUAN TIẾN ĐỘ                                           │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ⏰  Hạn xử lý:  25/05/2026 08:15     🚨  QUÁ HẠN 3 giờ 15 phút  │
│  ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓  [SLA BREACHED]                 │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Trường hợp: Team InspectionTeam (kiểm tra vi phạm)

```
  ┌───────────────────────────────────────────── 🔄 ĐANG KIỂM TRA ┐
  │  Team 5  (Inspection)                                          │
  │  🔍 Đội kiểm tra môi trường số 2                              │
  │  👤 Leader: Hoàng Minh E                       [↩ Đổi team]  │
  │                                                                │
  │  📅 Nhận: 20/05 09:10  →  Chấp nhận: 20/05 09:40            │
  │                                                                │
  │  Loại: Kiểm tra vi phạm hành chính                           │
  │  Kết quả sẽ là:  [Xử phạt vi phạm]  hoặc  [Không vi phạm]   │
  └────────────────────────────────────────────────────────────────┘
```

---

## Cấu trúc API Response

```
GET /v1/reports/{id}/progress

Response: ReportProgressResponse
{
  // ── Report Header ──────────────────────────────────────────────
  "reportId":      "3fa85f64-...",
  "code":          "RPT-260520-A3F9K2",
  "status":        "InProgress",
  "severity":      "High",
  "categoryName":  "Rác thải sinh hoạt",
  "address":       "12 Nguyễn Văn Linh, P.Bình Thuận, Q.7",
  "wardCode":      "27337",
  "description":   "Bãi rác tự phát ven kênh...",

  // ── SLA ────────────────────────────────────────────────────────
  "sla": {
    "resolveDueAt":    "2026-05-25T08:15:00Z",
    "hoursRemaining":  14,                      // âm = đã breach
    "isBreached":      false,
    "severityLabel":   "High (5 ngày)"
  },

  // ── Aggregate ──────────────────────────────────────────────────
  "summary": {
    "totalTeams":              3,
    "acceptedTeams":           2,               // đang InProgress
    "completedTeams":          1,
    "declinedTeams":           0,
    "pendingTeams":            1,               // còn Assigned chưa accept
    "overallProgressPercent":  67,
    "startedAt":               "2026-05-20T10:05:00Z"
  },

  // ── Per-Team ───────────────────────────────────────────────────
  "assignments": [
    {
      "assignmentId":   "uuid-...",
      "teamId":         "uuid-...",
      "teamName":       "Đội vệ sinh môi trường số 3",
      "teamType":       "Cleanup",
      "teamLeaderName": "Nguyễn Văn A",

      "status":         "Completed",
      "assignedAt":     "2026-05-20T09:10:00Z",
      "acceptedAt":     "2026-05-20T09:30:00Z",
      "completedAt":    "2026-05-21T14:30:00Z",
      "declineReason":  null,

      "progressPercent": 100,
      "progressNote":    null,
      "progressUpdatedAt": "2026-05-21T14:30:00Z",

      "progressImages": [],
      "afterImages": [
        { "url": "https://...", "uploadedAt": "2026-05-21T14:20:00Z" },
        { "url": "https://...", "uploadedAt": "2026-05-21T14:22:00Z" },
        { "url": "https://...", "uploadedAt": "2026-05-21T14:28:00Z" }
      ],
      "penaltyIssued": null
    },
    {
      "assignmentId":   "uuid-...",
      "teamId":         "uuid-...",
      "teamName":       "Đội vệ sinh môi trường số 7",
      "teamType":       "Cleanup",
      "teamLeaderName": "Trần Thị B",

      "status":          "InProgress",
      "assignedAt":      "2026-05-20T09:10:00Z",
      "acceptedAt":      "2026-05-20T10:05:00Z",
      "completedAt":     null,
      "declineReason":   null,

      "progressPercent":   60,
      "progressNote":      "Đã dọn sạch khu vực A, đang xử lý khu B",
      "progressUpdatedAt": "2026-05-21T11:20:00Z",

      "progressImages": [
        { "url": "https://...", "uploadedAt": "2026-05-21T09:30:00Z" },
        { "url": "https://...", "uploadedAt": "2026-05-21T11:15:00Z" }
      ],
      "afterImages":    [],
      "penaltyIssued":  null
    },
    {
      "assignmentId":   "uuid-...",
      "teamId":         "uuid-...",
      "teamName":       "Đội vệ sinh môi trường số 5",
      "teamType":       "Cleanup",
      "teamLeaderName": "Lê Văn C",

      "status":         "Assigned",
      "assignedAt":     "2026-05-20T09:10:00Z",
      "acceptedAt":     null,
      "completedAt":    null,
      "declineReason":  null,

      "progressPercent":   0,
      "progressNote":      null,
      "progressUpdatedAt": null,

      "progressImages": [],
      "afterImages":    [],
      "penaltyIssued":  null
    }
  ],

  // ── Status History ─────────────────────────────────────────────
  "statusHistory": [
    {
      "fromStatus":    "InProgress",
      "toStatus":      "InProgress",
      "changedAt":     "2026-05-21T14:30:00Z",
      "changedByName": "Nguyễn Văn A",
      "note":          "Team số 3 hoàn thành nhiệm vụ"
    },
    {
      "fromStatus":    "Dispatched",
      "toStatus":      "InProgress",
      "changedAt":     "2026-05-20T09:10:00Z",
      "changedByName": "leo.BinhThuan@greenlens.dev",
      "note":          "Phân công 3 teams xử lý"
    },
    {
      "fromStatus":    "Verified",
      "toStatus":      "Dispatched",
      "changedAt":     "2026-05-20T08:50:00Z",
      "changedByName": "deo.Q7@greenlens.dev",
      "note":          "Chuyển về P.Bình Thuận xử lý"
    },
    {
      "fromStatus":    "Submitted",
      "toStatus":      "Verified",
      "changedAt":     "2026-05-20T08:20:00Z",
      "changedByName": "deo.Q7@greenlens.dev",
      "note":          null
    },
    {
      "fromStatus":    null,
      "toStatus":      "Submitted",
      "changedAt":     "2026-05-20T08:15:00Z",
      "changedByName": "Ẩn danh",
      "note":          null
    }
  ]
}
```

---

## Ánh xạ UI → API field

| Thành phần UI                       | Field trong Response                          |
| ----------------------------------- | --------------------------------------------- |
| `RPT-260520-A3F9K2`                 | `code`                                        |
| Badge trạng thái `🔵 ĐANG XỬ LÝ`    | `status`                                      |
| `🗑️ Rác thải sinh hoạt`             | `categoryName`                                |
| `🔴 CAO`                            | `severity`                                    |
| Địa chỉ                             | `address`                                     |
| Hạn xử lý                           | `sla.resolveDueAt`                            |
| `Còn 14 giờ 32 phút` / `Quá hạn Xh` | `sla.hoursRemaining` (tính phía frontend)     |
| Màu đỏ/vàng/xanh SLA                | `sla.isBreached`                              |
| Progress bar tổng `67%`             | `summary.overallProgressPercent`              |
| `2 / 3 teams`                       | `summary.completedTeams / summary.totalTeams` |
| 4 ô thống kê teams                  | `summary.*Teams`                              |
| Badge `✅ / 🔄 / ⏳ / ❌` của team  | `assignments[].status`                        |
| Tên team                            | `assignments[].teamName`                      |
| `Leader: Nguyễn Văn A`              | `assignments[].teamLeaderName`                |
| Thời gian nhận / hoàn thành         | `assignments[].assignedAt / completedAt`      |
| Progress bar per team               | `assignments[].progressPercent`               |
| Ghi chú tiến độ                     | `assignments[].progressNote`                  |
| Ảnh tiến trình                      | `assignments[].progressImages[]`              |
| Ảnh nghiệm thu                      | `assignments[].afterImages[]`                 |
| Cảnh báo 2h còn lại                 | tính từ `assignments[].assignedAt + 2h`       |
| Lý do từ chối                       | `assignments[].declineReason`                 |
| Dòng lịch sử                        | `statusHistory[]`                             |

---

## Domain thay đổi cần bổ sung

Để response trên hoạt động đúng, cần thêm `MediaPhase` vào domain:

```
Domain/Enums/MediaPhase.cs
└── enum MediaPhase { Before, Progress, After }

Domain/Entities/ReportMedia.cs
└── + Phase: MediaPhase   ← phân biệt ảnh trước/trong/sau

UpdateProgressCommandHandler.cs
└── gán Phase = MediaPhase.Progress khi upload ảnh tiến trình

ResolveReportCommandHandler.cs
└── gán Phase = MediaPhase.After khi upload ảnh nghiệm thu
```

---

_Tài liệu này mô tả UI và API response cho LEO xem tiến trình báo cáo._
_Endpoint: `GET /v1/reports/{id}/progress` | Query: `GetReportProgressQuery`_
