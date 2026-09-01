# FE Handoff — Admin System Settings (Cấu hình hệ thống)

> **Đối tượng:** Admin Dashboard (Web)  
> **Backend:** greenlens-service — cập nhật 2026-09-01  
> **Phạm vi:** Màn **Cấu hình hệ thống** (`/admin/settings/*`)

---

## 1. Tóm tắt thay đổi

| # | Thay đổi | FE cần làm |
|---|----------|------------|
| 1 | Thêm field **`title`** | Dùng làm **label** field (thay vì `key` hoặc parse từ `description`) |
| 2 | Thêm field **`unit`** (nullable) | Hiển thị **cạnh ô nhập** (suffix), không gắn vào title |
| 3 | **`description`** mở rộng | Chỉ dùng làm tooltip / helper text (Chi tiết) |
| 4 | Gỡ **13 key** khỏi API | Xóa khỏi mock/hardcode FE nếu còn; BE seeder tự xóa DB |
| 5 | **`max_image_size_bytes` → `max_image_size_mb`** | Admin nhập **MB** (1–50), default `10` |
| 6 | Module **Gamification** | Sidebar vẫn có; **GET trả về `items: []`** (rỗng) — badge thresholds qua API riêng |
| 7 | Tổng ~**72** key active | Không hardcode catalog — luôn load từ API |
| 8 | **4 key Geo không giới hạn max** | `maxValue: null` — xem mục **3.1**; FE không set `max` trên input |

---

## 2. API (không đổi route)

Base: **`/api/v1/admin/system-settings`**

| Method | Path | Mô tả |
|--------|------|--------|
| `GET` | `/modules` | Sidebar modules |
| `GET` | `?module={slug}` hoặc `/{slug}` | Danh sách settings trong module |
| `PATCH` | `/{slug}` | Body `{ "key": "value", ... }` — chỉ gửi key đã đổi |
| `POST` | `/{slug}/reset` | Reset toàn module về `defaultValue` |

`module` slug: `reports`, `sla`, `geo`, `map`, `officer`, `cleanup`, `notifications`, `gamification`, `auth`, `comments`, `organization`, `community_cleanup`, `data_retention`, `rate_limits`, `inspection`, `ai`, `validation`.

Envelope chuẩn: `{ code, message, status, data }`.

---

## 3. Breaking change — shape `SystemSettingItem`

### 3.1 Key Geo không giới hạn `maxValue`

Module **`geo`** — 4 key sau có `minValue: 50`, **`maxValue: null`** (admin nhập tùy ý; BE chỉ validate ≥ min):

| Key | Default | Unit |
|-----|---------|------|
| `check_in_max_distance_meters` | 200 | m |
| `exif_gps_mismatch_meters` | 200 | m |
| `inspection_soft_gps_meters` | 200 | m |
| `progress_update_max_distance_meters` | 200 | m |

**FE:** khi `maxValue === null` → không gắn `max` trên number input / validator client. Seeder BE tự cập nhật DB khi deploy.

### Trước (cũ)

```json
{
  "id": "...",
  "module": "Geo",
  "key": "check_in_max_distance_meters",
  "valueType": "Int",
  "value": "200",
  "defaultValue": "200",
  "description": "Khoảng cách check-in tối đa (mét)",
  "minValue": 50,
  "maxValue": 1000,
  "isActive": true
}
```

### Sau (mới)

```json
{
  "id": "...",
  "module": "Geo",
  "key": "check_in_max_distance_meters",
  "title": "Khoảng cách check-in tối đa",
  "unit": "m",
  "valueType": "Int",
  "value": "200",
  "defaultValue": "200",
  "description": "Áp dụng khi đội dọn dẹp check-in tại hiện trường. Vị trí thiết bị cách điểm báo cáo quá xa sẽ bị từ chối.",
  "minValue": 50,
  "maxValue": null,
  "isActive": true
}
```

| Field | Kiểu | FE usage |
|-------|------|----------|
| `title` | `string` | Label chính của field |
| `unit` | `string \| null` | Suffix cạnh input; `null` = không hiện |
| `description` | `string` | Tooltip / accordion “Chi tiết” |
| `value` / `defaultValue` | `string` | Luôn string (parse theo `valueType`) |
| `minValue` / `maxValue` | `number \| null` | Validate client trước PATCH |

