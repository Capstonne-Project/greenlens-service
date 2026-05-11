# Luồng BE ↔ FE: chọn Tỉnh / Phường + địa chỉ + map ranh giới

Tài liệu mô tả cách frontend và backend phối hợp để đạt UX: **hai ô select (tỉnh, phường)**, **ô địa chỉ (số nhà, đường)**, và **map phía dưới** hiển thị **ranh giới tỉnh** khi chọn tỉnh, **ranh giới phường** khi chọn phường — map dùng **Goong** (React Native qua `@rnmapbox/maps` + style URL Goong).

---

## 1. Mục tiêu UX

| Thành phần | Hành vi mong muốn |
|------------|-------------------|
| Select Tỉnh / TP | Load danh sách tỉnh; khi chọn (vd. Vĩnh Long) → map zoom/highlight **polygon ranh giới tỉnh**. |
| Select Phường / Xã | Sau khi có tỉnh → load phường thuộc tỉnh; khi chọn → map hiển thị **ranh giới phường** (thường thay hoặc chồng lên ranh giới tỉnh tùy design). |
| Input địa chỉ | Text tự do: số nhà, tên đường (không thay thế mã hành chính). |
| Map (Goong) | Không tự “biết” mã tỉnh; cần **GeoJSON** (polygon). BE lưu URL tới file GeoJSON (`BoundaryUrl`). FE **fetch GeoJSON** → đưa vào `ShapeSource` / layer polygon. |

---

## 2. Nguyên tắc kỹ thuật

1. **Mã chuẩn** (`provinceCode` 2 ký tự số, `wardCode` 5 ký tự) là **nguồn sự thật** khi gửi báo cáo và khi tra cứu catalog.
2. **Ranh giới** là **dữ liệu GeoJSON** (thường host CDN); field trong DB là **`BoundaryUrl`** trên entity `Province` / `Ward` (nullable nếu chưa import).
3. **Goong Maps** chỉ **vẽ** GeoJSON qua SDK (Mapbox-style layers); Goong **không** nhận `provinceCode` để trả polygon — FE phải có URL hoặc nội dung GeoJSON từ BE/CDN.

---

## 3. Luồng dữ liệu (tóm tắt)

```
[FE] Mở màn hình
   → GET /v1/catalog/provinces
   → Hiển thị dropdown tỉnh (code + name)

[FE] User chọn tỉnh (vd. code = "86")
   → Lưu provinceCode state
   → Nếu response có boundaryUrl: GET boundaryUrl → parse GeoJSON → vẽ polygon trên map + fitBounds
   → GET /v1/catalog/provinces/86/wards
   → Hiển thị dropdown phường; reset ward đã chọn nếu đổi tỉnh

[FE] User chọn phường
   → Lưu wardCode state
   → Nếu có boundaryUrl phường: fetch GeoJSON → vẽ / cập nhật layer phường + fitBounds

[FE] User nhập số nhà, đường → addressLine (text)

[FE] Gửi báo cáo (POST /v1/pollution-reports)
   → Body gồm: provinceCode, wardCode, address (ô text), latitude/longitude (GPS), ...
```

---

## 4. API Backend (GreenLens)

### 4.1 Catalog — đã có

| Method | Endpoint | Mục đích |
|--------|----------|----------|
| GET | `/v1/catalog/provinces` | `items[]`: `code`, `name`, `boundaryUrl` (nullable — URL GeoJSON) |
| GET | `/v1/catalog/provinces/{provinceCode}/wards` | `items[]`: `code`, `name`, `unitAbbreviation`, `boundaryUrl` (nullable) |

- Public (`AllowAnonymous`), envelope chuẩn `{ code, message, status, data }`.

### 4.2 Map — `boundaryUrl` trên catalog

- **Provinces / wards** đều trả `boundaryUrl` (nullable) trỏ tới GeoJSON trên CDN khi đã seed trong DB.
- `null` → FE không vẽ polygon, chỉ map nền / GPS.

### 4.3 Submit báo cáo — địa chỉ & kiểm tra danh mục

**Payload:** `provinceCode`, `wardCode` (tùy chọn nhưng **phải đi cặp**), `address`, `latitude`, `longitude`, …

**Quy tắc BE:**

- Validator: tỉnh `\d{2}`, phường `\d{5}` khi có giá trị; **cùng có hoặc cùng không** hai mã.
- Handler: nếu cả hai có → `Exists` trên `wards` theo `code` + `province_code`; sai → `INVALID_WARD_PROVINCE` (422).
- Lưu `Report` với mã đã trim.

---

## 5. Luồng Frontend chi tiết

### 5.1 State tối thiểu

- `selectedProvinceCode`, `selectedWardCode`
- `provinceItems`, `wardItems` (từ API)
- `provinceGeoJson`, `wardGeoJson` (object GeoJSON sau fetch — hoặc cache theo code)
- `addressLine`, `lat`, `lng`

### 5.2 Khi đổi tỉnh

1. Cập nhật `selectedProvinceCode`.
2. Xóa layer / state polygon phường cũ.
3. Nếu item tỉnh có `boundaryUrl`: `fetch(boundaryUrl)` → kiểm tra JSON → set source polygon tỉnh → **fitBounds** theo bbox của geometry (hoặc `camera` Goong/Mapbox).
4. Gọi `GET .../provinces/{code}/wards`, gán `wardItems`, reset `selectedWardCode`.

### 5.3 Khi đổi phường

1. Cập nhật `selectedWardCode`.
2. Nếu có `boundaryUrl` trong item phường: fetch GeoJSON → vẽ polygon phường (style khác tỉnh: viền đậm / fill trong suốt hơn) → **fitBounds** hoặc zoom nhẹ vào ward.

### 5.4 Expo + Goong

- Dùng **development build** (EAS / `expo prebuild`), không dùng Expo Go cho `@rnmapbox/maps`.
- `styleURL` Goong + `api_key` query theo doc Goong.
- Polygon: `ShapeSource` + `FillLayer` / `LineLayer` với `shape={geojson}`.

### 5.5 An toàn & hiệu năng

- **HTTPS** cho `boundaryUrl`; kiểm tra CORS nếu CDN khác domain app web (RN ít gặp CORS).
- **Cache** GeoJSON theo `code` (memory hoặc MMKV) để user đổi qua lại tỉnh/phường không fetch lại.
- **Fallback:** không có boundary → chỉ center map vào GPS hoặc vào centroid mặc định VN.

---

## 6. Checklist tích hợp

| Bước | BE | FE |
|------|----|-----|
| Danh sách tỉnh | ✅ `/v1/catalog/provinces` | Bind select; optional load map sau khi có `boundaryUrl` |
| Danh sách phường | ✅ `.../provinces/{code}/wards` | Bind select phụ thuộc tỉnh |
| Ranh giới trên map | ✅ Trả `boundaryUrl` trong catalog | Fetch GeoJSON → layer polygon |
| Submit | `provinceCode`, `wardCode`, `address`, GPS | Form POST pollution-report |

---

## 7. Tham chiếu code trong repo

- Domain: `src/Greenlens.Domain/Entities/Location/Province.cs`, `Ward.cs` — `BoundaryUrl`.
- Catalog API: `src/Greenlens.Api/Controllers/CatalogController.cs`.
- Application: `src/Greenlens.Application/Features/Catalog/GetProvinces/*`, `GetWardsByProvince/*`.

---

*Backend: catalog expose `boundaryUrl`; submit report validate cặp mã tỉnh/phường với bảng `wards`.*
