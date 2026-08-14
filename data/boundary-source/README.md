# Boundary GeoJSON source data

Ranh giới hành chính (tỉnh/xã) dạng GeoJSON từ gis.vn, dùng để import 1 lần vào cột
`boundary` (PostGIS) của `provinces`/`wards` — thay thế CDN CloudFront cũ đã chết.

## File cần có ở đây (KHÔNG commit vào git — xem `.gitignore`)

- `34-tinh.geojson` — 34 features cấp tỉnh, property `ma_tinh` khớp `provinces.code`.
- `34-phuong-xa.geojson` — 3321 features cấp ward, property `ma_xa` khớp `wards.code`.

Format: FeatureCollection chuẩn RFC 7946, WGS84/SRID 4326, geometry `MultiPolygon`.

Nếu bạn không có 2 file này, hỏi người đã có (hoặc xem lịch sử trao đổi trong team) —
đây là data tải thủ công từ gis.vn, không có script tự động tải lại.

## Cách chạy import (1 lần cho mỗi database bạn quản lý)

1. Đặt 2 file GeoJSON đúng tên vào thư mục này (`data/boundary-source/`).

2. Đảm bảo connection string tới DB đích đã đúng — set qua user-secrets của
   `Greenlens.Api` (không sửa `appsettings.Development.json` vì file đó được commit):

   ```bash
   cd src/Greenlens.Api
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=greenlens_dev;Username=postgres;Password=<password_thật>"
   ```

   Quên password Postgres? Xem [RESET_POSTGRES_PASSWORD.md](RESET_POSTGRES_PASSWORD.md).

3. Chạy import (tự động apply migration `AddBoundaryGeometryToLocationCatalog` trước khi import nếu chưa có):

   ```bash
   dotnet run --project tools/Greenlens.DbSeed -- import-boundary "data/boundary-source/34-tinh.geojson" "data/boundary-source/34-phuong-xa.geojson"
   ```

4. Kỳ vọng log cuối: `matched=34/34` cho tỉnh, `matched≈3319-3321/3321` cho ward (đã verify
   thực tế ra 3319/3321 — 2 ward không khớp do dữ liệu gis.vn có thể thiếu/khác code cho vài
   trường hợp đặc biệt, chấp nhận được). Nếu số `matched` thấp hơn nhiều, đối chiếu lại
   `provinces`/`wards` hiện có trong DB — mã hành chính (`ma_tinh`/`ma_xa`) phải khớp đúng với
   `code` đang seed (xem `src/Greenlens.Infrastructure/Seeders/Location/seed_data.sql`).

5. Verify trực tiếp trong DB nếu muốn chắc chắn:
   ```sql
   SELECT count(*) AS total, count(boundary) AS with_boundary FROM provinces;
   SELECT count(*) AS total, count(boundary) AS with_boundary FROM wards;
   ```

## Khi nào cần chạy lại

- Mỗi database mới (máy dev mới, DB staging/prod mới) chưa import boundary lần nào.
- Chạy lại an toàn nhiều lần (UPDATE theo `code`, không tạo trùng dữ liệu).

## Liên quan

- `src/Greenlens.Infrastructure/Persistence/Migrations/*_AddBoundaryGeometryToLocationCatalog.cs`
  — migration thêm cột `boundary geometry(MultiPolygon,4326)` + GIST index.
- `src/Greenlens.Infrastructure/Persistence/Seeders/Location/BoundaryGeometryImporter.cs`
  — logic đọc streaming + UPDATE.
- `src/Greenlens.Infrastructure/Geo/WardBoundaryLookupService.cs`
  — dùng cột này để point-in-polygon (BR-ORG-004/010/016) và trả GeoJSON cho FE
  qua `GET /v1/catalog/wards/{code}/boundary`, `GET /v1/offices/my/ward-boundary`.