**PATCH / reset** response cũng trả `SystemSettingItem` cùng shape trên.

---

## 4. UI layout đề xuất

```
┌─────────────────────────────────────────────────────────────┐
│ [title]  (?)                                                │
│ [description — collapsed / expandable]                      │
│                                    ┌──────────┐             │
│                                    │  200     │  m          │
│                                    └──────────┘             │
└─────────────────────────────────────────────────────────────┘
```

### Quy tắc render

1. **Label** = `item.title` — không tự thêm đơn vị vào title.
2. **Input suffix** = `item.unit` nếu có (vd. `m`, `MB`, `ngày`, `giờ`, `ký tự`).
3. **Tooltip `(?)`** = `item.description`.
4. **`valueType = Json`** (vd. `contract_warning_days`): JSON editor; `unit` có thể là `"ngày"` — hiện gợi ý, không suffix số đơn.
5. **`unit = null`**: tỷ lệ 0–1, trọng số, grid level — chỉ input số, không suffix.

### Ví dụ TypeScript

```tsx
function SettingField({ item }: { item: SystemSettingItem }) {
  return (
    <div className="setting-row">
      <div className="setting-meta">
        <label>{item.title}</label>
        <HelpTooltip text={item.description} />
      </div>
      <div className="setting-input">
        <NumberInput
          value={item.value}
          min={item.minValue ?? undefined}
          max={item.maxValue ?? undefined}
        />
        {item.unit && <span className="unit-suffix">{item.unit}</span>}
      </div>
    </div>
  );
}
```

### Badge “Mặc định”

Hiển thị: `Mặc định: {defaultValue}` + `{unit}` nếu có (vd. `Mặc định: 10 MB`).

---

## 5. Key đã gỡ — FE **không** hiển thị / không PATCH

BE xóa khỏi DB khi chạy seeder. Nếu FE còn hardcode, **xóa hết**:

| Module | Key đã gỡ | Lý do |
|--------|-----------|--------|
| `reports` | `recurrence_lookback_days` | Không dùng trong code |
| `reports` | `max_image_size_bytes` | Thay bằng `max_image_size_mb` |
| `reports` | `max_drafts_per_user` | Hardcode BE = 3 |
| `reports` | `draft_retention_days` | Hardcode BE = 7 ngày (web/mobile chưa làm draft) |
| `reports` | `flag_notify_threshold` | Hardcode BE = 3 |
| `sla` | `sla_verify_breach_priority_boost` | Trùng `priority_sla_verify_breach_boost` (module Officer) |
| `map` | `map_viewport_default_days` | Default query param `days=30` hardcode API |
| `cleanup` | `progress_update_interval_hours` | Chưa implement |
| `auth` | `captcha_after_failed_attempts` | CAPTCHA chưa implement |
| `organization` | `max_tasks_per_team` | Dùng `appsettings` WorkloadLimits |
| `organization` | `team_workload_warning_threshold` | Dùng `appsettings` WorkloadLimits |
| `inspection` | `inspection_evidence_max_per_request` | Chưa implement |
| `validation` | `escalation_reason_min_length` | Leo thang LEO→DEO đã gỡ — cleanup escalate hardcode 20 ký tự |

---

## 5b. API đã gỡ (LEO → DEO)

| Method | Path | Ghi chú FE |
|--------|------|------------|
| `POST` | `/api/v1/reports/{id}/escalate` | **Xóa** nút/hành động escalate lên DEO — chỉ LEO phường xử lý |

---

## 6. Key đổi tên / đổi đơn vị

| Cũ | Mới | Ghi chú FE |
|----|-----|------------|
| `max_image_size_bytes` | **`max_image_size_mb`** | Input MB (1–50), label “Dung lượng ảnh tối đa”, suffix `MB`. **Không** hiển thị bytes. |

---

## 7. Module Gamification

- Sidebar vẫn có mục **Gamification** (`GET /modules`).
- **`GET /system-settings/gamification`** → `items: []` (rỗng).
- Ngưỡng badge: dùng **`GET/PATCH /api/v1/admin/badges`** (ngoài system settings).

