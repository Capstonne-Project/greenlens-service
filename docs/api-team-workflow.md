# Team Workflow — Full API Flow

> Base URL: `http://<host>/v1`
> Auth: tất cả API cần header `Authorization: Bearer <access_token>`
> Role: `Cleanup` hoặc `Inspector`

---

## Tổng quan flow

```
Officer: POST /v1/reports/{id}/assign
         → Report.Status: Verified → InProgress
         → Assignment.Status = Assigned (chờ team xác nhận)
         → Assignment.StartedAt = null
              │
              ├─ [Trong 2h] Team KHÔNG muốn làm
              │   PUT /v1/reports/{id}/decline
              │       → Assignment: Assigned → Declined
              │       → Nếu TẤT CẢ team Assigned|Declined → Report: InProgress → Verified
              │
              └─ Team muốn nhận việc
                  PUT /v1/reports/{id}/accept          ← bắt buộc trước khi làm
                      → Assignment: Assigned → InProgress
                      → Assignment.StartedAt = now
                      → Report.StartedAt = now (nếu là team đầu tiên accept)
                           │
                           ├─ [Tuỳ chọn, nhiều lần] Cập nhật tiến độ
                           │   PUT /v1/reports/{id}/progress  (multipart/form-data)
                           │       → Cập nhật ProgressPercent, ProgressNote
                           │       → Lưu ProgressUpdatedByUserId (từ token)
                           │       → Upload ảnh lên S3 (nếu có), trả về URLs
                           │       → Assignment.Status KHÔNG đổi
                           │
                           └─ Hoàn thành
                               PUT /v1/reports/{id}/resolve   (Cleanup, ≥ 2 ảnh after)
                               PUT /v1/reports/{id}/penalty   (Inspector, ban hành xử phạt)
                                   → Assignment: InProgress → Completed
                                   → Nếu TẤT CẢ non-Declined Completed
                                       → Report: InProgress → Resolved / PenaltyIssued
```

---

## State Machine

### Report.Status

| Từ | Đến | Trigger |
|---|---|---|
| `Verified` | `InProgress` | Officer gọi `/assign` |
| `InProgress` | `Verified` | Tất cả Assignment `Assigned` hoặc `Declined` |
| `InProgress` | `Resolved` | Tất cả non-Declined Assignment `Completed` (Cleanup) |
| `InProgress` | `PenaltyIssued` | Tất cả non-Declined Assignment `Completed` (Inspector) |
| `InProgress` | `ClosedNoViolation` | Inspector gọi `/close-no-violation` |
| `Resolved` / `PenaltyIssued` | `Closed` | Citizen gọi `/close` hoặc auto sau 7 ngày |
| `Resolved` | `InProgress` | Citizen gọi `/reopen` (tối đa 2 lần) |

### Assignment.Status

| Từ | Đến | Trigger | Điều kiện |
|---|---|---|---|
| *(mới tạo)* | `Assigned` | Officer gọi `/assign` | — |
| `Assigned` | `InProgress` | Team leader gọi `/accept` | Phải là leader |
| `Assigned` | `Declined` | Team leader gọi `/decline` | Trong 2h từ `assignedAt` |
| `InProgress` | `Completed` | Team leader gọi `/resolve` hoặc `/penalty` | Assignment đang `InProgress` |

---

## API Chi tiết

---

### 1. Xem danh sách task được giao

```
GET /v1/reports/my-assignments
```

**Query params:**

| Param | Type | Default | Mô tả |
|---|---|---|---|
| `page` | int | 1 | Trang hiện tại |
| `pageSize` | int | 20 | Số item mỗi trang |
| `assignmentStatus` | string | *(tất cả)* | Lọc: `Assigned`, `InProgress`, `Completed`, `Declined` |

**Response 200:**

```json
{
  "code": "SUCCESS",
  "status": 200,
  "data": {
    "items": [
      {
        "reportId": "aff788d8-5b32-4aef-92e3-7c20a1600c5f",
        "reportCode": "RPT-260519-1BD012",
        "assignmentId": "ad68cf46-32a6-44d7-ac9b-690858df1df6",
        "assignmentStatus": "Assigned",
        "categoryCode": "TRASH",
        "categoryName": "Rác thải",
        "severity": "High",
        "reportStatus": "InProgress",
        "latitude": 10.7769,
        "longitude": 106.7009,
        "address": "123 Nguyễn Huệ, Phường Bến Nghé",
        "wardCode": "26743",
        "note": "Khu vực tập kết rác lớn",
        "assignedAt": "2026-05-19T08:00:00Z",
        "startedAt": null,
        "completedAt": null,
        "slaResolveDueAt": "2026-05-24T08:00:00Z",
        "firstImageUrl": "https://cdn.example.com/reports/abc.jpg"
      }
    ],
    "totalCount": 1,
    "page": 1,
    "pageSize": 20
  }
}
```

