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

---

## Đẩy GeoJSON lên VPS & import vào Postgres (staging / production)

> **Quan trọng:** GeoJSON **không** được host như file tĩnh trên VPS cho FE fetch.
> Luồng đúng: upload file lên VPS (hoặc chạy import từ máy dev) → tool `import-boundary`
> ghi vào cột PostGIS `provinces.boundary` / `wards.boundary` → API trả boundary qua DB.

### Chuẩn bị file (máy local)

| File | Kích thước gợi ý | Ghi chú |
|------|------------------|---------|
| `34-tinh.geojson` | ~ vài MB | 34 tỉnh |
| `34-phuong-xa.geojson` | ~ vài trăm MB | 3321 phường/xã — dùng `rsync` nếu mạng chập |

File **không** commit git (`.gitignore`). Copy từ team hoặc tải gis.vn, đặt vào `data/boundary-source/`.

### Bước 1 — Upload file lên VPS

Trên VPS, stack Docker thường nằm tại `/opt/greenlens/` (xem `docker-compose.yml`).

```bash
# Tạo thư mục trên VPS (chạy 1 lần)
ssh deploy@<VPS_IP> "mkdir -p /opt/greenlens/boundary-source"

# Upload (PowerShell / Git Bash trên Windows)
scp data/boundary-source/34-tinh.geojson deploy@<VPS_IP>:/opt/greenlens/boundary-source/

# File ward lớn — nên dùng rsync (resume khi đứt mạng)
rsync -avP --progress data/boundary-source/34-phuong-xa.geojson deploy@<VPS_IP>:/opt/greenlens/boundary-source/
```

Kiểm tra trên VPS:

```bash
ssh deploy@<VPS_IP>
ls -lh /opt/greenlens/boundary-source/
# Kỳ vọng: 34-tinh.geojson + 34-phuong-xa.geojson
```

### Bước 2 — Import vào database trên VPS

Postgres trong Docker bind `127.0.0.1:5432` trên VPS (`docker-compose.yml`). Có **2 cách** phổ biến:

#### Cách A (khuyến nghị): Chạy import từ **máy dev** qua SSH tunnel

Không cần cài .NET SDK trên VPS. Máy dev đã có repo + `dotnet`.

**Terminal 1 — mở tunnel** (giữ chạy):

```bash
ssh -L 5433:127.0.0.1:5432 deploy@<VPS_IP>
```

**Terminal 2 — import** (từ repo root trên máy dev):

```bash
cd /path/to/greenlens-service

# Lấy password từ .env.production trên VPS — KHÔNG commit password này
# POSTGRES_USER, POSTGRES_DB, POSTGRES_PASSWORD

export ConnectionStrings__DefaultConnection="Host=localhost;Port=5433;Database=greenlens;Username=greenlens;Password=<POSTGRES_PASSWORD_từ_VPS>"

dotnet run --project tools/Greenlens.DbSeed -- import-boundary \
  "data/boundary-source/34-tinh.geojson" \
  "data/boundary-source/34-phuong-xa.geojson"
```

> Dùng file GeoJSON **local** (đường dẫn trên máy dev). Tunnel chỉ cần cho DB — không cần file nằm trên VPS với cách này.

#### Cách B: Chạy import **trực tiếp trên VPS**

Dùng khi file đã upload ở `/opt/greenlens/boundary-source/` (Bước 1).

1. Clone repo (hoặc `git pull`) trên VPS — ví dụ `/opt/greenlens/src/greenlens-service`.
2. Cài .NET 9 SDK trên VPS (chỉ cần 1 lần):

   ```bash
   wget https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
   bash /tmp/dotnet-install.sh --channel 9.0 --install-dir $HOME/.dotnet
   export PATH="$HOME/.dotnet:$PATH"
   ```

3. Chạy import (Postgres listen localhost:5432 trên host):

   ```bash
   cd /opt/greenlens/src/greenlens-service

   export ConnectionStrings__DefaultConnection="Host=127.0.0.1;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"
   # Hoặc: source /opt/greenlens/.env.production rồi build connection string

   dotnet run --project tools/Greenlens.DbSeed -- import-boundary \
     "/opt/greenlens/boundary-source/34-tinh.geojson" \
     "/opt/greenlens/boundary-source/34-phuong-xa.geojson"
   ```

Tool tự chạy `MigrateAsync` trước khi import — đảm bảo migration `AddBoundaryGeometryToLocationCatalog` đã apply.

### Bước 3 — Verify trên VPS

**Qua psql trong container:**

```bash
cd /opt/greenlens
docker compose --env-file .env.production exec postgres psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -c "
  SELECT count(*) AS total, count(boundary) AS with_boundary FROM provinces;
  SELECT count(*) AS total, count(boundary) AS with_boundary FROM wards;
"
```

Kỳ vọng:

| Bảng | total | with_boundary |
|------|-------|---------------|
| provinces | 34 | 34 |
| wards | ~3321 | ~3319–3321 |

**Qua API** (sau khi import):

```bash
curl -s "https://<API_HOST>/v1/catalog/provinces" | head
curl -s "https://<API_HOST>/v1/catalog/wards/27145/boundary" -H "Authorization: Bearer <token>"
```

### Bước 4 — Dọn dẹp (tuỳ chọn)

Sau import thành công, có thể xóa GeoJSON trên VPS để tiết kiệm disk — dữ liệu đã nằm trong Postgres:

```bash
rm /opt/greenlens/boundary-source/*.geojson
```

Giữ bản backup local hoặc trên máy dev.

### Khi nào cần chạy lại trên VPS

| Tình huống | Hành động |
|------------|-----------|
| DB production mới (volume Postgres trống) | Seed location + import boundary |
| Restore DB từ backup **cũ** (trước migration boundary) | Chạy lại import |
| Deploy API mới | **Không** cần import lại (boundary nằm trong DB volume) |
| Đổi file GeoJSON nguồn (gis.vn cập nhật) | Upload file mới + chạy lại import (UPDATE theo `code`, an toàn) |

### Lỗi thường gặp

| Triệu chứng | Nguyên nhân | Cách xử lý |
|-------------|-------------|------------|
| `matched=0/34` | Chưa seed `provinces`/`wards` hoặc `code` không khớp `ma_tinh`/`ma_xa` | Chạy migration + location seed trước; đối chiếu `seed_data.sql` |
| Connection refused :5432 | Tunnel chưa mở hoặc compose chưa chạy | `docker compose ps`; mở SSH tunnel (Cách A) |
| File not found | Sai đường dẫn trên VPS | `ls -lh /opt/greenlens/boundary-source/` |
| Import ward rất lâu | File ~300MB+ | Bình thường — tool streaming, đợi 5–15 phút |
| `password authentication failed` | Sai cred `.env.production` | Kiểm tra `POSTGRES_*` trên VPS |

### Bảo mật

- **Không** commit GeoJSON vào git (file lớn + `.gitignore`).
- **Không** đặt password Postgres vào lệnh trong script commit — dùng env var / `.env.production` trên VPS.
- GeoJSON trên VPS chỉ cần quyền đọc cho user deploy; xóa sau import nếu muốn.
