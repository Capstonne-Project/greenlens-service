# LEO Ward Boundary — FE Guide

Endpoint mới cho trang `/officer/map`: lấy boundary của phường mà LEO đang quản lý, suy trực tiếp từ JWT — không cần biết `provinceCode` hay tự dò `wardCode` qua endpoint nào khác.

---

## Endpoint

```
GET /v1/offices/my/ward-boundary
Authorization: Bearer <LEO access token>
```

Không có query param, không có path param — BE tự lấy `LocalOfficeId` của LEO đang login từ token, suy ra `wardCode` và `boundaryUrl` tương ứng.

**Response (200):**
```json
{
  "code": "SUCCESS",
  "status": 200,
  "message": "",
  "data": {
    "wardCode": "00004",
    "wardName": "Phường Phúc Xá",
    "boundaryUrl": "https://d3iova6424vljy.cloudfront.net/prod/location/wards/01_part_001.json"
  }
}
```

**Response (404)** — LEO chưa được Admin gán vào office nào:
```json
{
  "code": "OFFICE_NOT_FOUND",
  "status": 404,
  "message": "...",
  "data": null
}
```

`boundaryUrl` có thể là `null` nếu phường chưa có dữ liệu ranh giới (chưa seed) — FE fallback: không mask, giữ map hiển thị bình thường.

---

## ⚠️ Điểm quan trọng: `boundaryUrl` KHÔNG phải file riêng cho 1 ward

`boundaryUrl` trỏ tới file GeoJSON **`FeatureCollection` chứa nhiều ward** — dữ liệu được nhóm theo file (nhiều wardláng giềng dùng chung 1 file để giảm số file trên CDN). Vì vậy **bắt buộc phải fetch rồi filter** theo `wardCode`, không thể coi cả file là polygon của phường LEO.

```js
async function getMyWardPolygon() {
  const res = await fetch('/v1/offices/my/ward-boundary', {
    headers: { Authorization: `Bearer ${accessToken}` }
  })
  const { data } = await res.json()

  if (!data.boundaryUrl) {
    return null // chưa có dữ liệu ranh giới — fallback, không mask
  }

  const collection = await fetch(data.boundaryUrl).then(r => r.json())

  const feature = collection.features.find(
    f => f.properties.code === data.wardCode
  )

  return feature ?? null
}
```

**Cấu trúc file GeoJSON từ CDN** (giống hệt cơ chế đã dùng ở `ward-boundary-fe-guide.md`):
```json
{
  "type": "FeatureCollection",
  "features": [
    {
      "type": "Feature",
      "properties": { "code": "00004", "name": "Phường Phúc Xá" },
      "geometry": { "type": "Polygon", "coordinates": [[...]] }
    }
  ]
}
```

Tọa độ theo chuẩn GeoJSON: `[longitude, latitude]` (kinh độ trước, vĩ độ sau).

---

## Render + mask trắng ngoài ranh giới

```js
const feature = await getMyWardPolygon()

if (feature) {
  // 1. Fit bounds khít vào polygon
  const bounds = turf.bbox(feature) // [minX, minY, maxX, maxY]
  map.fitBounds(bounds, { padding: 20 })

  // 2. Mask: polygon lớn bao hết viewport, "khoét lỗ" đúng hình ward
  const worldMask = {
    type: 'Feature',
    geometry: {
      type: 'Polygon',
      coordinates: [
        [[-180, -90], [180, -90], [180, 90], [-180, 90], [-180, -90]], // outer ring: cả thế giới
        feature.geometry.coordinates[0] // inner ring (hole): ranh giới ward — chỉ hỗ trợ Polygon 1 ring;
        // nếu feature.geometry.type === 'MultiPolygon', lặp qua từng polygon và thêm mỗi ring là 1 hole
      ]
    }
  }

  map.getSource('mask').setData(worldMask)
} else {
  // Không có boundary — fallback: map hiển thị như cũ, không mask/crop
}
```

---

## Tóm tắt

| Bước | Việc FE làm |
|------|-------------|
| Vào `/officer/map` | Gọi `GET /v1/offices/my/ward-boundary` (kèm Bearer token của LEO) |
| Nếu `boundaryUrl` null | Fallback: map hiển thị bình thường, không mask |
| Nếu có `boundaryUrl` | Fetch file GeoJSON từ CDN, **filter `features` theo `properties.code === data.wardCode`** |
| Render | Dùng feature tìm được để `fitBounds` + build mask polygon (world outer ring + ward ring làm hole) |
| Lỗi 404 `OFFICE_NOT_FOUND` | LEO chưa được gán office — hiển thị thông báo phù hợp, không gọi bước fetch CDN |

---

## Khác gì với endpoint `GET /v1/catalog/wards/{wardCode}/boundary`?

Hệ thống cũng có endpoint `GET /v1/catalog/wards/{wardCode}/boundary` (nhận `wardCode` bất kỳ làm path param, AllowAnonymous) cho các trường hợp khác cần tra boundary theo mã ward đã biết trước. Endpoint `/v1/offices/my/ward-boundary` ở trên dành riêng cho LEO xem đúng phường mình quản lý — không cần biết `wardCode` trước, không cần truyền tham số, và yêu cầu role `LEO`.
