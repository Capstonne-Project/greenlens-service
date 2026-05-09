# CLAUDE.md

> Hướng dẫn cho Claude Code khi làm việc trên backend của dự án **SU26SE049 - Crowdsourced Application for Reporting Environmental Pollution**.

---

## 1. Project Overview

**Tên dự án (EN):** Crowdsourced Application for Reporting Environmental Pollution
**Tên dự án (VI):** Ứng dụng báo cáo điểm rác thải và ô nhiễm môi trường
**Mã dự án:** SU26SE049
**Supervisor:** Nguyễn Thị Cẩm Hương (huongntc2@fe.edu.vn)
**Duration:** 01/01/2026 – 30/04/2026

### Mục tiêu

Hệ thống crowdsourcing cho phép công dân gửi báo cáo ô nhiễm môi trường (có ảnh + GPS), trực quan hóa hotspot trên bản đồ, và theo dõi tiến độ xử lý minh bạch. Backend chịu trách nhiệm xử lý nghiệp vụ cốt lõi: authentication, report lifecycle, geo-queries, gamification, AI integration, notifications, và analytics.

### Actors (6)

| Actor | Vai trò chính |
|---|---|
| **Citizen** | Gửi báo cáo, xem map, theo dõi trạng thái, gamification |
| **Environmental Officer** | Xác minh, phân loại, giao việc, quản lý SLA |
| **Cleanup Team** | Nhận task thực địa, check-in, upload ảnh before/after, đóng task |
| **System Administrator** | Quản lý user/role, danh mục, cấu hình, audit |
| **AI Service** (automated) | Phân loại ảnh, phát hiện trùng, ước lượng severity, anti-fraud |
| **Community Organization** (optional) | Xem map công khai, xuất open data |

### Non-functional targets

- 5,000 concurrent users; scale 100,000+ reports
- API p95 < 2s ở tải đỉnh (BR-SYS-001)
- Uptime ≥ 99.5%/tháng (BR-SYS-003)
- RPO ≤ 24h, RTO ≤ 4h (BR-DAT-004)
- i18n: vi-VN, en-US (BR-SYS-006)

---

## 2. Tech Stack