> `startedAt = null` khi `assignmentStatus = Assigned` (chưa accept).

---

### 2. Xem lịch sử tiến độ của team

```
GET /v1/reports/my-progress
```

TeamId tự động lấy từ token — không cần truyền. Chỉ Team Leader mới gọi được.

**Query params:**

| Param | Type | Default | Mô tả |
|---|---|---|---|
| `page` | int | 1 | Trang hiện tại |
| `pageSize` | int | 20 | Số item mỗi trang |
| `assignmentStatus` | string | *(tất cả)* | Lọc theo trạng thái assignment |

**Response 200:**

```json
{
  "code": "SUCCESS",
  "status": 200,
  "data": {
    "items": [
      {
        "reportId": "aff788d8-5b32-4aef-92e3-7c20a1600c5f",
        "reportCode": "RPT-260519-1BD012",
        "assignmentId": "ad68cf46-32a6-44d7-ac9b-690858df1df6",
        "assignmentStatus": "InProgress",
        "reportStatus": "InProgress",
        "progressPercent": 60,
        "progressNote": "Đã dọn xong khu A, đang xử lý khu B",
        "progressUpdatedAt": "2026-05-19T10:00:00Z",
        "progressUpdatedByUserId": "9b23a1c4-1234-5678-abcd-000000000001",
        "assignedAt": "2026-05-19T08:00:00Z",
        "startedAt": "2026-05-19T08:30:00Z",
        "completedAt": null
      }
    ],
    "totalCount": 1,
    "page": 1,
    "pageSize": 20
  }
}
```

**Responses:**

| Code | Mô tả |
|---|---|
| 200 | Thành công |
| 422 `NOT_TEAM_LEADER` | User không phải leader của team nào |

---

### 3. Xem chi tiết report

```
GET /v1/reports/{id}
```

Trả về full thông tin report kèm media, assignments, lịch sử status.

---

### 4. Chấp nhận task *(bắt buộc trước khi làm việc)*

```
PUT /v1/reports/{id}/accept
```

TeamId tự động lấy từ token. Chỉ **Team Leader** được gọi.

**Body:** Không có body.

**Điều kiện:**
- User phải là leader của team được assign vào report
- `Assignment.Status == Assigned`

**Điều gì xảy ra:**
- `Assignment.Status: Assigned → InProgress`
- `Assignment.StartedAt = now`
- `Report.StartedAt = now` (nếu là team đầu tiên accept)

**Responses:**

| Code | Mô tả |
|---|---|
| 204 | Chấp nhận thành công |
| 422 `NOT_TEAM_LEADER` | User không phải leader |
| 422 `REPORT_NOT_FOUND` | Không tìm thấy report |
| 422 `ASSIGNMENT_NOT_FOUND` | Team không được assign vào report này |
| 422 `INVALID_STATUS_TRANSITION` | Assignment không ở trạng thái `Assigned` |

**Ví dụ request:**

```http
PUT /v1/reports/3fa85f64-5717-4562-b3fc-2c963f66afa6/accept
Authorization: Bearer eyJ...
```

---

### 5. Từ chối task *(chỉ trong 2h sau khi được giao)*

```
PUT /v1/reports/{id}/decline
```

**Body:**

```json
{
  "teamId": "4c951a07-8d5f-4036-8bee-89a65c1c21f5",
  "reason": "Đội không đủ nhân lực xử lý khu vực này trong tuần này"
}
```

| Field | Bắt buộc | Validation |
|---|---|---|
| `teamId` | ✓ | uuid |
| `reason` | ✓ | Tối thiểu 20 ký tự |

**Điều kiện:** `Assignment.Status == Assigned` (chưa accept mới được decline).

**Điều gì xảy ra:**
- `Assignment.Status: Assigned → Declined`
- Nếu **tất cả** assignment đều `Assigned` hoặc `Declined` → `Report.Status: InProgress → Verified`

