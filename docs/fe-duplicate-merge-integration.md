# Mobile App — Duplicate Report Merge Integration Guide

> **Backend branch:** `develop`
> **Ngày cập nhật:** 2026-07-26
> **Liên quan:** BR-REP-031, BR-REP-032, BR-NTF-002
> **Request liên quan:** [`fe-be-request-merged-report-images.md`](./fe-be-request-merged-report-images.md)

---

## 1. Thay đổi API: `GET /v1/reports/my`

### Field merge + thumbnail

| Field | Type | Nullable | Mô tả |
|-------|------|----------|-------|
| `imageUrl` | `string?` | ✅ | Ảnh đại diện. Với `status=Duplicate`, vẫn trả thumb sau khi media đã reassign sang primary (project theo `ReportMedia.SourceReportId`). |
| `mergedIntoPrimaryReportId` | `Guid?` | ✅ | ID của báo cáo gốc mà báo cáo này đã được gộp vào. `null` nếu không phải duplicate. |
| `mergedIntoPrimaryReportCode` | `string?` | ✅ | Mã hiển thị của báo cáo gốc (e.g. `RPT-2026-0045`). `null` nếu không phải duplicate. |

### Response mẫu

```json
{
  "data": {
    "items": [
      {
        "id": "d1a2b3c4-...",
        "code": "RPT-2026-0048",
        "categoryName": "Ô nhiễm rác thải",
        "severity": "Medium",
        "status": "Duplicate",
        "address": "125 Nguyễn Huệ, P. Bến Thành, Q.1",
        "createdAt": "2026-07-22T14:30:00Z",
        "resolvedAt": null,
        "closedAt": null,
        "imageUrl": "https://cdn.example.com/img/thumb_abc.jpg",
        "mergedIntoPrimaryReportId": "a1b2c3d4-...",
        "mergedIntoPrimaryReportCode": "RPT-2026-0045"
      },
      {
        "id": "e5f6g7h8-...",
        "code": "RPT-2026-0050",
        "categoryName": "Ô nhiễm nước",
        "severity": "High",
        "status": "Submitted",
        "address": "456 Trần Hưng Đạo, Q.5",
        "createdAt": "2026-07-22T16:00:00Z",
        "resolvedAt": null,
        "closedAt": null,
        "imageUrl": "https://cdn.example.com/img/thumb_xyz.jpg",
        "mergedIntoPrimaryReportId": null,
        "mergedIntoPrimaryReportCode": null
      }
    ],
    "pagination": { "page": 1, "pageSize": 20, "totalCount": 2, "totalPages": 1 }
  }
}
```

### Hướng dẫn UI

