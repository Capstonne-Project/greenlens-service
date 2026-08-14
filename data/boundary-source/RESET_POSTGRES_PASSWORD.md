# Quên password Postgres (user `postgres`) trên Windows

Dùng khi `dotnet ef database update` hoặc `dotnet run --project tools/Greenlens.DbSeed`
báo lỗi `28P01: password authentication failed for user "postgres"` và bạn không nhớ
password đã đặt lúc cài Postgres.

Cách dưới đây reset lại password mới bằng cách tạm cho phép đăng nhập không cần
password (`trust`), đổi password, rồi khôi phục lại — cần quyền Administrator.

## 1. Tìm version/đường dẫn Postgres đang chạy

Mở PowerShell (không cần Admin) và chạy:

```powershell
Get-Service -Name '*postgres*'
```

Ghi lại tên service, ví dụ `postgresql-x64-18`. Rồi lấy đường dẫn data directory:

```powershell
(Get-WmiObject Win32_Service -Filter "Name='postgresql-x64-18'").PathName
```

Kết quả có dạng `... -D "C:\Program Files\PostgreSQL\18\data" -w` — phần trong `-D "..."`
là data directory, ví dụ `C:\Program Files\PostgreSQL\18\data`. Dùng đúng version của bạn
(có thể là 16, 17, 18...) cho các bước dưới.

## 2. Mở PowerShell với quyền Administrator

Start → gõ `powershell` → chuột phải → **Run as Administrator**.

## 3. Backup và sửa `pg_hba.conf`

Thay `18` bằng version thật của bạn ở mọi lệnh dưới đây.

```powershell
Copy-Item "C:\Program Files\PostgreSQL\18\data\pg_hba.conf" "C:\Program Files\PostgreSQL\18\data\pg_hba.conf.bak"
notepad "C:\Program Files\PostgreSQL\18\data\pg_hba.conf"
```

Tìm 2 dòng gần cuối file dạng:

```
host    all             all             127.0.0.1/32            scram-sha-256
host    all             all             ::1/128                 scram-sha-256
```

Đổi `scram-sha-256` (hoặc `md5`) thành `trust` cho **2 dòng đó** (chỉ 2 dòng localhost).
Lưu file, đóng Notepad.

## 4. Restart Postgres service

```powershell
Restart-Service postgresql-x64-18
```

## 5. Đổi password mới (không cần password lúc này vì đang ở mode `trust`)

```powershell
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -c "ALTER USER postgres WITH PASSWORD 'MatKhauMoiCuaBan';"
```

Thay `MatKhauMoiCuaBan` bằng password bạn muốn đặt — **ghi lại**, đừng để quên lần nữa.

## 6. Khôi phục lại `pg_hba.conf` (bắt buộc — đừng bỏ qua bước này)

```powershell
Copy-Item "C:\Program Files\PostgreSQL\18\data\pg_hba.conf.bak" "C:\Program Files\PostgreSQL\18\data\pg_hba.conf" -Force
Restart-Service postgresql-x64-18
```

Nếu bỏ qua bước này, Postgres sẽ mãi ở mode `trust` — bất kỳ ai có quyền truy cập máy
đều đăng nhập được vào DB mà không cần password.

## 7. Cập nhật lại user-secrets với password mới

```powershell
cd D:\CapsoneProject\Server\greenlens-service\src\Greenlens.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=greenlens_dev;Username=postgres;Password=MatKhauMoiCuaBan"
```

Nếu gặp lỗi `Could not find the global property 'UserSecretsId'`, chạy trước:

```powershell
dotnet user-secrets init
```

rồi chạy lại lệnh `set` ở trên.

## 8. Thử lại

```powershell
cd D:\CapsoneProject\Server\greenlens-service
dotnet run --project tools/Greenlens.DbSeed -- import-boundary "data/boundary-source/34-tinh.geojson" "data/boundary-source/34-phuong-xa.geojson"
```

Xem thêm [README.md](README.md) cho hướng dẫn đầy đủ về import boundary.
