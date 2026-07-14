# FE — Comments API Guide (Citizen + LEO)

> **Prefix:** `/v1` · **Envelope:** `{ code, message, status, data }`  
> **Business rules:** BR-CMT-001..004, BR-REP-012  
> **Liên quan:** [`fe-citizen-map-report-detail.md`](./fe-citizen-map-report-detail.md)

---

## 1. Tổng quan

| Endpoint | Method | Auth | Mô tả |
|----------|--------|------|--------|
| `/v1/reports/{reportId}/comments` | GET | Bearer | Danh sách bình luận (phân trang) |
| `/v1/reports/{reportId}/comments` | POST | Bearer (Citizen+) | Thêm bình luận |
| `/v1/comments/{commentId}` | PUT | Bearer (tác giả) | Sửa trong 15 phút |
| `/v1/comments/{commentId}` | DELETE | Bearer (tác giả) | Xóa trong 15 phút |
| `/v1/comments/{commentId}/hide` | POST | LEO, DEO, Admin | Ẩn bình luận vi phạm |
| `/v1/media/comments/images` | POST | Bearer | Upload ảnh đính kèm (max 5MB) |

**Luồng đề xuất (Citizen):**

1. `POST /v1/media/comments/images` — upload 0–2 ảnh, lấy `url`, `mimeType`, `sizeBytes`
2. `POST /v1/reports/{reportId}/comments` — gửi `content` + `images[]`
3. `GET /v1/reports/{reportId}/comments` — refresh danh sách

---

## 2. Upload ảnh bình luận

`POST /v1/media/comments/images` · `multipart/form-data`

| Field | Rule |
|-------|------|
| `file` | jpg / png / webp / heic, **max 5MB** |

**Response 200 `data`:**

```json
{
  "url": "https://pub-xxx.r2.dev/comments/images/abc.png",
  "key": "comments/images/abc.png",
  "message": "Tải ảnh bình luận thành công.",
  "mimeType": "image/png",
  "sizeBytes": 102400
}
```

---

## 3. Thêm bình luận

`POST /v1/reports/{reportId}/comments`

**Body:**

```json
{
  "content": "Khu vực này thường xuyên đổ rác buổi tối.",
  "images": [
    {
      "url": "https://pub-xxx.r2.dev/comments/images/abc.png",
      "mimeType": "image/png",
      "sizeBytes": 102400
    }
  ]
}
```

| Field | Rule |
|-------|------|
| `content` | 1–500 ký tự, bắt buộc |
| `images` | Tùy chọn, tối đa 2 phần tử |

**Response 201 `data`:**

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "reportId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
  "content": "Khu vực này thường xuyên đổ rác buổi tối.",
  "createdAt": "2026-07-15T10:00:00Z",
  "canEdit": true,
  "images": []
}
```

**Lỗi thường gặp:**

| `code` | HTTP | Khi nào |
|--------|------|---------|
| `LOGIN_REQUIRED` | 403 | Chưa đăng nhập |
| `COMMENT_NOT_ALLOWED` | 403 | Báo cáo `hideReporterName` — citizen khác không được comment |
| `INAPPROPRIATE_CONTENT` | 422 | Word filter (BR-CMT-003) — sau 3 lần → `COMMENT_BANNED` 7 ngày |
| `COMMENT_BANNED` | 422 | Tài khoản tạm khóa bình luận |

---

## 4. Danh sách bình luận

`GET /v1/reports/{reportId}/comments?page=1&pageSize=20`

**Response 200 `data`:**

```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "content": "Cảm ơn bạn đã báo cáo.",
      "authorName": "Nguyễn Văn A",
      "authorId": "3fa85f64-5717-4562-b3fc-2c963f66afa8",
      "createdAt": "2026-07-15T10:00:00Z",
      "updatedAt": null,
      "isHidden": false,
      "canEdit": false,
      "canDelete": false,
      "images": []
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 1,
    "totalPages": 1
  }
}
```

- Citizen **không** thấy `isHidden: true`
- LEO/DEO/Admin thấy đầy đủ (kể cả đã ẩn)
- `canEdit` / `canDelete` chỉ `true` cho tác giả trong 15 phút

---

## 5. Sửa / xóa (tác giả)

**PUT** `/v1/comments/{commentId}`

```json
{ "content": "Nội dung đã chỉnh sửa." }
```

**DELETE** `/v1/comments/{commentId}` → `204`

| `code` | HTTP |
|--------|------|
| `EDIT_WINDOW_EXPIRED` | 422 |
| `NOT_COMMENT_AUTHOR` | 403 |

---

## 6. Ẩn bình luận (LEO)

`POST /v1/comments/{commentId}/hide` · Roles: `LEO`, `DEO`, `Admin`

```json
{ "reason": "Nội dung xúc phạm hoặc spam quảng cáo" }
```

`reason` tối thiểu 10 ký tự → `204`

---

## 7. Báo cáo ẩn danh (BR-REP-012)

Khi submit báo cáo với `hideReporterName: true`:

- Citizen khác **không** comment được (`COMMENT_NOT_ALLOWED`)
- Người gửi gốc + LEO/DEO/Admin vẫn comment được

---

## 8. Thông báo

Khi có bình luận mới trên báo cáo của citizen → push/in-app `NewComment` (template `new_comment`).