**Responses:**

| Code | Mô tả |
|---|---|
| 204 | Từ chối thành công |
| 422 `INVALID_STATUS_TRANSITION` | Assignment không ở `Assigned` (đã accept rồi không decline được) |
| 422 `DECLINE_WINDOW_EXPIRED` | Đã quá 2h kể từ `assignedAt` |
| 422 `REASON_TOO_SHORT` | Lý do < 20 ký tự |
| 404 `ASSIGNMENT_NOT_FOUND` | Không tìm thấy assignment của team này |

---

### 6. Cập nhật tiến độ + upload ảnh *(tuỳ chọn, nhiều lần)*

```
PUT /v1/reports/{id}/progress
Content-Type: multipart/form-data
```

TeamId tự động lấy từ token. Chỉ **Team Leader** được gọi.

**Điều kiện:** `Assignment.Status == InProgress` (đã accept rồi mới cập nhật được).

**Form fields:**

| Field | Type | Bắt buộc | Validation |
|---|---|---|---|
| `progressPercent` | int | ✓ | 0 – 100 |
| `progressNote` | string | Không | Ghi chú tự do |
| `images` | file[] | Không | Tối đa 5 ảnh, mỗi ảnh ≤ 20MB (jpg, png, webp) |

**Điều gì xảy ra:**
- Cập nhật `ProgressPercent`, `ProgressNote` trong assignment
- Lưu `ProgressUpdatedByUserId` = userId từ token
- Upload từng ảnh lên S3 (nếu có)
- `Assignment.Status` **không thay đổi**

**Response 200:**

```json
{
  "code": "SUCCESS",
  "status": 200,
  "data": {
    "uploadedImageUrls": [
      "https://cdn.example.com/progress/img1.jpg",
      "https://cdn.example.com/progress/img2.jpg"
    ]
  }
}
```

> `uploadedImageUrls` là mảng rỗng `[]` nếu không upload ảnh.

**Responses:**

| Code | Mô tả |
|---|---|
| 200 | Cập nhật thành công |
| 413 `FILE_TOO_LARGE` | Một file > 20MB |
| 422 `NOT_TEAM_LEADER` | User không phải leader |
| 422 `ASSIGNMENT_NOT_FOUND` | Team không được assign vào report |
| 422 `ASSIGNMENT_NOT_IN_PROGRESS` | Assignment chưa được Accept (vẫn `Assigned`) |
| 422 `INVALID_PROGRESS_PERCENT` | Percent ngoài 0–100 |

**Ví dụ — chỉ cập nhật %:**

```http
PUT /v1/reports/3fa85f64.../progress
Authorization: Bearer eyJ...
Content-Type: multipart/form-data

progressPercent=75
progressNote=Đã dọn xong 3/4 khu vực
```

**Ví dụ — kèm ảnh:**

```http
PUT /v1/reports/3fa85f64.../progress
Authorization: Bearer eyJ...
Content-Type: multipart/form-data

progressPercent=50
progressNote=Đang xử lý khu B
images=@photo1.jpg
images=@photo2.jpg
```

---

### 7. Hoàn thành — submit kết quả (Cleanup Team)

```
PUT /v1/reports/{id}/resolve
```

**Body:**

```json
{
  "teamId": "4c951a07-8d5f-4036-8bee-89a65c1c21f5",
  "afterImageUrls": [
    "https://cdn.example.com/after/img1.jpg",
    "https://cdn.example.com/after/img2.jpg"
  ]
}
```

| Field | Bắt buộc | Validation |
|---|---|---|
| `teamId` | ✓ | uuid |
| `afterImageUrls` | ✓ | Tối thiểu **2 URL** ảnh after |

> **Lưu ý:** Upload ảnh after trước bằng `PUT /progress` (kèm `images`), lấy URL trả về rồi mới gọi `/resolve`.

**Điều gì xảy ra:**
- `Assignment.Status: InProgress → Completed`
- Nếu **tất cả** non-Declined assignment `Completed` → `Report.Status: InProgress → Resolved`

**Responses:**

| Code | Mô tả |
|---|---|
| 204 | Hoàn thành thành công |
| 422 `INSUFFICIENT_AFTER_IMAGES` | Thiếu ảnh after (cần ≥ 2) |
| 422 `INVALID_STATUS_TRANSITION` | Assignment không ở `InProgress` |
| 404 `ASSIGNMENT_NOT_FOUND` | Không tìm thấy assignment của team này |