FE: giữ menu item; khi `items.length === 0` hiện empty state + link sang “Quản lý huy hiệu”.

---

## 8. Bảng `unit` theo nhóm (tham khảo — source of truth vẫn là API)

| `unit` | Áp dụng cho |
|--------|-------------|
| `m` | Bán kính / khoảng cách mét (`*_meters`) |
| `MB` | `max_image_size_mb` |
| `giờ` | SLA verify, overdue, cleanup windows, … |
| `ngày` | SLA resolve, reopen, retention, map viewport, … |
| `phút` | Lockout, OTP window, comment edit, presign TTL, … |
| `giây` | Rate limit lock, AI timeout, temp image TTL |
| `năm` / `tháng` | Data retention |
| `°` | Lat/lng bounds, map bbox span |
| `ảnh` / `lần` / `người` / `điểm` / `ký tự` / `lần/giờ` / `lần/ngày` | Đếm theo ngữ cảnh |
| `null` | Tỷ lệ 0–1, trọng số priority, grid level |

---

## 9. Validation client

Giữ logic cũ + bổ sung:

```ts
function validateSetting(item: SystemSettingItem, raw: string): string | null {
  if (item.valueType === 'Int' || item.valueType === 'Decimal') {
    const n = Number(raw);
    if (item.minValue != null && n < item.minValue) return `Tối thiểu ${item.minValue}${item.unit ? ' ' + item.unit : ''}`;
    if (item.maxValue != null && n > item.maxValue) return `Tối đa ${item.maxValue}${item.unit ? ' ' + item.unit : ''}`;
  }
  if (item.valueType === 'Json') {
    try { JSON.parse(raw); } catch { return 'JSON không hợp lệ'; }
  }
  return null;
}
```

PATCH body **vẫn là string** cho mọi value (kể cả số).

---

## 10. Checklist FE

- [ ] Cập nhật type `SystemSettingItem`: thêm `title`, `unit`; bỏ assume đơn vị trong `description`
- [ ] Label field dùng `title`
- [ ] Suffix input dùng `unit` (ẩn nếu `null`)
- [ ] Tooltip / Chi tiết dùng `description`
- [ ] Xóa 12 key retired khỏi mock/local catalog
- [ ] Đổi `max_image_size_bytes` → `max_image_size_mb` (MB, không bytes)
- [ ] Module Gamification: empty state khi `items.length === 0`
- [ ] Không hardcode ~72 key — load động từ API
- [ ] Dialog xác nhận PATCH: hiển thị `title` + old→new value + `unit`
- [ ] Test regression: Geo (suffix `m`), Reports (`max_image_size_mb`), Organization (`contract_warning_days` JSON)

---

## 11. Ví dụ response đầy đủ — module Geo

```json
{
  "code": "SUCCESS",
  "message": "OK",
  "status": 200,
  "data": {
    "items": [
      {
        "key": "check_in_max_distance_meters",
        "title": "Khoảng cách check-in tối đa",
        "unit": "m",
        "valueType": "Int",
        "value": "200",
        "defaultValue": "200",
        "description": "Áp dụng khi đội dọn dẹp check-in tại hiện trường...",
        "minValue": 50,
        "maxValue": null,
        "isActive": true
      },
      {
        "key": "exif_gps_mismatch_meters",
        "title": "Ngưỡng lệch GPS trong ảnh",
        "unit": "m",
        "valueType": "Int",
        "value": "200",
        "defaultValue": "200",
        "description": "Áp dụng khi so sánh tọa độ EXIF...",
        "minValue": 50,
        "maxValue": null,
        "isActive": true
      }
    ]
  }
}
```

---

## 12. Liên hệ / tài liệu BE

- Chi tiết catalog & business rules: [`admin-system-configuration.md`](./admin-system-configuration.md)
- Swagger: `/swagger` → tag Admin → System Settings

**Deploy BE:** cần chạy migration (`AddSystemSettingTitle`, `AddSystemSettingUnit`) + seeder để backfill `title`/`unit` và xóa key retired.
