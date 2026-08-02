# FE Update — Duplicate/Recurrence media list + new detail API (2026-08-02)

> **Prefix:** `/v1` · **Envelope:** `{ code, message, status, data }`
> **Controller:** `ReportsController` · **Roles:** `LEO`, `DEO`, `Admin`

---

## 1. Fix — `media[]` giờ trả đủ ảnh, không còn chỉ 1 ảnh đầu

Áp dụng cho 2 API list đã có, **không đổi tên endpoint, không đổi shape response** — chỉ thay đổi số lượng phần tử trong `media`:

- `GET /v1/reports/duplicate-candidates`
- `GET /v1/reports/violation-recurrence-candidates`

**Trước:** `media` (và `primary.media` / `priorClosedReport.media`) chỉ trả về 1 ảnh đầu tiên.
**Sau:** trả về **toàn bộ** ảnh/video citizen đã submit cho từng báo cáo, sắp theo `uploadedAt` tăng dần.

**FE cần làm:**

- Nếu UI đang giả định `media[0]` là ảnh duy nhất → đổi sang render **carousel/gallery** cho toàn bộ `media[]`.
- Không cần đổi field name, không cần đổi query param.

---

## 2. API mới — Chi tiết so sánh báo cáo nghi ngờ trùng lặp

### `GET /v1/reports/{id}/duplicate-candidate-detail`

**Auth:** Bearer · Roles: `LEO`, `DEO`, `Admin` · **BR-REP-031 / BR-REP-032**

`{id}` = báo cáo đang bị flag `isPossibleDuplicate` (candidate), lấy từ `items[].id` của `GET /v1/reports/duplicate-candidates`.

**Response 200 `data`:**

```json
{
  "report": {
    "id": "49b8f7c9-4e19-460f-ac7d-f139d2d454e7",
    "code": "RPT-260714-40DD21",
    "status": "Submitted",
    "categoryCode": "household_waste",
    "categoryName": "Rác thải sinh hoạt",
    "severity": "Medium",
    "description": "Mô tả ô nhiễm",
    "latitude": 10.77691,
    "longitude": 106.70091,
    "address": "Test address HCM",
    "createdAt": "2026-07-14T18:02:40Z",
    "media": [
      { "id": "...", "url": "...", "thumbnailUrl": "...", "type": "Image", "uploadedAt": "2026-07-14T18:01:00Z" }
    ]
  },
  "primaryReport": {
    "id": "f22137c2-7977-42ed-a0e4-4d52e7185954",
    "code": "RPT-260714-FEE90F",
    "status": "Verified",
    "categoryCode": "household_waste",
    "categoryName": "Rác thải sinh hoạt",
    "severity": "Medium",
    "description": "...",
    "latitude": 10.77690,
    "longitude": 106.70095,
    "address": "Test address HCM",
    "createdAt": "2026-07-14T18:00:38Z",
    "media": [ ... ]
  },
  "duplicateDetectionSource": "geo_category_ai",
  "aiSimilarityScore": 0.8135,
  "distanceMeters": 4.2,
  "hoursSincePrimaryCreated": 0.04
}
```

**Dùng để:** thay thế việc FE phải gọi `GET /v1/reports/{id}` **2 lần** (candidate + primary) để dựng màn hình so sánh side-by-side — giờ 1 API trả đủ cả 2 bên kèm `media[]` đầy đủ, khoảng cách (m) và chênh lệch thời gian tạo (giờ).

**Error codes:**

| `code` | HTTP | Khi nào |
|---|---|---|
| `REPORT_NOT_FOUND` | 404 | `{id}` hoặc báo cáo gốc không tồn tại |
| `PRIMARY_REPORT_NOT_FOUND` | 404 | `possibleDuplicateOfReportId` trỏ tới report đã bị xoá |
| `NOT_POSSIBLE_DUPLICATE` | 422 | Báo cáo không còn cờ `isPossibleDuplicate` (đã bị dismiss/confirm) |

Response tương tự bên `violation-recurrence-comparison` (`GET /v1/reports/{id}/violation-recurrence-comparison`) đã có sẵn — cùng pattern để FE tái dùng component so sánh side-by-side.
