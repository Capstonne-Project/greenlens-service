# Admin — Quản lý từ bị chặn (Blocked Words)

> **Base URL:** `/v1/admin/blocked-words`  
> **Authorization:** `Bearer <JWT>` — Role = `Admin`  
> **Business Rules:** BR-REP-004 (mô tả báo cáo), BR-CMT-003 (bình luận), BR-ADM-010 (audit log)

Bộ lọc tục tĩu dùng **substring, không phân biệt hoa thường**. Admin thêm/sửa/xóa từ trên dashboard → backend refresh cache ngay, không cần restart API.

---

## Mục lục

| # | Endpoint | Method | Mô tả |
|---|----------|--------|-------|
| 1 | `/blocked-words` | GET | Danh sách từ (phân trang, tìm kiếm) |
| 2 | `/blocked-words` | POST | Thêm từ/cụm từ |
| 3 | `/blocked-words/{id}` | PUT | Cập nhật từ, ghi chú, bật/tắt |
| 4 | `/blocked-words/{id}` | DELETE | Vô hiệu hóa (IsActive = false) |

---

## 1. `GET /v1/admin/blocked-words`

**Query**

| Param | Default | Max | Mô tả |
|-------|---------|-----|-------|
| `page` | 1 | — | Trang (1-based) |
| `pageSize` | 20 | 100 | Số bản ghi/trang |
| `search` | — | — | Lọc theo `word` (contains, lowercase) |
| `isActive` | — | — | `true` / `false` — chỉ từ đang/không áp dụng |

**Response 200**

```json
{
  "code": "SUCCESS",
  "message": "OK",
  "status": 200,
  "data": {
    "items": [
      {
        "id": "b1000001-0000-0000-0000-000000000006",
        "word": "vcl",
        "note": null,
        "isActive": true,
        "createdAt": "2026-07-15T00:00:00Z",
        "updatedAt": null
      }
    ],
    "totalCount": 10
  }
}
```

---

## 2. `POST /v1/admin/blocked-words`

**Body**

```json
{
  "word": "dm",
  "note": "Viết tắt thường gặp"
}
```

| Field | Bắt buộc | Rule |
|-------|----------|------|
| `word` | Có | 2–100 ký tự; lưu dạng lowercase trimmed |
| `note` | Không | Tối đa 500 ký tự |

**Response 201** — `data`: `{ id, word, isActive }`

**Lỗi**

| Code | HTTP | Khi nào |
|------|------|---------|
| `BLOCKED_WORD_DUPLICATE` | 409 | Từ đã tồn tại (kể cả bản ghi đã deactivate) |
| `VALIDATION_ERROR` | 400 | word quá ngắn/dài |

---

## 3. `PUT /v1/admin/blocked-words/{id}`

**Body**

```json
{
  "word": "vcl",
  "note": "Viết tắt",
  "isActive": true
}
```

Dùng `isActive: true` để **kích hoạt lại** từ đã deactivate.

**Response 200** — message: `Đã cập nhật từ bị chặn.`

---

## 4. `DELETE /v1/admin/blocked-words/{id}`

Soft-deactivate: `IsActive = false`. Bản ghi vẫn trong DB (audit BR-ADM-010).

**Response 200** — message: `Đã vô hiệu hóa từ bị chặn.`

---

## Ảnh hưởng runtime

| Luồng | Hành vi khi match từ bị chặn |
|-------|------------------------------|
| Gửi báo cáo — `description` | `INAPPROPRIATE_CONTENT` (400) — BR-REP-004 |
| Thêm/sửa bình luận | `INAPPROPRIATE_CONTENT` (422) — BR-CMT-003; 3 lần → ban 7 ngày |

Cache refresh tự động sau POST/PUT/DELETE và khi API khởi động.

---

## Seed mặc định (migration)

10 từ: `địt`, `đụ`, `lồn`, `cặc`, `đéo`, `vcl`, `vl`, `fuck`, `shit`, `bitch`.

---

## UI gợi ý (Admin Dashboard)

1. **Bảng** — cột: word, note, isActive, createdAt; filter search + active.
2. **Thêm** — modal nhập `word` + `note` optional.
3. **Sửa** — inline hoặc modal; toggle active.
4. **Xóa** — confirm → gọi DELETE (deactivate).
5. Không hiển thị danh sách từ cho Citizen/LEO (chỉ Admin).

---

## Migration

```powershell
dotnet ef database update --project src/Greenlens.Infrastructure --startup-project src/Greenlens.Api
```

Migration: `202607150210_AddBlockedWords`.