---

### 8. Hoàn thành — ban hành xử phạt (Inspector Team)

```
PUT /v1/reports/{id}/penalty
```

**Body:**

```json
{
  "teamId": "4c951a07-8d5f-4036-8bee-89a65c1c21f5"
}
```

**Điều gì xảy ra:**
- `Assignment.Status: InProgress → Completed`
- Nếu **tất cả** non-Declined assignment `Completed` → `Report.Status: InProgress → PenaltyIssued`

**Responses:**

| Code | Mô tả |
|---|---|
| 204 | Thành công |
| 422 `INVALID_STATUS_TRANSITION` | Assignment không `InProgress` |
| 404 `ASSIGNMENT_NOT_FOUND` | Không tìm thấy assignment |

---

### 9. Đóng — không vi phạm (Inspector Team)

```
PUT /v1/reports/{id}/close-no-violation
```

**Body:**

```json
{
  "reason": "Kiểm tra thực địa không phát hiện vi phạm, khu vực đã được dọn dẹp trước khi đến"
}
```

| Field | Bắt buộc | Validation |
|---|---|---|
| `reason` | ✓ | Tối thiểu 50 ký tự |

**Điều gì xảy ra:** `Report.Status: InProgress → ClosedNoViolation`

**Responses:**

| Code | Mô tả |
|---|---|
| 204 | Thành công |
| 422 `REASON_TOO_SHORT_50` | Lý do < 50 ký tự |
| 422 `INVALID_STATUS_TRANSITION` | Report không ở `InProgress` |

---

## Tóm tắt thứ tự gọi API — Happy Path

```
1. GET  /v1/reports/my-assignments          → xem task mới (status: Assigned)
2. GET  /v1/reports/{id}                    → xem chi tiết
3. PUT  /v1/reports/{id}/accept             → chấp nhận (Assignment: Assigned → InProgress)
4. PUT  /v1/reports/{id}/progress           → cập nhật tiến độ + ảnh (nhiều lần, tuỳ chọn)
5. PUT  /v1/reports/{id}/resolve            → hoàn thành (Cleanup) với ≥ 2 ảnh after
   hoặc
   PUT  /v1/reports/{id}/penalty            → xử phạt (Inspector)
6. GET  /v1/reports/my-progress             → xem lịch sử tất cả assignment đã làm
```

---

## Upload ảnh after trước khi resolve

Ảnh after phải upload qua `PUT /progress` trước, lấy URL rồi mới gọi `/resolve`:

```
# Bước 1: upload 2 ảnh after (kèm vào lần cập nhật tiến độ cuối)
PUT /v1/reports/{id}/progress
  progressPercent=100
  progressNote=Hoàn thành
  images=@after1.jpg    → url1
  images=@after2.jpg    → url2

# Bước 2: resolve với URL lấy được
PUT /v1/reports/{id}/resolve
{
  "teamId": "...",
  "afterImageUrls": ["url1", "url2"]
}
```

---

## Error codes tổng hợp

| Code | HTTP | Mô tả |
|---|---|---|
| `REPORT_NOT_FOUND` | 404 | Không tìm thấy report |
| `ASSIGNMENT_NOT_FOUND` | 404 | Không tìm thấy assignment của team |
| `NOT_TEAM_LEADER` | 422 | User không phải leader của team nào |
| `INVALID_STATUS_TRANSITION` | 422 | Sai trạng thái để thực hiện hành động |
| `DECLINE_WINDOW_EXPIRED` | 422 | Quá 2h kể từ khi được giao |
| `REASON_TOO_SHORT` | 422 | Lý do từ chối < 20 ký tự |
| `REASON_TOO_SHORT_50` | 422 | Lý do đóng không vi phạm < 50 ký tự |
| `INSUFFICIENT_AFTER_IMAGES` | 422 | Ảnh after < 2 tấm |
| `ASSIGNMENT_NOT_IN_PROGRESS` | 422 | Assignment chưa Accept hoặc đã Completed |
| `INVALID_PROGRESS_PERCENT` | 422 | Percent không nằm trong 0–100 |
| `FILE_TOO_LARGE` | 413 | File upload > 20MB |
