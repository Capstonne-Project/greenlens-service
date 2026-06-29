# GreenLens — Danh sách Công nghệ & Dịch vụ

> **Dự án:** SU26SE049 — Crowdsourced Application for Reporting Environmental Pollution
> **Backend repo:** `Capstonne-Project/greenlens-service`

---

## 1. Dịch vụ bên thứ 3 (Third-party Services)

### Authentication

| #   | Dịch vụ                     | Nhà cung cấp | Mục đích                                       |
| --- | --------------------------- | ------------ | ---------------------------------------------- |
| 1   | **Firebase Authentication** | Google       | Xác thực số điện thoại (Phone Auth via OTP)    |
| 2   | **Google OAuth 2.0**        | Google       | Đăng nhập bằng tài khoản Google (Social Login) |
| 3   | **JWT Bearer Token**        | Self-issued  | Access token (24h) + Refresh token (30 ngày)   |

### Storage

| #   | Dịch vụ           | Nhà cung cấp | Mục đích                                                           |
| --- | ----------------- | ------------ | ------------------------------------------------------------------ |
| 4   | **Cloudflare R2** | Cloudflare   | Object storage — lưu ảnh/video báo cáo ô nhiễm (S3-compatible API) |

### AI APIs

| #   | Dịch vụ                       | Nhà cung cấp         | Mục đích                                                    |
| --- | ----------------------------- | -------------------- | ----------------------------------------------------------- |
| 5   | **AI Classification Service** | Self-hosted (Python) | Phân loại ảnh ô nhiễm, gợi ý severity, nhận diện waste type |

### Notification

| #   | Dịch vụ                            | Nhà cung cấp | Mục đích                                         |
| --- | ---------------------------------- | ------------ | ------------------------------------------------ |
| 6   | **Firebase Cloud Messaging (FCM)** | Google       | Push notification đến thiết bị mobile            |
| 7   | **Gmail SMTP**                     | Google       | Gửi email (OTP xác thực, thông báo, mời recruit) |

### Networking & Security

| #   | Dịch vụ                            | Nhà cung cấp | Mục đích                                             |
| --- | ---------------------------------- | ------------ | ---------------------------------------------------- |
| 8   | **Cloudflare Tunnel**              | Cloudflare   | Kết nối an toàn VPS ↔ Cloudflare (không cần mở port) |
| 9   | **Cloudflare CDN / Reverse Proxy** | Cloudflare   | Cache, chống DDoS, ẩn IP server                      |
| 10  | **Cloudflare DNS**                 | Cloudflare   | Quản lý domain `greenlens.online`                    |
| 11  | **Cloudflare SSL/TLS**             | Cloudflare   | HTTPS Full mode (mã hóa end-to-end)                  |

---

## 2. Công nghệ phát triển (Development Technologies)

### Back-end

| #   | Công nghệ            | Version | Vai trò                                      |
| --- | -------------------- | ------- | -------------------------------------------- |
| 1   | **.NET**             | 9.0     | Runtime platform                             |
| 2   | **ASP.NET Core**     | 9.0     | Web API framework                            |
| 3   | **C#**               | 13      | Ngôn ngữ lập trình                           |
| 4   | **MediatR**          | 14.1.0  | CQRS pattern — Command/Query dispatch        |
| 5   | **FluentValidation** | 12.1.1  | Input validation pipeline                    |
| 6   | **Mapster**          | 10.0.7  | Object mapping (Entity ↔ DTO)                |
| 7   | **BCrypt.Net-Next**  | 4.1.0   | Password hashing (bcrypt)                    |
| 8   | **Serilog**          | 10.0.0  | Structured logging                           |
| 9   | **Swashbuckle**      | 9.0.0   | Swagger / OpenAPI 3.0 documentation          |
| 10  | **Hangfire**         | 1.8.x   | Background job scheduler (recurring/one-off) |
| 11  | **AWSSDK.S3**        | 4.x     | Cloudflare R2 client (S3-compatible)         |
| 12  | **FFMpegCore**       | 5.4.0   | Video transcoding wrapper cho FFmpeg         |
| 13  | **FFmpeg**           | CLI     | Video encoder (H.264, CRF 28, 720p)          |
| 14  | **FirebaseAdmin**    | 3.5.0   | Firebase Admin SDK (.NET)                    |