```
┌──────────────────────────────────────────────────────────────┐
│  📋 Báo cáo của tôi                                         │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  RPT-2026-0050  |  🟠 Cao  |  Ô nhiễm nước                  │
│  456 Trần Hưng Đạo, Q.5                                     │
│  Status: Submitted                                           │
│                                                              │
│  ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─  │
│                                                              │
│  RPT-2026-0048  |  🟡 TB   |  Ô nhiễm rác thải              │
│  125 Nguyễn Huệ, P. Bến Thành                               │
│  Status: Đã gộp                                              │
│  ↗ Theo dõi tiến độ tại RPT-2026-0045  ← CLICKABLE LINK     │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

**Logic hiển thị:**

```
if (item.status == "Duplicate" && item.mergedIntoPrimaryReportId != null) {
    // Hiện badge "Đã gộp" thay vì "Duplicate"
    // Hiện link "Theo dõi tiến độ tại {mergedIntoPrimaryReportCode}"
    // Khi tap link → navigate to ReportDetail(mergedIntoPrimaryReportId)
}
```

---

## 2. Thay đổi API: `GET /v1/reports/{id}` (primary detail)

### Field mới trên `ReportDetailResponse`

| Field | Type | Nullable | Mô tả |
|-------|------|----------|-------|
| `mergedIntoPrimaryReportId` | `Guid?` | ✅ | Nếu báo cáo này là Duplicate → ID primary. |
| `mergedIntoPrimaryReportCode` | `string?` | ✅ | Mã primary tương ứng. |
| `mergedReports` | `MergedReportItem[]` | ✅ | Các báo cáo đã gộp **vào** primary này (rỗng/`[]` nếu không có). |

### `MergedReportItem`

| Field | Type | Nullable | Mô tả |
|-------|------|----------|-------|
| `id` | `Guid` | ❌ | ID báo cáo Duplicate |
| `code` | `string` | ❌ | Mã hiển thị |
| `imageUrl` | `string?` | ✅ | Thumb của report con — lấy từ media trên primary có `sourceReportId = id` |
| `createdAt` | `datetime` | ❌ | Thời điểm tạo |
| `status` | `string` | ❌ | Thường `Duplicate` |

**Cách lấy `imageUrl` (BE):** lúc `confirm-duplicate`, `ReportMedia.ReassignToReport` set `SourceReportId` = id report con rồi chuyển `ReportId` sang primary. Projection đọc media primary theo `SourceReportId`.

> **Lưu ý:** merge **trước** migration `source_report_id` không có origin → `imageUrl` có thể null cho các child cũ.

### Response mẫu (primary)

```json
{
  "data": {
    "id": "a1b2c3d4-0000-0000-0000-000000000001",
    "code": "RPT-2026-0045",
    "status": "InProgress",
    "reporterCount": 3,
    "mergedIntoPrimaryReportId": null,
    "mergedIntoPrimaryReportCode": null,
    "mergedReports": [
      {
        "id": "d1a2b3c4-0000-0000-0000-000000000048",
        "code": "RPT-2026-0048",
        "imageUrl": "https://cdn.example.com/img/thumb_child_48.jpg",
        "createdAt": "2026-07-22T14:30:00Z",
        "status": "Duplicate"
      }
    ]
  }
}
```

---

## 3. Thay đổi Notification

### Trước (cũ)

```json
{
  "type": "ReportStatusChanged",
  "title": "Báo cáo được gộp",
  "message": "Báo cáo của bạn đã được xác định là trùng lặp và gộp vào một báo cáo hiện có. Cảm ơn đóng góp của bạn!",
  "referenceId": "d1a2b3c4-..."
}
```

> `referenceId` trỏ đến báo cáo **bị duplicate** (báo cáo của citizen)

### Sau (mới)

```json
{
  "type": "ReportStatusChanged",
  "title": "Báo cáo được gộp",
  "message": "Báo cáo của bạn đã được xác định là trùng lặp và gộp vào báo cáo RPT-2026-0045. Bạn có thể theo dõi tiến độ xử lý tại báo cáo gốc. Cảm ơn đóng góp của bạn!",
  "referenceId": "a1b2c3d4-..."
}
```

> `referenceId` giờ trỏ đến **báo cáo GỐC (primary)** để mobile deep-link trực tiếp.

### ⚠️ Breaking change: `referenceId`

| | Trước | Sau |
|--|-------|-----|
| `referenceId` trỏ đến | Báo cáo bị duplicate (của citizen) | **Báo cáo gốc (primary)** |

**Lý do:** Mobile cần deep-link vào **báo cáo gốc** để citizen theo dõi tiến độ xử lý, không phải báo cáo đã bị merge.

### Hướng dẫn xử lý notification tap

```
onNotificationTap(notification) {
    if (notification.type == "ReportStatusChanged") {
        // referenceId = primary report ID
        navigateTo(ReportDetailScreen, { reportId: notification.referenceId })
    }
}
```

> **Lưu ý:** Citizen có thể xem được báo cáo gốc vì `GET /v1/reports/{id}` cho phép tất cả user đã đăng nhập xem chi tiết.

---

## 4. Checklist tích hợp

- [ ] Cập nhật model `MyReportItem` (`mergedIntoPrimaryReportId`, `mergedIntoPrimaryReportCode`, `imageUrl` vẫn có khi Duplicate)
- [ ] Cập nhật model `ReportDetail` thêm `mergedReports[]` + `mergedIntoPrimary*`
- [ ] UI section “báo cáo đã gộp” trên primary detail dùng `mergedReports[].imageUrl`
- [ ] Xử lý UI khi `status == "Duplicate"` → hiện badge "Đã gộp" + link báo cáo gốc
- [ ] Cập nhật notification handler: `referenceId` giờ trỏ đến **primary report**
- [ ] Deep-link notification tap → `ReportDetailScreen(referenceId)`
- [ ] Test: submit 2 báo cáo cùng vị trí → confirm duplicate → verify notification + my reports + primary `mergedReports`
