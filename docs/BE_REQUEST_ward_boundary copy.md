# Yêu cầu BE — Endpoint ranh giới phường cho LEO Map

## Bối cảnh / mục tiêu UI

Trang `/officer/map` (dành cho LEO — officer quản lý 1 phường) đang làm UI mới:

- Bên trái: bản đồ **chỉ hiện đúng khu vực phường mà LEO đó quản lý** — toàn bộ phần ngoài ranh giới phường phải là **nền trắng trơn**, không hiện các phường/quận/tỉnh khác xung quanh.
- Bên phải: danh sách report trong phường đó (đã xong, dùng endpoint có sẵn `GET /v1/offices/my/reports`, phần này KHÔNG cần BE làm gì thêm).

Để làm được phần bản đồ, FE cần 1 file **GeoJSON polygon** mô tả hình dạng ranh giới của phường đó. FE sẽ dùng file này để:
1. Zoom/fit bản đồ khít vào đúng ranh giới phường.
2. Vẽ 1 lớp "mask" phủ trắng toàn bộ bản đồ, chỉ để lộ ra phần bên trong polygon ranh giới (kỹ thuật: tạo 1 polygon lớn bao hết bản đồ, "khoét lỗ" đúng hình phường, rồi tô trắng — chỉ có cách này mới làm được vì MapLibre không hỗ trợ crop theo polygon trực tiếp).

**Điểm mấu chốt: FE cần lấy được polygon ranh giới chỉ từ `wardCode`** (không có sẵn `provinceCode` ở màn hình này).

## Vấn đề hiện tại

- Hệ thống đã có `GET /v1/catalog/provinces/{provinceCode}/wards` — trả về danh sách ward theo tỉnh, mỗi ward có field `boundaryUrl` (link tới GeoJSON). Endpoint này hoạt động tốt, không cần đổi.
- Nhưng ở màn `/officer/map`, FE chỉ biết được `wardCode` + `wardName` của LEO đang đăng nhập qua `GET /v1/offices/my/reports` (BE tự suy ra phường từ JWT/token của LEO) — **response này không có `provinceCode`**.
- Vì vậy FE không có cách nào gọi `GET /v1/catalog/provinces/{provinceCode}/wards` để lấy `boundaryUrl` — thiếu 1 mắt xích.

## Đề xuất — cách 1 (khuyến nghị): endpoint mới lấy boundary trực tiếp theo wardCode

```
GET /v1/catalog/wards/{wardCode}/boundary
```

**Response (theo envelope chuẩn `{ code, status, message, data }` đang dùng toàn hệ thống):**

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

Quy tắc:
- `boundaryUrl` là **link** tới file GeoJSON (không phải trả object GeoJSON trực tiếp trong response) — giống đúng cơ chế `boundaryUrl` đã có sẵn ở `Province`/`Ward` hiện tại. FE sẽ tự fetch link này ở bước sau.
- File GeoJSON tại `boundaryUrl` phải là 1 `Polygon` hoặc `MultiPolygon` (hoặc `Feature`/`FeatureCollection` bọc quanh 1 trong 2 loại đó) — toạ độ `[longitude, latitude]` theo chuẩn GeoJSON (kinh độ trước, vĩ độ sau).
- `boundaryUrl` có thể là `null` nếu phường đó chưa có dữ liệu ranh giới — FE đã xử lý fallback (map hiển thị như cũ, không mask/crop).
- Nếu `wardCode` không tồn tại → trả 404 theo format lỗi chuẩn hiện tại.

## Đề xuất — cách 2 (thay thế, nếu cách 1 khó làm)

Nếu BE thấy tạo route mới rắc rối, có thể chọn 1 trong các cách sau — miễn giữ đúng yêu cầu "FE lấy được `boundaryUrl` chỉ từ `wardCode`, không cần biết `provinceCode`":

- **Gộp vào response có sẵn**: thêm field `boundaryUrl` trực tiếp vào `GET /v1/offices/my/reports` (cùng cấp với `wardCode`, `wardName` đang có). FE đọc luôn từ response đó, không cần gọi thêm endpoint nào.
- **Query param**: `GET /v1/catalog/wards/boundary?wardCode=27001` — nếu route theo path param khó implement.

Báo FE biết bên nào được chọn để FE chỉnh code gọi API cho khớp (code hiện đang gọi theo cách 1).

## Vì sao không dùng lại `GET /v1/catalog/provinces/{provinceCode}/wards` luôn

Vì màn `/officer/map` không có `provinceCode` ở bất kỳ đâu trong session/response hiện tại của LEO — nếu bắt FE tự dò tỉnh thì phải gọi hết danh sách tỉnh rồi thử từng tỉnh để tìm `wardCode` khớp, rất tốn API call và không đáng tin cậy (1 wardCode có thể trùng ở dữ liệu cũ/mới). Endpoint trực tiếp theo `wardCode` là cách sạch nhất.

## Trạng thái FE

- FE đã code xong toàn bộ phần UI (mask trắng ngoài ranh giới, fit bounds, layout 2 cột) và đã build/typecheck pass.
- Hiện tại vì endpoint này chưa tồn tại, FE gọi bị 404 → `boundaryUrl` = null → map fallback hiển thị bình thường (không mask), không crash.
- Ngay khi BE có endpoint (theo cách 1 hoặc cách 2), FE chỉ cần đổi 1-2 dòng code fetch để chạy được ngay, không cần thay đổi UI logic.

## Mức độ ưu tiên

Đây là điểm nghẽn **duy nhất** còn lại để hoàn thiện tính năng bản đồ theo phường cho LEO.