### Database

| #   | Công nghệ                    | Version | Vai trò                                                    |
| --- | ---------------------------- | ------- | ---------------------------------------------------------- |
| 15  | **PostgreSQL**               | 16+     | Relational database chính                                  |
| 16  | **Entity Framework Core**    | 9.0.x   | ORM (Object-Relational Mapping)                            |
| 17  | **Npgsql**                   | 9.0.x   | .NET PostgreSQL driver                                     |
| 18  | **EFCore.NamingConventions** | 9.0.x   | Tự động snake_case cho tên bảng/cột                        |
| 19  | **Npgsql.NetTopologySuite**  | 9.0.x   | PostGIS geo-spatial queries (point-in-polygon, ST_DWithin) |

### Architecture

| Pattern                | Mô tả                                                           |
| ---------------------- | --------------------------------------------------------------- |
| **Clean Architecture** | 4 layer: Domain → Application → Infrastructure → API            |
| **CQRS**               | Tách Command (write) / Query (read) qua MediatR                 |
| **Vertical Slice**     | Mỗi feature = 1 thư mục (Command, Handler, Validator, Response) |
| **Result Pattern**     | Không dùng exception cho business logic, trả `Result<T>`        |

---

## 3. Quản lý Source Code & DevOps

### Source Code Management

| #   | Dịch vụ    | Vai trò                                                                       |
| --- | ---------- | ----------------------------------------------------------------------------- |
| 1   | **GitHub** | Git repository hosting (`Capstonne-Project/greenlens-service`)                |
| 2   | **Git**    | Version control — branching strategy: `main`, `develop`, `feature/*`, `fix/*` |

### CI/CD Pipeline

| #   | Dịch vụ                              | Vai trò                                                          |
| --- | ------------------------------------ | ---------------------------------------------------------------- |
| 3   | **GitHub Actions**                   | CI/CD — tự động test → build → deploy khi push `main`            |
| 4   | **GHCR** (GitHub Container Registry) | Lưu trữ Docker image (`ghcr.io/capstonne-project/greenlens-api`) |

### Containerization

| #   | Dịch vụ            | Vai trò                                        |
| --- | ------------------ | ---------------------------------------------- |
| 5   | **Docker**         | Đóng gói ứng dụng thành container              |
| 6   | **Docker Compose** | Orchestrate multi-container (API + PostgreSQL) |

### CI/CD Flow

```
Push to main
    │
    ├─► Test:    dotnet restore → build → test
    ├─► Build:   Docker build → push image → GHCR
    └─► Deploy:  SSH → docker pull → docker compose up → health check
```

---

## 4. Môi trường Deploy

| #   | Môi trường      | Branch                 | Config file                                       | Cách deploy                   | URL                        |
| --- | --------------- | ---------------------- | ------------------------------------------------- | ----------------------------- | -------------------------- |
| 1   | **Development** | `develop`, `feature/*` | `appsettings.Development.json`                    | `dotnet run` (local)          | `http://localhost:5162`    |
| 2   | **Production**  | `main`                 | `appsettings.Production.json` + `.env.production` | GitHub Actions → Docker → VPS | `https://greenlens.online` |

### Deployment Flow

```
Developer (local)
    │
    │  git push feature/* → develop
    │  (manual test trên localhost)
    │
    │  git merge develop → main
    ▼
GitHub Actions (CI/CD)
    │
    ├── 1. Test (dotnet test)
    ├── 2. Build Docker image
    ├── 3. Push → GHCR
    └── 4. SSH Deploy → VPS
                │
                ├── docker pull (image mới)
                ├── docker compose up -d
                ├── Health check (/health)
                └── Smoke test
                        │
                        ▼
            Cloudflare (Tunnel + DNS + SSL + CDN)
                        │
                        ▼
                Người dùng cuối
            (https://greenlens.online)
```
