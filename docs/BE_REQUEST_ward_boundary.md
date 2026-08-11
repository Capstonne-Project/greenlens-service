# Yêu cầu BE — Endpoint ranh giới phường cho LEO Map

## Bối cảnh

Trang `/officer/map` (LEO) đang được nâng cấp để:
1. Vẽ ranh giới phường mà LEO đang quản lý lên bản đồ (polygon, không phải scatter toàn thành phố).
2. Hiện danh sách report trong phường đó ở panel bên phải (đã dùng endpoint có sẵn `GET /v1/offices/my/reports`, không cần BE làm gì thêm cho phần này).

Phần còn thiếu duy nhất là **lấy ranh giới (boundary) của phường theo `wardCode`**.

## Vấn đề hiện tại

- Đã có `GET /v1/catalog/provinces/{provinceCode}/wards` — trả về list ward theo tỉnh, mỗi ward có field `boundaryUrl`.
- Nhưng FE chỉ biết được `wardCode` + `wardName` của LEO qua `GET /v1/offices/my/reports` (BE tự suy từ JWT) — **không có `provinceCode`**.
- Không có endpoint nào cho phép lấy boundary trực tiếp từ `wardCode` mà không cần biết trước `provinceCode`.

## Đề xuất endpoint mới

```
GET /v1/catalog/wards/{wardCode}/boundary
```

**Response (theo envelope chuẩn `{ code, status, message, data }`):**

```json
{
  "code": "SUCCESS",
  "status": 200,
  "message": "",
  "data": {
    "wardCode": "27001",
    "boundaryUrl": "https://cdn.example.com/boundaries/27001.geojson"
  }
}
```

- `boundaryUrl`: link tới file GeoJSON (Polygon hoặc MultiPolygon) mô tả ranh giới phường — giống cách `boundaryUrl` đã hoạt động ở `Province`/`Ward` hiện tại (không phải trả GeoJSON object trực tiếp trong response, mà trả link để FE tự fetch — theo đúng flow đã thống nhất).
- `boundaryUrl` có thể `null` nếu phường đó chưa có dữ liệu ranh giới — FE sẽ xử lý fallback (không vẽ polygon, giữ nguyên map dạng cũ).
- Nếu `wardCode` không tồn tại → trả 404 theo chuẩn lỗi hiện tại của hệ thống.

## Việc FE đã làm (để BE biết context, không cần làm gì thêm)

- FE đã viết sẵn code gọi endpoint này (`lib/api/services/fetchLocationCatalog.ts` → `fetchWardBoundary(wardCode)`), đang tạm gọi vào `/v1/catalog/wards/{wardCode}/boundary` y như đề xuất trên.
- Nếu BE muốn đặt route khác (ví dụ gộp vào `/v1/offices/my/reports` luôn trả kèm `boundaryUrl`, hoặc endpoint khác), báo lại để FE đổi path — chỉ cần giữ đúng shape `{ wardCode, boundaryUrl }`.

## Ưu tiên

Đây là điểm nghẽn duy nhất còn lại để hoàn thiện tính năng — phần map/panel FE đã xong và build/typecheck pass, chỉ chờ endpoint này để hiển thị polygon ranh giới.