| Layer | Tech |
|---|---|
| Runtime | **.NET 9** (LTS sẵn-có gần nhất tính đến 2026-05) |
| Web API | ASP.NET Core 9, Controller-based (xem `§4.4`) |
| ORM | Entity Framework Core 9 |
| Database | PostgreSQL 18 + **PostGIS** (cho geo-queries `BR-MAP-*`, `BR-REP-030`) |
| Cache | Redis (rate limit, session, map cache 10' theo `BR-MAP-012`) |
| Object Storage | AWS S3 (ảnh, video — `BR-SYS-002`) |
| Message Queue | RabbitMQ hoặc MassTransit + InMemory cho dev |
| Background Jobs | Hangfire (auto-close, SLA breach, AI retry) |
| Auth | ASP.NET Core Identity + JWT (access 24h, refresh 30d — `BR-AUTH-013`) |
| Validation | FluentValidation  |
| Mapping | Mapster (ưu tiên hơn AutoMapper vì nhanh hơn, source-gen) |
| Logging | Serilog → Seq/ELK |
| Observability | OpenTelemetry → Jaeger/Tempo |
| API Docs | Swashbuckle (OpenAPI 3.0) hoặc NSwag |
| Testing | xUnit + FluentAssertions + Testcontainers (Postgres) + NSubstitute |

> **Quy tắc:** Trước khi thêm package mới, hỏi user. Không tự ý đưa thêm dependency lớn (Serilog sinks, MediatR alternatives, v.v.).

---

## 3. Solution Structure (Clean Architecture)

```
src/
├── Greenlens.Domain/              # Core business — KHÔNG phụ thuộc framework
│   ├── Common/                    # BaseEntity, AuditableEntity, ValueObject, Result<T>
│   ├── Entities/                  # User, Report, ReportMedia, Comment, Badge, ...
│   ├── Enums/                     # ReportStatus, PollutionType, Severity, UserRole
│   ├── ValueObjects/              # GeoLocation, Email, PhoneNumber, Money...
│   ├── Events/                    # ReportSubmittedEvent, StatusChangedEvent...
│   ├── Exceptions/                # DomainException, BusinessRuleViolationException
│   └── Specifications/            # Spec pattern cho query phức tạp
│
├── Greenlens.Application/         # Use cases — phụ thuộc Domain
│   ├── Common/
│   │   ├── Behaviors/             # Validation, Logging, Transaction, Caching
│   │   ├── Interfaces/            # IApplicationDbContext, ICurrentUser, IDateTime, IFileStorage
│   │   └── Mappings/              # Mapster config
│   ├── Features/                  # Tổ chức theo VERTICAL SLICE (xem §4.1)
│   │   ├── Auth/                  # Register, Login, RefreshToken, ResetPassword
│   │   ├── Reports/               # Submit, Verify, Assign, Resolve, Close, FlagDuplicate
│   │   ├── Map/                   # GetNearby, GetHotspots, GetHeatmap
│   │   ├── Officer/               # Verify, Assign, ReassignTask
│   │   ├── Cleanup/               # CheckIn, UpdateProgress, MarkResolved, Escalate
│   │   ├── Notifications/
│   │   ├── Comments/
│   │   ├── Gamification/          # AwardPoints, Leaderboard, Badges
│   │   ├── Admin/                 # Users, Roles, Categories, Templates, AuditLog
│   │   └── Analytics/             # Dashboard, KPI, Export
│   └── BusinessRules/             # Constants định danh BR-*-*** (xem §5)
│
├── Greenlens.Infrastructure/      # Adapters — DB, MQ, external services
│   ├── Persistence/
│   │   ├── ApplicationDbContext.cs
│   │   ├── Configurations/        # IEntityTypeConfiguration<>
│   │   ├── Migrations/
│   │   ├── Repositories/          # Chỉ tạo repo khi DbContext không đủ
│   │   └── Interceptors/          # AuditableEntityInterceptor, OutboxInterceptor
│   ├── Identity/                  # IdentityUser extension, JWT service
│   ├── Storage/                   # AWSS3FileStorage, ImageProcessor (EXIF strip)
│   ├── AI/                        # AIClassificationService (third-party adapter)
│   ├── Geo/                       # PostGIS queries, NetTopologySuite helpers
│   ├── Notifications/             # EmailSender, PushNotifier (FCM)
│   ├── BackgroundJobs/            # AutoCloseReportJob, SlaBreachJob, AIRetryJob
│   └── DependencyInjection.cs
│
├── Greenlens.Api/                 # Composition root — HTTP entrypoint
│   ├── Controllers/               # API Controllers (C# conventions)
│   ├── Middlewares/               # ExceptionHandling, RequestLogging, RateLimit
│   ├── Filters/                   # AuthorizationFilter, ValidationFilter
│   ├── appsettings*.json
│   └── Program.cs
│
└── Greenlens.Shared/              # Optional — shared kernel (DTO contract, error codes)

tests/
├── Greenlens.Domain.UnitTests/
├── Greenlens.Application.UnitTests/
├── Greenlens.Application.IntegrationTests/   # Testcontainers Postgres
└── Greenlens.Api.FunctionalTests/             # WebApplicationFactory
```

### Quy tắc phụ thuộc (Dependency Rule)

```
Api ──► Application ──► Domain
 │           │
 └──► Infrastructure ──► Application (interfaces) ──► Domain
```

- **Domain:** không reference bất kỳ project nào khác. Không `Microsoft.*`, không `EntityFrameworkCore`.
- **Application:** chỉ reference Domain. Định nghĩa **interfaces**, Infrastructure implement.
- **Infrastructure:** reference Application + Domain. Là nơi duy nhất chứa code phụ thuộc framework cụ thể (EF, S3, FCM…).
- **Api:** reference Application + Infrastructure (chỉ để DI). Mỏng nhất có thể — ưu tiên đặt logic trong Application.

> Nếu Claude thấy `using Microsoft.EntityFrameworkCore` trong Domain hoặc Application (trừ `IApplicationDbContext`) — **dừng lại và sửa**.

---

## 4. Architectural Conventions

### 4.1. Vertical Slice trong Application

Mỗi use case là một thư mục với 4 file (CQRS qua MediatR):

```
Features/Reports/SubmitReport/
├── SubmitReportCommand.cs           # record SubmitReportCommand(...) : IRequest<Result<Guid>>
├── SubmitReportCommandHandler.cs
├── SubmitReportCommandValidator.cs  # FluentValidation
└── SubmitReportResponse.cs          # nếu cần shape riêng
```

**Lý do:** thay đổi 1 feature = đụng 1 thư mục. Không tạo Service "tổng" như `ReportService` chứa 20 method.

### 4.2. CQRS

- **Commands** (mutate): `record XxxCommand(...) : IRequest<Result<T>>`
- **Queries** (read): `record XxxQuery(...) : IRequest<Result<TDto>>`
- Query có thể bypass DbContext tracking: dùng `AsNoTracking()` hoặc projection trực tiếp ra DTO bằng Mapster `.ProjectToType<>()`.
- Command **luôn** đi qua transaction (xem `TransactionBehavior`).

### 4.3. Result Pattern (KHÔNG dùng exception cho luồng business)

```csharp
public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public Error? Error { get; }
    // ...
}

public sealed record Error(string Code, string Message, ErrorType Type);
// ErrorType: Validation | NotFound | Conflict | Forbidden | BusinessRule | Unexpected
```

- **Exception** chỉ dùng cho lỗi hạ tầng (DB down, S3 timeout) hoặc programmer bug.
- **Business rule violation** (vd. `BR-AUTH-011` khóa tài khoản) → trả `Result.Failure(Errors.Auth.AccountLocked)`.
- Map `Error.Type` ra HTTP status trong middleware/`Results` extension ở Api layer.

### 4.4. Endpoint style: API Controllers (mặc định)

- Tổ chức theo **API Controllers** trong `Controllers/`, mỗi controller là 1 file class implement `ControllerBase`.
- Pattern:

```csharp
public sealed class ReportsController : ControllerBase
{
    private readonly ISender _sender;
    
    public ReportsController(ISender sender)
    {
        _sender = sender;
    }
    
    [HttpPost]
    [AllowAnonymous] // BR-AUTH-014
    public async Task<IActionResult> SubmitAsync([FromBody] SubmitReportCommand cmd)
        => (await _sender.Send(cmd)).ToHttp();
    
    [HttpGet("/nearby")]
    public async Task<IActionResult> GetNearbyAsync([FromQuery] GetNearbyReportsQuery query)
        => (await _sender.Send(query)).ToHttp();
}
```

### 4.5. Naming

- Project: `GreenLens.<Layer>` (PascalCase, dấu chấm)
- Async method: hậu tố `Async`
- DTO: `XxxDto` (output), `XxxRequest` (input HTTP), `XxxCommand`/`XxxQuery` (Application)
- Enum: số ít, `PascalCase`. Dùng `[JsonStringEnumConverter]` trên JSON output.
- Migration: `yyyyMMddHHmm_VerbNoun` (vd. `202605091200_AddReportSlaColumns`)

### 4.6. Database

- Snake_case ở DB, PascalCase ở C#. Cấu hình qua `EFCore.NamingConventions` package.
- **Mọi entity** kế thừa `AuditableEntity` (`CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`) — phục vụ `BR-ADM-010`.
- **Soft delete** mặc định cho User, Report, Comment (cột `DeletedAt` nullable + global query filter) — phục vụ `BR-AUTH-022`, `BR-DAT-002`.
- Không dùng `DbContext.SaveChanges()` nhiều lần trong 1 request — gom qua `IUnitOfWork` hoặc `TransactionBehavior`.
- **Geo:** dùng `NetTopologySuite.Geometries.Point` (SRID 4326) cho `Report.Location`. Index GIST trong migration.
- Index bắt buộc: `Report (Status, CreatedAt)`, `Report.Location` (GIST), `User.Email` (unique), `User.PhoneNumber` (unique).

### 4.7. Migration

- Mỗi PR thay đổi schema = 1 migration tự-chứa, có rollback test.
- Chạy migration ở **startup chỉ trong dev**; production dùng `dotnet ef migrations bundle` hoặc tool riêng.

### 4.8. Authentication & Authorization

- JWT Bearer, kèm refresh token rotation. Lưu refresh token (hashed) trong DB.
- Roles: `Citizen`, `Officer`, `CleanupTeam`, `Admin`. Anonymous-allowed endpoints khai báo rõ (BR-AUTH-014).
- Authorization theo **policy**, không phải role string rải rác:
  ```csharp
  options.AddPolicy(Policies.CanVerifyReport, p => p.RequireRole("Officer", "Admin"));
  ```
- `ICurrentUser` (Application interface) bọc `IHttpContextAccessor` ở Infrastructure — **không** import `IHttpContextAccessor` trong Application.
- BR-OFF-004 (segregation of duties) check ngay trong handler, không trong middleware.

### 4.9. Validation 2 lớp

1. **Input validation** (FluentValidation) — định dạng, độ dài, range. Behavior chạy trước handler.
2. **Business validation** trong handler — ví dụ `BR-REP-021` "đúng role mới được chuyển trạng thái" cần truy DB.

### 4.10. File upload (BR-REP-001, BR-REP-002, BR-CMT-002)

- Upload trực tiếp lên S3 qua **presigned URL** (client → S3 trực tiếp), backend chỉ cấp URL và lưu metadata.
- Validate ở backend khi client confirm: kích thước, content-type (magic bytes, không tin extension), số lượng (max 5 ảnh/báo cáo).
- Strip EXIF nhạy cảm trước khi gửi sang AI service (BR-AI-007). Giữ EXIF gốc encrypted để xác minh khi cần (BR-REP-011).

### 4.11. Background jobs (xem mapping ở §5)

| Job | Lịch | BR liên quan |
|---|---|---|
| `AutoCloseResolvedReportJob` | hourly | BR-REP-016, BR-REP-025 |
| `SlaBreachVerificationJob` | every 15' | BR-OFF-002 |
| `SlaBreachResolutionJob` | every 30' | BR-OFF-020 |
| `OverdueReportJob` | hourly | BR-REP-008, BR-REP-009 |
| `AiRetryJob` | every 5' | BR-AI-006 |
| `DraftCleanupJob` | daily | BR-REP-019 |
| `LeaderboardSnapshotJob` | daily/weekly/monthly | BR-GAM-005 |
| `AuditLogRetentionJob` | weekly | BR-ADM-010, BR-DAT-002 |
| `AccountHardDeleteJob` | daily | BR-AUTH-022 |

---

## 5. Business Rule Mapping

> **Source of truth:** file `SU26SE049_BusinessRules_v1_0.docx`. Mỗi rule có ID `BR-<MODULE>-<NNN>`.
> **Quy tắc bắt buộc:** mọi handler/validator/job implement business rule **phải** chú thích bằng `///` XML comment kèm ID:

```csharp
/// <summary>
/// Submit a new pollution report.
/// </summary>
/// <remarks>
/// Implements: BR-REP-001 (photo required), BR-REP-003 (Vietnam GPS bounds),
/// BR-REP-005 (category required), BR-REP-010 (rate limit), BR-REP-013 (initial state).
/// </remarks>
public sealed class SubmitReportCommandHandler : IRequestHandler<SubmitReportCommand, Result<Guid>>
{ ... }
```

### Bảng tra cứu nhanh

| Module | Prefix | Khu vực code chính |
|---|---|---|
| Auth & Account | `BR-AUTH-*` | `Application/Features/Auth/*`, `Infrastructure/Identity/*` |
| Pollution Report | `BR-REP-*` | `Application/Features/Reports/*`, `Domain/Entities/Report.cs` |
| Map & Location | `BR-MAP-*` | `Application/Features/Map/*`, `Infrastructure/Geo/*` |
| Officer | `BR-OFF-*` | `Application/Features/Officer/*` |
| Cleanup Team | `BR-CLN-*` | `Application/Features/Cleanup/*` |
| Notifications | `BR-NTF-*` | `Application/Features/Notifications/*`, `Infrastructure/Notifications/*` |
| Comments | `BR-CMT-*` | `Application/Features/Comments/*` |
| Gamification | `BR-GAM-*` | `Application/Features/Gamification/*` |
| AI Service | `BR-AI-*` | `Infrastructure/Ai/*`, `Application/Features/Reports/Ai/*` |
| Administration | `BR-ADM-*` | `Application/Features/Admin/*` |
| Data & Privacy | `BR-DAT-*` | xuyên suốt, infra-level |
| Non-functional | `BR-SYS-*` | infra-level, hosting |

### State Machine bắt buộc (BR-REP-020, BR-REP-021)

```
                   ┌─► Rejected   (Officer, reason ≥ 20 chars)
Submitted ─────────┼─► Verified ──► InProgress ──► Resolved ──┬─► Closed (Citizen confirm OR auto 7d)
                   └─► Duplicate  (Officer/AI)                └─► InProgress (re-open, max 2 lần)
```

- Implement trong `Domain/Entities/Report.cs` qua method `Verify(officer)`, `Reject(officer, reason)`, v.v. — **không** cho phép set `Status` qua public setter.
- Mỗi transition raise một `DomainEvent` (`ReportVerifiedEvent`, …).

### Một số rule cần chú ý đặc biệt

| BR | Implementation note |
|---|---|
| BR-AUTH-005 | Password strength: regex + bcrypt cost ≥ 12 (BR-DAT-001). |
| BR-AUTH-011 | Sai password 5 lần / 15' → lock 30'. CAPTCHA từ lần 3. Lưu attempt count + lockout_until trong User. |
| BR-AUTH-013 | JWT 24h + refresh 30d. Web inactivity 30' → đăng xuất (FE timer + BE refresh denial nếu refresh > 30d). |
| BR-AUTH-022 | Soft delete 90 ngày → `AccountHardDeleteJob` xoá vĩnh viễn. Báo cáo của user → `Anonymized`. |
| BR-REP-003 | Lat 8.0–24.0; Lng 102.0–110.0. Validate trong validator + DB check constraint. |
| BR-REP-010 | Sliding window rate limit: 5/h, 20/24h. Dùng Redis sorted set. |
| BR-REP-020/021 | State machine ở Domain entity, **không** ở handler. |
| BR-REP-030 | Duplicate detection: PostGIS `ST_DWithin(geom, geom, 50)` AND same category AND within 24h. AI bổ sung pHash (BR-AI-002). |
| BR-OFF-010 | `Priority = severity*3 + relatedCount*2 + ageInHours/24`. Tính trên DB view hoặc materialized view. |
| BR-OFF-020 | SLA: Critical 3d / High 5d / Medium 7d / Low 10d kể từ `Verified`. Background job đánh dấu breach. |
| BR-CLN-002 | Check-in distance ≤ 200m: PostGIS `ST_DWithin`. |
| BR-CLN-004 | 2 ảnh "after" khác hash: tính perceptual hash (pHash), Hamming distance ≥ ngưỡng. |
| BR-NTF-003 | Anti-spam digest: queue notification, gom cuối ngày nếu > 20/loại. |
| BR-MAP-004 | Round GPS to 10m precision khi public — `Math.Round(lat, 4)` (≈11m, đủ). |
| BR-MAP-012 | Cache map data 10' ở Redis, key theo bbox + filters. |
| BR-AI-006 | Timeout 5s → tag `ai_pending`, fallback queue retry trong 1h. |
| BR-DAT-001 | bcrypt ≥ 12 rounds; AES-256 at-rest cho secrets (dùng Data Protection API hoặc Vault). |
| BR-SYS-004 | Rate limit công khai 60 rpm/IP anon, 300 rpm/user authed — dùng `RateLimiterMiddleware` của ASP.NET Core 9. |

---

## 6. Coding Standards

### C# 13 / .NET 9

- **Nullable reference types: enabled** project-wide.
- **ImplicitUsings: enabled**, nhưng vẫn explicit khi dễ nhầm.
- **File-scoped namespace**, **primary constructors**, **collection expressions** (`[1, 2, 3]`).
- **`record` cho DTO/Command/Query/Event** (immutable). **`class` cho entity** (vì có behavior + identity).
- **Sealed by default** cho class non-abstract.
- **`async`/`await` xuyên suốt**, **không** có `.Result` hoặc `.Wait()`. **Mọi** method I/O nhận `CancellationToken`.
- **`ConfigureAwait(false)`** trong library projects (Application, Infrastructure). Bỏ qua trong Api project.
- **Sử dụng `IAsyncEnumerable<T>`** cho query streaming (export CSV — BR-OFF-022).
- **`record struct`** cho value object nhỏ (GeoLocation, Money) để tránh allocation.

### EditorConfig

- Tab → 4 spaces. UTF-8. LF. Trailing newline.
- `dotnet_diagnostic.CA*` — bật rules quan trọng (CA1062 null check, CA2007 ConfigureAwait, CA1849 sync-over-async).
- `Microsoft.CodeAnalysis.NetAnalyzers` enabled.
- StyleCop optional (đặt rule chung trong `.editorconfig` thay vì file riêng).

### Comments

- Tiếng Anh trong code. **Tiếng Việt được phép** trong XML doc khi mô tả rule nghiệp vụ (giữ nguyên thuật ngữ tài liệu BR).
- KHÔNG comment "what" (`// increment counter`). Chỉ comment "why" (`// retry once: BR-AI-006 fallback path`).

### Git

- Trunk-based. Branch `feature/<ticket>-<slug>`, `fix/...`, `chore/...`.
- Commit theo Conventional Commits: `feat(reports): submit report endpoint (BR-REP-001..013)`.
- PR template phải liệt kê **BR ID** đã implement/cover bằng test.

---

## 7. Testing Strategy

### Pyramid

| Tầng | Tỉ lệ | Stack | Phạm vi |
|---|---|---|---|
| Unit | ~70% | xUnit + FluentAssertions + NSubstitute | Domain entities, value objects, validators, pure handlers |
| Integration | ~25% | + Testcontainers Postgres + Respawn | DbContext, repositories, EF queries, geo queries |
| Functional/E2E | ~5% | + WebApplicationFactory | Controller → DB, auth flow, error mapping |

### Quy tắc

- **Mỗi BR phải có ít nhất 1 test** (unit hoặc integration), tên test gắn ID:
  ```csharp
  [Fact]
  public async Task SubmitReport_NoPhoto_ReturnsValidationError_BR_REP_001() { ... }
  ```
- AAA (Arrange-Act-Assert), **không** chia sẻ state qua static.
- Test database: schema reset bằng Respawn giữa các test class, không xóa toàn bộ DB.
- **Không mock** EF Core `DbContext` — dùng Testcontainers.
- Mock chỉ cho **boundary** (S3, AI, email, FCM).

---

## 8. Configuration & Secrets

- `appsettings.json` chỉ chứa giá trị **không nhạy cảm** (logging level, feature flags).
- Secrets:
  - Dev → `dotnet user-secrets`
  - Staging/Prod → environment variables hoặc Azure Key Vault / AWS Secrets Manager.
- **Nghiêm cấm** commit connection string thật, JWT signing key, S3 credentials, FCM key.
- Pattern bind: `Options<T>` + validation:
  ```csharp
  builder.Services.AddOptions<JwtOptions>()
      .Bind(builder.Configuration.GetSection("Jwt"))
      .ValidateDataAnnotations()
      .ValidateOnStart();
  ```

### Feature flags

- Dùng `Microsoft.FeatureManagement` cho rollout AI, gamification badges mới…

---

## 9. Cross-cutting Concerns

### Logging

- Serilog, JSON output cho production. Log enricher: `RequestId`, `UserId`, `IPAddress`, `UserAgent` (cho audit BR-ADM-010).
- **KHÔNG log PII** (email, phone, GPS chi tiết) ở Information level. Mask hoặc dùng Debug.

### Error handling

- Một middleware duy nhất (`ExceptionHandlingMiddleware`) chuyển:
  - `ValidationException` → 400
  - `NotFoundException` → 404
  - `ForbiddenException` → 403
  - `BusinessRuleViolationException` → 422 (Unprocessable Entity)
  - Khác → 500 + correlation ID
- Response error theo **RFC 7807 Problem Details**.

### Audit (BR-ADM-010)

- Auditable events đi qua `IAuditLogger` → table `audit_logs`.
- Bắt thay đổi entity nhạy cảm bằng EF interceptor (`AuditingSaveChangesInterceptor`), bổ sung manual log cho config changes.
- Retention 12 tháng (BR-DAT-002) — `AuditLogRetentionJob`.

### Outbox pattern

- Events tích hợp (gửi notification, gọi AI, sync ra MQ) đi qua **outbox table** trong cùng transaction → background dispatcher publish. Đảm bảo at-least-once.

### Localization (BR-SYS-006, BR-NTF-004)

- Resource files `.resx` cho error messages + notification templates.
- Header `Accept-Language` xác định culture; mặc định `vi-VN`.

---

## 10. Performance & Scaling

- API p95 < 2s ở 5,000 CCU (BR-SYS-001):
  - Bật **response compression** (Brotli).
  - Cache đọc-nhiều: hotspots, public map, leaderboard (Redis, TTL 1–10').
  - **Pagination bắt buộc** cho list endpoints (cursor-based ưu tiên hơn offset).
  - Query đắt → projection trực tiếp DTO, không hydrate entity.
- Image storage: object storage S3-compatible (BR-SYS-002), CDN trước public endpoint.
- DB: index theo §4.6. Tránh N+1 — dùng `.Include()` có chủ đích hoặc projection.
- Background work nặng (AI, notification, export) → queue, không block request.

---

## 11. Workflow của Claude

### Trước khi code

1. **Đọc lại business rules** liên quan (file BR docx). Nếu rule chưa rõ — **hỏi user trước khi implement**, không tự suy diễn.
2. Xác định:
   - Feature thuộc vertical slice nào? Tạo mới hay mở rộng?
   - Touchpoints: entity nào, migration cần không, BR nào áp dụng?
3. Nếu thay đổi schema → kèm migration trong cùng change.

### Khi code

- **Một feature, một slice.** Không sửa rải rác Domain + Application + Infrastructure trong 1 task nếu user chỉ yêu cầu 1 thay đổi nhỏ.
- **Tuân thủ dependency rule** ở §3. Nếu cần infra trong Application → tạo interface ở `Application/Common/Interfaces/`, implement ở Infrastructure.
- **Không bypass state machine** ở Domain entity. Nếu cần transition mới — bổ sung method trong entity.
- **Mọi handler thực thi business rule** phải có XML comment liệt kê BR ID (xem §5).
- **Mọi I/O nhận `CancellationToken`.**

### Sau khi code

1. Tự chạy `dotnet build` + `dotnet test`.
2. Update CLAUDE.md nếu phát sinh quy ước mới (hỏi user trước).
3. Liệt kê BR ID đã cover trong commit/PR.

### Khi không chắc

- **Không tạo rule mới.** Hỏi: "BR-XXX-yyy chưa rõ điểm Z, anh/chị xác nhận giúp?"
- **Không thay đổi tech stack** (đổi ORM, thêm framework lớn) mà không hỏi.
- **Không xóa migration đã merge** — luôn thêm migration mới để revert.

---

## 12. Glossary nhanh (xem Phụ lục B của BR doc)

- **SLA** — Service Level Agreement. BR-OFF-002 (verify 24h), BR-OFF-020 (resolve theo severity).
- **Hotspot** — ≥ 10 báo cáo cùng loại / 500m / 30 ngày (BR-MAP-010).
- **Duplicate** — cùng loại + ≤ 50m + ≤ 24h (BR-REP-030).
- **PII** — email, SĐT, GPS chi tiết, CCCD. KHÔNG log, KHÔNG export trừ admin approval (BR-OFF-022).
- **EXIF** — metadata ảnh; strip GPS nhạy cảm trước khi gửi AI (BR-AI-007).
- **Anonymized** — user xóa tài khoản nhưng báo cáo còn lại, ẩn danh người gửi (BR-AUTH-022).

---

**Phiên bản CLAUDE.md:** 1.0
**Đồng bộ với:** `SU26SE049_BusinessRules_v1_0.docx` v1.0 (17/04/2026).
Khi BR doc cập nhật → cập nhật §5 và `Application/BusinessRules/*.cs` constants tương ứng.
