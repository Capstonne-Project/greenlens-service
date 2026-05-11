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
| Object Storage | **Cloudflare R2** (S3-compatible, zero-egress — ảnh, video, `BR-SYS-002`) |
| CDN / WAF / DDoS | **Cloudflare** (proxy public traffic; xem `§14`) |
| CAPTCHA | **Cloudflare Turnstile** (BR-AUTH-011 từ lần sai thứ 3, form công khai) |
| DNS | Cloudflare DNS (cùng tài khoản với R2/Turnstile) |
| Message Queue | RabbitMQ hoặc MassTransit + InMemory cho dev |
| Background Jobs | Hangfire (auto-close, SLA breach, AI retry) |
| Auth | ASP.NET Core Identity + JWT (access 24h, refresh 30d — `BR-AUTH-013`) |
| Validation | FluentValidation  |
| Mapping | Mapster (ưu tiên hơn AutoMapper vì nhanh hơn, source-gen) |
| Logging | Serilog → Seq/ELK |
| Observability | OpenTelemetry → Jaeger/Tempo |
| API Docs | Swashbuckle (OpenAPI 3.0) hoặc NSwag |
| Security | OwaspHeaders.Core (security headers), ASP.NET Core Data Protection (key rotation), bcrypt.net-next (≥12 rounds — BR-DAT-001) |
| Testing | xUnit + FluentAssertions + Testcontainers (Postgres) + NSubstitute |

> **Quy tắc:** Trước khi thêm package mới, hỏi user. Không tự ý đưa thêm dependency lớn (Serilog sinks, MediatR alternatives, v.v.).
>
> **Vì sao Cloudflare R2 thay vì AWS S3:** workload chính là phục vụ ảnh báo cáo công khai trên map, lưu lượng egress sẽ rất cao. R2 có **zero egress fee** + S3-compatible API (dùng `AWSSDK.S3` chỉ cần đổi endpoint). Hai lưu ý: (a) đặt `DisablePayloadSigning = true` và `DisableDefaultChecksumValidation = true` trên `PutObjectRequest` (R2 chưa hỗ trợ Streaming SigV4); (b) phục vụ ảnh qua **custom domain + Cloudflare Cache**, KHÔNG dùng `*.r2.dev` cho production (rate-limited). Chi tiết ở `§14.2`.

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
│   │   ├── Interfaces/
│   │   │   ├── ICurrentUser.cs, IDateTimeProvider.cs, IFileStorage.cs, ICacheService.cs, ITurnstileVerifier.cs
│   │   │   └── Persistence/       # IGenericRepository<T>, IUnitOfWork, IXxxRepository (xem §4.12)
│   │   └── Mappings/              # Mapster config (entity ↔ DTO projection)
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
│   │   ├── UnitOfWork.cs          # implement IUnitOfWork (§4.12)
│   │   ├── Configurations/        # IEntityTypeConfiguration<>
│   │   ├── Migrations/
│   │   ├── Repositories/          # GenericRepository<T> (internal abstract) + XxxRepository (§4.12)
│   │   └── Interceptors/          # AuditingSaveChangesInterceptor, SoftDeleteInterceptor
│   ├── Identity/                  # IdentityUser extension, JWT service
│   ├── Storage/                   # R2FileStorage (S3-compatible adapter), ImageProcessor (EXIF strip)
│   ├── AI/                        # AIClassificationService (third-party adapter)
│   ├── Geo/                       # PostGIS queries, NetTopologySuite helpers
│   ├── Notifications/             # EmailSender, PushNotifier (FCM)
│   ├── BackgroundJobs/            # AutoCloseReportJob, SlaBreachJob, AIRetryJob
│   ├── Security/                  # TurnstileVerifier, IpReputationCheck, SecretsRotator
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
- **Infrastructure:** reference Application + Domain. Là nơi duy nhất chứa code phụ thuộc framework cụ thể (EF, R2/S3 SDK, FCM, Cloudflare Turnstile…).
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

- Upload trực tiếp lên **Cloudflare R2** qua **presigned URL** (client → R2 trực tiếp), backend chỉ cấp URL và lưu metadata. Endpoint R2: `https://<account-id>.r2.cloudflarestorage.com`. Region đặt là `auto` (hoặc `us-east-1` để tương thích với SDK cũ).
- **R2-specific gotchas** khi dùng `AWSSDK.S3`:
  ```csharp
  var req = new PutObjectRequest {
      BucketName = "ecoreport-media",
      Key = key,
      InputStream = stream,
      DisablePayloadSigning = true,            // R2 chưa hỗ trợ Streaming SigV4
      DisableDefaultChecksumValidation = true  // tránh checksum mismatch
  };
  ```
- Validate ở backend khi client confirm upload xong: kích thước, content-type (magic bytes, không tin extension), số lượng (max 5 ảnh/báo cáo). Reject nếu file không match metadata đã pre-signed.
- Strip EXIF nhạy cảm trước khi gửi sang AI service (BR-AI-007). Giữ EXIF gốc encrypted (dùng Data Protection API key đã rotate, xem §13.4) để xác minh khi cần (BR-REP-011).
- Phục vụ public: ảnh đi qua **custom domain Cloudflare** (vd. `media.ecoreport.example`) đứng trước R2 bucket, KHÔNG expose `*.r2.dev` (rate-limited, không cho production). Cấu hình cache + WAF — xem `§14.2` & `§14.3`.

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

### 4.12. Repository & Unit of Work (Strict Pattern)

> **Quyết định:** project dùng **strict repository** — mọi entity đều có repository interface riêng, kế thừa từ `IGenericRepository<T>` base. Application layer **KHÔNG** import `IApplicationDbContext` (hay bất kỳ DbContext nào). Mọi data access đi qua `IXxxRepository`, mọi commit đi qua `IUnitOfWork`. Lý do: nhất quán 100% — mọi handler đều giống nhau, không có ngoại lệ "entity này dùng repo, entity kia dùng DbContext".

#### Cấu trúc

```
Application/Common/Interfaces/Persistence/
├── IGenericRepository.cs          # base contract: Query, QueryAsNoTracking, GetByIdAsync, Add, Remove, ExistsAsync
├── IUnitOfWork.cs                 # SaveChangesAsync, BeginTransactionAsync
├── IDbTransaction.cs              # wrapper để Application không phải import EF
│
├── IReportRepository.cs           # : IGenericRepository<Report> + GetForVerificationAsync, FindDuplicatesAsync
├── IReportMediaRepository.cs      # : IGenericRepository<ReportMedia>  (body rỗng — chỉ base)
├── IUserRepository.cs             # : IGenericRepository<User> + GetByEmailAsync
├── ICommentRepository.cs          # : IGenericRepository<Comment>
├── IBadgeRepository.cs            # : IGenericRepository<Badge>
├── ICleanupTaskRepository.cs      # : IGenericRepository<CleanupTask> + GetPendingByTeamAsync
├── IAuditLogRepository.cs         # : IGenericRepository<AuditLog>
├── ICategoryRepository.cs         # : IGenericRepository<PollutionCategory>
├── INotificationRepository.cs     # : IGenericRepository<Notification>
└── ...                            # 1 entity = 1 interface (bắt buộc)

Infrastructure/Persistence/
├── ApplicationDbContext.cs        # internal — CHỈ Infrastructure dùng, KHÔNG export ra Application
├── UnitOfWork.cs                  # implement IUnitOfWork; dispatch domain events sau Save
└── Repositories/
    ├── GenericRepository.cs       # internal abstract — base impl của IGenericRepository<T>
    ├── ReportRepository.cs        # internal sealed : GenericRepository<Report>, IReportRepository
    ├── ReportMediaRepository.cs   # internal sealed : GenericRepository<ReportMedia>, IReportMediaRepository
    ├── UserRepository.cs
    ├── CommentRepository.cs
    ├── BadgeRepository.cs
    ├── CleanupTaskRepository.cs
    ├── AuditLogRepository.cs
    ├── CategoryRepository.cs      # body rỗng — kế thừa base là đủ
    ├── NotificationRepository.cs
    └── ...
```

#### `IGenericRepository<T>` — base interface

```csharp
public interface IGenericRepository<T> where T : BaseEntity
{
    IQueryable<T> Query();                        // tracking (cho write)
    IQueryable<T> QueryAsNoTracking();            // no-tracking (cho read + projection)
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct);
    void Add(T entity);
    void AddRange(IEnumerable<T> entities);
    void Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct);
}
```

#### Specific repo — body rỗng hoặc thêm method nghiệp vụ

```csharp
// Body rỗng — CRUD đơn giản, base đủ dùng
public interface ICategoryRepository : IGenericRepository<PollutionCategory>;

// Có method nghiệp vụ riêng
public interface IReportRepository : IGenericRepository<Report>
{
    Task<Report?> GetForVerificationAsync(Guid id, CancellationToken ct);        // bundle Include
    Task<List<Report>> FindPotentialDuplicatesAsync(Point location, PollutionType type, CancellationToken ct);
}
```

#### `IUnitOfWork`

```csharp
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);         // commit + dispatch domain events
    Task<IDbTransaction> BeginTransactionAsync(CancellationToken ct);
}
```

#### Quy tắc cốt lõi

1. **Mọi entity đều có `IXxxRepository : IGenericRepository<T>`** — kể cả entity CRUD đơn giản. Body interface rỗng nếu không cần method riêng.
2. **`GenericRepository<T>` là `internal abstract`** trong Infrastructure — KHÔNG đăng ký DI open generic. Mỗi entity có class cụ thể kế thừa.
3. **`ApplicationDbContext` là `internal`** — chỉ Infrastructure nhìn thấy. Application layer **KHÔNG BAO GIỜ** import `IApplicationDbContext` hay `DbContext`.
4. **Handler chỉ inject `IXxxRepository` + `IUnitOfWork`**, không inject generic `IGenericRepository<T>` trực tiếp:
   ```csharp
   public sealed class VerifyReportCommandHandler(
       IReportRepository reports,
       IUserRepository users,
       IAuditLogRepository auditLogs,
       IUnitOfWork uow,
       ICurrentUser currentUser) : ...
   ```
5. **Không repo nào có `SaveChangesAsync`.** Commit qua `IUnitOfWork` duy nhất. Một transaction = 1 lần `uow.SaveChangesAsync()`, có thể ảnh hưởng nhiều entity.
6. **`TransactionBehavior` (MediatR pipeline)** tự bao `BeginTransactionAsync` / `CommitAsync` quanh mọi Command. Handler chỉ gọi `uow.SaveChangesAsync()` 1 lần.
7. **Domain events dispatch SAU `SaveChangesAsync` thành công** — implement trong `UnitOfWork`.
8. **Soft delete:** `repo.Remove(entity)` với `SoftDeletableEntity` được `SoftDeleteInterceptor` chuyển thành update `DeletedAt`. Hard delete chỉ qua job bảo trì (BR-AUTH-022).
9. **Specific repo chỉ thêm method khi có lý do cụ thể:**
   - ✅ Bundle Include phức tạp dùng ở nhiều handler (`GetForVerificationAsync`)
   - ✅ Query PostGIS / raw SQL (`FindPotentialDuplicatesAsync`)
   - ❌ Wrap lại `GetByIdAsync`, `ExistsAsync` — đã có ở base

#### DI Registration (trong `DependencyInjection.cs`)

```csharp
// ❌ KHÔNG đăng ký open generic
// services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

// ✅ Đăng ký từng repo cụ thể
services.AddScoped<IReportRepository, ReportRepository>();
services.AddScoped<IReportMediaRepository, ReportMediaRepository>();
services.AddScoped<IUserRepository, UserRepository>();
services.AddScoped<ICommentRepository, CommentRepository>();
services.AddScoped<IBadgeRepository, BadgeRepository>();
services.AddScoped<ICleanupTaskRepository, CleanupTaskRepository>();
services.AddScoped<IAuditLogRepository, AuditLogRepository>();
services.AddScoped<ICategoryRepository, CategoryRepository>();
services.AddScoped<INotificationRepository, NotificationRepository>();

services.AddScoped<IUnitOfWork, UnitOfWork>();
```

#### Anti-pattern cần tránh

| ❌ Sai | ✅ Đúng |
|---|---|
| `services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>))` | Đăng ký từng repo: `services.AddScoped<IReportRepository, ReportRepository>()` |
| Handler inject `IGenericRepository<Report>` | Handler inject `IReportRepository` |
| Handler inject `IApplicationDbContext` | Handler inject `IXxxRepository` + `IUnitOfWork` |
| `await reportRepo.SaveChangesAsync(ct)` | `await uow.SaveChangesAsync(ct)` |
| Entity CRUD đơn giản dùng DbContext trực tiếp | Entity CRUD đơn giản cũng có repo (body rỗng kế thừa base) |


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
| BR-AUTH-011 | Sai password 5 lần / 15' → lock 30'. **CAPTCHA từ lần 3** dùng Cloudflare Turnstile (xem `§14.4`): FE render widget, BE verify token qua Siteverify trước khi accept request login. Lưu attempt count + lockout_until trong User. |
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
| BR-SYS-004 | Rate limit công khai 60 rpm/IP anon, 300 rpm/user authed — **2 tầng**: (1) Cloudflare WAF Rate Limiting Rules ở edge để chặn DDoS sớm (xem `§14.3`); (2) `RateLimiterMiddleware` của ASP.NET Core 9 ở app làm last-line + áp policy theo `userId` (mà edge không thấy). |

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

- `appsettings.json` chỉ chứa giá trị **không nhạy cảm** (logging level, feature flags, R2 endpoint URL, Turnstile site key — site key public OK).
- Secrets:
  - Dev → `dotnet user-secrets`
  - Staging/Prod → environment variables hoặc Azure Key Vault / AWS Secrets Manager.
- **Nghiêm cấm** commit:
  - Connection string thật, JWT signing key
  - **R2 Access Key ID + Secret Access Key** (S3-compatible credentials)
  - **Cloudflare Turnstile secret key** (KHÁC site key — secret KHÔNG bao giờ vào client)
  - **Cloudflare API token** (nếu dùng cho cache purge, dynamic WAF rules)
  - FCM key, SMTP password
- Pattern bind: `Options<T>` + validation:
  ```csharp
  builder.Services.AddOptions<R2Options>()
      .Bind(builder.Configuration.GetSection("Cloudflare:R2"))
      .ValidateDataAnnotations()
      .ValidateOnStart();
  ```
- **Rotation:** R2 access keys rotate 90 ngày 1 lần; Turnstile secret key rotate khi có dấu hiệu lộ. Quy trình rotate ở `§13.4`.

### Feature flags

- Dùng `Microsoft.FeatureManagement` cho rollout AI, gamification badges mới…

---

## 9. Cross-cutting Concerns

### Logging

- Serilog, JSON output cho production. Log enricher: `RequestId`, `UserId`, `IPAddress`, `UserAgent` (cho audit BR-ADM-010).
- **IP thật khi sau Cloudflare:** `HttpContext.Connection.RemoteIpAddress` chỉ thấy IP của Cloudflare edge. Phải đọc từ header `CF-Connecting-IP` (Cloudflare đã chuẩn hoá, đáng tin) hoặc dùng `ForwardedHeadersMiddleware` cấu hình **whitelist Cloudflare IP ranges** (xem `§14.5`). KHÔNG tin `X-Forwarded-For` raw — bất kỳ ai cũng spoof được.
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
- Image storage: Cloudflare R2 (BR-SYS-002), Cloudflare Cache đứng trước public custom domain — egress đến internet là **0đ**. Xem `§14.2`.
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

## 13. Security

> **Nguyên tắc nền:** defense in depth. Mỗi tầng (edge/Cloudflare → app/ASP.NET → DB/Postgres) chịu trách nhiệm riêng và **không tin** tầng trước. Edge bị bypass thì app vẫn phải an toàn.

### 13.1. Threat model (rút gọn)

Các threat chính của hệ thống này theo STRIDE + OWASP API Top 10:

| Threat | Vector điển hình | Mitigation chính |
|---|---|---|
| Spoofing | Forge JWT, fake CF header | JWT signing key đủ mạnh + HS256→RS256 cho prod; CF-Connecting-IP chỉ tin nếu request đến từ Cloudflare IP range (`§14.5`) |
| Tampering | Sửa GPS/ảnh trên đường truyền, modify presigned URL | TLS 1.3 mandatory; presigned URL ký HMAC + TTL ≤ 5'; backend re-validate metadata |
| Repudiation | User chối từng làm hành động | Audit log đầy đủ (BR-ADM-010, retention 12 tháng) — xem `§9 Audit` |
| Information Disclosure | Lộ PII qua list endpoint, IDOR | BR-MAP-004 (round GPS), authorization policies (`§4.8`), DTO projection KHÔNG leak entity |
| DoS / DDoS | Flood API, large file upload, slow loris | Cloudflare WAF + Rate Limit (`§14.3`); ASP.NET rate limiter (BR-SYS-004); request body size limit; timeout |
| Elevation of Privilege | Citizen gọi endpoint Officer, role tampering | Policy-based authz; BR-AUTH-009 (chỉ Admin gán role); JWT claims server-signed |
| Injection | SQL injection, XSS reflected, command injection | EF Core parameterized queries (KHÔNG `FromSqlRaw` với input thô); FluentValidation; CSP header (`§13.6`); content-type sniffing block |
| Broken Auth | Credential stuffing, session fixation | Rate limit + Turnstile (BR-AUTH-011); refresh token rotation; bcrypt 12+ (BR-DAT-001) |
| Mass Assignment | Bind extra fields qua Command record | `record` immutable + chỉ public field cần thiết; KHÔNG dùng `[FromBody] User` raw |
| SSRF | AI service gọi URL từ user input | Allow-list domain cho mọi outbound HTTP; deny private CIDR (10/8, 172.16/12, 192.168/16, 169.254/16) |

### 13.2. Authentication hardening

Bổ sung quy tắc trên `§4.8` (đã định nghĩa JWT + roles + policies):

- **Password storage:** **bcrypt** với work factor ≥ 12 (BR-DAT-001). KHÔNG dùng `PasswordHasher<TUser>` mặc định của Identity (PBKDF2 mạnh nhưng cộng đồng OWASP ưu tiên bcrypt/argon2 cho ngành 2026). Cấu hình:
  ```csharp
  services.AddSingleton<IPasswordHasher<User>, BcryptPasswordHasher>();
  // BcryptPasswordHasher dùng BCrypt.Net-Next với workFactor: 12
  ```
- **JWT:** ký bằng **RS256** (asymmetric) cho prod — verifier có thể là service khác mà không cần share secret. HS256 chỉ chấp nhận trong dev. Thuật toán fix cứng phía verifier để chống `alg=none` attack:
  ```csharp
  options.TokenValidationParameters = new() {
      ValidAlgorithms = new[] { SecurityAlgorithms.RsaSha256 },
      // ... issuer, audience, signing key
  };
  ```
- **Refresh token rotation:** mỗi refresh sinh token mới + invalidate token cũ. Lưu hash (SHA-256) của token, không lưu plaintext. Detect reuse → revoke toàn bộ session của user (signal credential theft).
- **JWT claim tối thiểu:** `sub` (userId), `role`, `iat`, `exp`, `jti`. KHÔNG bỏ email/phone vào JWT — chúng sẽ rò qua log.
- **Token blacklist khi logout:** dùng Redis với TTL = remaining lifetime của JWT.
- **Brute-force protection:** sliding window theo `username` HOẶC `IP` (lấy max), không chỉ IP — attacker xoay IP qua botnet.
- **Account enumeration:** message "Email hoặc mật khẩu không đúng" giống nhau cho cả 2 case (email không tồn tại VS sai mật khẩu). KHÔNG trả khác nhau.

### 13.3. Authorization deep-dive

- **Policy-based** thay role string rải rác. Tất cả policy tập trung tại `Greenlens.Api/Authorization/Policies.cs`:
  ```csharp
  public static class Policies
  {
      public const string CanVerifyReport = nameof(CanVerifyReport);
      public const string CanAssignTeam   = nameof(CanAssignTeam);
      public const string CanExportData   = nameof(CanExportData);
      // ...
  }
  ```
- **Resource-based authorization** cho ownership check (vd. Citizen chỉ xoá báo cáo của chính mình, BR-REP-026): dùng `IAuthorizationService` + `IAuthorizationHandler<TRequirement, TResource>`, KHÔNG check `report.ReporterId == currentUser.Id` rải rác.
- **BR-OFF-004 (segregation of duties)** — Officer không tự verify báo cáo do mình tạo: kiểm tra trong handler, **không** ở filter (filter không thấy resource).
- **IDOR prevention:** mọi endpoint nhận `id` phải check ownership/scope trước khi load. Test bắt buộc:
  ```csharp
  [Fact]
  public async Task GetReport_OtherUserReport_Returns403_BR_DAT_003() { ... }
  ```

### 13.4. Secrets management & key rotation

- **Hierarchy:**
  1. **App secret** (JWT signing, R2 keys, Turnstile secret, FCM): Azure Key Vault / AWS Secrets Manager / HashiCorp Vault. **Không** environment variable trên prod (process listing rò).
  2. **Per-user secret** (refresh token, OTP): bcrypt-hashed trong DB.
  3. **Per-record secret** (encrypted EXIF, encrypted PII export): ASP.NET Core Data Protection API + key ring trong Vault.
- **Rotation cadence:**
  | Secret | Cadence | Mechanism |
  |---|---|---|
  | JWT signing key (RS256) | 90 ngày | Dual-key window: cấp token bằng key mới, verify cả 2 key trong 24h, drop key cũ |
  | R2 access key | 90 ngày | Tạo key mới ở Cloudflare → cấu hình app → revoke key cũ sau 24h grace |
  | Turnstile secret | khi nghi ngờ lộ | Tạo key mới + đổi cùng lúc FE/BE |
  | DB password | 180 ngày | Rolling restart |
  | bcrypt cost | review hàng năm | Tăng cost factor (12 → 13 khi hardware nhanh hơn) |
- **Rotation script:** `scripts/rotate-secrets.ps1` (PowerShell hoặc bash). Idempotent. Có dry-run.
- **Detection of leaked secrets:** GitGuardian / TruffleHog chạy trong CI. Nếu phát hiện → rotate ngay, audit Cloudflare access log để xem có ai đã dùng key đó.

### 13.5. Input & output security

- **Input validation 3 lớp** (mở rộng `§4.9`):
  1. Cloudflare WAF / OWASP ManagedRuleset chặn payload độc hại biết trước (`§14.3`).
  2. FluentValidation: shape, length, range, regex.
  3. Domain entity: invariant nghiệp vụ.
- **Output encoding:**
  - JSON response: `System.Text.Json` mặc định safe-encode. KHÔNG dùng `JsonSerializer` với `JavaScriptEncoder.UnsafeRelaxedJsonEscaping` cho user-facing output.
  - HTML email template (BR-NTF-001): dùng template engine có auto-escape (Razor, Scriban). KHÔNG concat string.
- **File upload security** (bổ sung `§4.10`):
  - Magic bytes check: dùng `MimeDetective` lib hoặc tự đọc 8-16 byte đầu để verify image/png, image/jpeg, image/webp, video/mp4. Reject nếu mismatch extension.
  - Image bomb / decompression bomb: limit dimensions (vd. max 8000×8000), max output bytes sau resize.
  - Re-encode ảnh server-side trước khi public (qua ImageSharp): xoá script/EXIF độc, normalize format.
- **Mass-assignment guard:** Command record chỉ liệt kê field user được set. Field server-controlled (Status, CreatedAt, ReporterId nội bộ) **không** ở Command, set trong handler.

### 13.6. HTTP security headers

Dùng package **`OwaspHeaders.Core`** — middleware đặt full set OWASP-recommended headers chỉ với 1 dòng:

```csharp
app.UseSecureHeadersMiddleware(SecureHeadersMiddlewareBuilder
    .CreateBuilder()
    .UseHsts(maxAge: 31_536_000, includeSubDomains: true)            // 1 năm, BR-DAT-001 TLS
    .UseContentTypeOptions()                                          // X-Content-Type-Options: nosniff
    .UseContentSecurityPolicy(builder => builder
        .WithDefaultSrc(s => s.Self())
        .WithScriptSrc(s => s.Self()
            .From("https://challenges.cloudflare.com"))               // Turnstile JS
        .WithImgSrc(s => s.Self()
            .From("data:")
            .From("https://media.ecoreport.example"))                 // R2 public domain
        .WithConnectSrc(s => s.Self()
            .From("https://challenges.cloudflare.com")))
    .UseXFrameOptions(XFrameOptions.Deny)
    .UseReferrerPolicy(ReferrerPolicy.NoReferrer)
    .UsePermittedCrossDomainPolicies(XPermittedCrossDomainPolicies.None)
    .UseCrossOriginOpenerPolicy(CrossOriginOpenerPolicy.SameOrigin)
    .UseCrossOriginResourcePolicy(CrossOriginResourcePolicy.SameOrigin)
    .Build());
```

Header set tối thiểu **bắt buộc**:
- `Strict-Transport-Security: max-age=31536000; includeSubDomains` — force HTTPS, BR-DAT-001.
- `Content-Security-Policy: default-src 'self'; ...` — chống XSS. Cho phép `https://challenges.cloudflare.com` cho Turnstile JS.
- `X-Content-Type-Options: nosniff` — chống MIME sniffing.
- `X-Frame-Options: DENY` — chống clickjacking (mobile app render webview).
- `Referrer-Policy: no-referrer` — không leak URL ra third-party.
- `Cross-Origin-Resource-Policy: same-origin` — chống Spectre-style cross-origin reads.

### 13.7. CORS

Public API (Map, public reports list cho citizen) cần CORS, nhưng **không bao giờ** `AllowAnyOrigin().AllowCredentials()` — đó là reflective CORS, security hole. Pattern:

```csharp
services.AddCors(options =>
{
    // Public API: chỉ GET, không credentials, origin nào cũng OK
    options.AddPolicy("PublicApi", p => p
        .WithOrigins("https://ecoreport.example", "https://m.ecoreport.example")
        .WithMethods("GET")
        .WithHeaders("Content-Type"));

    // Authenticated API: strict origin list, allow credentials
    options.AddPolicy("AuthedApi", p => p
        .WithOrigins("https://app.ecoreport.example")    // FE web
        .AllowCredentials()
        .AllowAnyMethod()
        .AllowAnyHeader());
});
```

Áp policy theo controller/action: `[EnableCors("AuthedApi")]`. KHÔNG đặt CORS toàn cục với `AllowAnyOrigin`.

### 13.8. Rate limiting (app layer)

Cloudflare WAF chặn ở edge (`§14.3`). App layer là **last line** + áp policy theo `userId` mà edge không thấy:

```csharp
services.AddRateLimiter(options =>
{
    // BR-SYS-004: 60 rpm/IP cho anonymous
    options.AddPolicy("anon-ip", ctx => RateLimitPartition.GetSlidingWindowLimiter(
        partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
        factory: _ => new() { PermitLimit = 60, Window = TimeSpan.FromMinutes(1), SegmentsPerWindow = 6 }));

    // BR-SYS-004: 300 rpm/user authed
    options.AddPolicy("user", ctx => RateLimitPartition.GetSlidingWindowLimiter(
        partitionKey: ctx.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anon",
        factory: _ => new() { PermitLimit = 300, Window = TimeSpan.FromMinutes(1), SegmentsPerWindow = 6 }));

    // BR-REP-010: 5 reports/h, 20/24h per Citizen — áp riêng cho POST /reports
    options.AddPolicy("submit-report", ctx => /* sliding window 5/1h, dùng Redis backing */);

    options.OnRejected = async (ctx, ct) =>
    {
        ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        ctx.HttpContext.Response.Headers["Retry-After"] = "60";
        await ctx.HttpContext.Response.WriteAsJsonAsync(new { error = "rate_limit_exceeded" }, ct);
    };
});
```

**Quan trọng:** ASP.NET rate limiter mặc định in-memory, không share giữa instance. Production scale ngang → backing bằng Redis (`Microsoft.AspNetCore.RateLimiting.Redis` hoặc tự dùng Redis sorted set). BR-REP-010 (5/h, 20/24h) **bắt buộc** dùng Redis.

### 13.9. Data privacy & compliance (BR-DAT-001..005)

Đã rải rác ở các mục khác, gom đây để tra cứu nhanh:

- **At-rest encryption:** Postgres TDE (Transparent Data Encryption) hoặc volume-level (LUKS). R2: server-side encryption mặc định.
- **In-transit:** TLS 1.2+ (`§13.6` HSTS). Cloudflare → origin: dùng **Authenticated Origin Pulls** (mTLS) để origin chỉ chấp nhận traffic từ Cloudflare (`§14.5`).
- **PII columns** trong DB: encrypt application-level qua Data Protection API cho cột rất nhạy (email phụ, CCCD nếu có). Cột bắt buộc index (email, phone) thì hash deterministic + lưu plaintext (chấp nhận trade-off).
- **GDPR / Nghị định 13/2023/NĐ-CP:**
  - Export: BR-DAT-003 — endpoint `GET /me/export` trả ZIP gồm user data + reports + comments + audit log của user (12 tháng gần nhất).
  - Erasure: BR-AUTH-022 soft-delete 90 ngày → hard-delete + báo cáo `Anonymized`.
  - Consent: BR-DAT-005 — log consent ở `consent_log` table, `(user_id, scope, version, granted_at)`.
- **Right to know:** GET /me/data-access-log trả các lần Officer/Admin xem dữ liệu cá nhân của user (BR-OFF-022).

### 13.10. Vulnerability management

- **Dependencies:** `dotnet list package --vulnerable --include-transitive` chạy weekly trong CI. Critical/High → block merge.
- **SAST:** SonarQube hoặc GitHub CodeQL chạy mỗi PR.
- **DAST:** OWASP ZAP baseline scan trên staging mỗi release.
- **Secret scanning:** GitGuardian/TruffleHog trong CI.
- **Pen-test:** trước go-live + annually. Out-of-scope cho project capstone, nhưng note để team hand-off.
- **Security advisories:** subscribe GitHub Dependabot + Microsoft Security Response Center (MSRC).

### 13.11. Incident response

- **On-call playbook** ngắn (capstone scope):
  1. Detect: alert từ Serilog (level Error spike), Cloudflare (DDoS notification), uptime check.
  2. Triage: severity (P0=data breach / system down, P1=feature broken cho >50% users, P2/P3).
  3. Contain: rotate secret nghi ngờ lộ, disable feature flag, block IP/ASN ở Cloudflare WAF.
  4. Eradicate: fix + test + deploy hotfix.
  5. Recover: re-enable feature, monitor 24h.
  6. Post-mortem trong 5 ngày: blameless, RCA, action items.
- **Communication:** email supervisor + 1 team lead trong 1h cho P0/P1.

---

## 14. Cloudflare Integration

Cloudflare đứng **trước toàn bộ traffic public** của hệ thống (web app, mobile API endpoint, R2 media). Các sản phẩm đang dùng:

| Sản phẩm | Vai trò | Section |
|---|---|---|
| **DNS** | Authoritative DNS cho `ecoreport.example` | `§14.1` |
| **CDN + Cache** | Cache static + API GET responses, edge ở 300+ POP | `§14.2` |
| **R2** | Object storage (ảnh, video báo cáo) | `§14.2` |
| **WAF + Rate Limiting** | Chặn OWASP threats + DDoS ở edge | `§14.3` |
| **Turnstile** | CAPTCHA-alternative cho login & form công khai | `§14.4` |
| **Authenticated Origin Pulls (mTLS)** | Origin chỉ accept traffic từ Cloudflare | `§14.5` |
| **Logs & Analytics** | Edge logs + WAF events → SIEM | `§14.6` |

### 14.1. DNS & TLS

- DNS records orange-cloud (proxied) cho mọi subdomain public: `app`, `api`, `media`, `m`. Origin record vd. `origin.ecoreport.example` grey-cloud (DNS-only) — chỉ dùng cho deploy/admin, **firewall** chỉ allow IP văn phòng / VPN.
- SSL/TLS mode: **Full (strict)** — origin phải có cert hợp lệ (Let's Encrypt OK). KHÔNG dùng "Flexible" (CF→origin plaintext, vô nghĩa cho security).
- TLS 1.2 minimum ở edge; ưu tiên TLS 1.3.
- HSTS bật ở Cloudflare (1 năm, includeSubDomains, preload nếu domain lên HSTS preload list).

### 14.2. R2 + CDN cho media

**Setup:**
1. Tạo bucket `ecoreport-media` (production) + `ecoreport-media-staging`.
2. Custom domain: connect bucket vào `media.ecoreport.example` (Cloudflare R2 hỗ trợ Custom Domains, traffic đi qua Cloudflare cache tự động).
3. R2 API token: Object Read & Write, scope chỉ tới bucket cần (KHÔNG account-wide).
4. **KHÔNG public bucket qua `*.r2.dev`** — đó là dev preview, rate-limited, không phục vụ production.

**Backend code:**
```csharp
// appsettings.json
{
  "Cloudflare": {
    "R2": {
      "AccountId": "...",
      "Endpoint": "https://<account-id>.r2.cloudflarestorage.com",
      "PublicBaseUrl": "https://media.ecoreport.example",
      "Bucket": "ecoreport-media"
      // AccessKeyId + SecretAccessKey từ Vault, KHÔNG ở appsettings
    }
  }
}

// Infrastructure/Storage/R2FileStorage.cs
public sealed class R2FileStorage(IAmazonS3 s3, IOptions<R2Options> opt) : IFileStorage
{
    public async Task<string> CreatePresignedPutUrlAsync(string key, string contentType, TimeSpan ttl, CancellationToken ct)
    {
        var req = new GetPreSignedUrlRequest
        {
            BucketName = opt.Value.Bucket,
            Key        = key,
            Verb       = HttpVerb.PUT,
            Expires    = DateTime.UtcNow.Add(ttl),
            ContentType = contentType,
        };
        return await s3.GetPreSignedURLAsync(req);
    }

    public string GetPublicUrl(string key) => $"{opt.Value.PublicBaseUrl}/{key}";
}

// DI
services.AddSingleton<IAmazonS3>(sp =>
{
    var o = sp.GetRequiredService<IOptions<R2Options>>().Value;
    return new AmazonS3Client(
        o.AccessKeyId,
        o.SecretAccessKey,
        new AmazonS3Config
        {
            ServiceURL  = o.Endpoint,
            ForcePathStyle = true,           // R2 thích path-style
            // Vùng để "auto" hoặc "us-east-1" alias
        });
});
```

**Cache rules cho `media.ecoreport.example`:**
- Cache Level: Standard
- Edge Cache TTL: 1 năm (ảnh không đổi vì key chứa hash)
- Browser Cache TTL: 1 năm
- `Cache-Control: public, max-age=31536000, immutable` từ R2 metadata khi PUT
- Purge: dùng Cloudflare API token (scope: Cache Purge) khi cần invalidate

### 14.3. WAF + Rate Limiting

**Managed Rules** (bật ngay từ start):
- Cloudflare Managed Ruleset
- OWASP ManagedRuleset (Paranoia Level 1 cho start, tăng dần khi không có false positive)
- Cloudflare Exposed Credentials Check

**Custom Rules** (tinh chỉnh cho project):
```
# Block known-bad bots ngay lập tức
(cf.client.bot) and not (cf.verified_bot_category in {"Search Engine Crawler"})
→ Action: Block

# Challenge khi truy cập /api/auth/login từ ASN nghi ngờ
(http.request.uri.path eq "/api/auth/login") and (ip.geoip.asnum in {... botnet ASN list})
→ Action: Managed Challenge

# Chặn upload file extension lạ ở /api/media/presign
(http.request.uri.path eq "/api/media/presign") and not (http.request.body.raw contains "image/" or http.request.body.raw contains "video/")
→ Action: Block
```

**Rate Limiting Rules** (edge — bổ sung BR-SYS-004 ở app layer):
| Path | Threshold | Action |
|---|---|---|
| `/api/auth/login` | 10 req/IP/10s | Block 5 phút |
| `/api/auth/register` | 5 req/IP/1m | Block 10 phút |
| `/api/reports` POST | 60 req/IP/1m | Challenge |
| `/api/*` (catch-all anon) | 100 req/IP/1m | Block 1 phút |

**Bot Fight Mode:** bật. Super Bot Fight Mode nếu lên paid tier.

**DDoS protection:** L3/L4 mặc định, L7 auto. Page rules tắt `Always Online` cho `/api/*` (API không nên serve cached khi origin down — trả 503 thật để client retry với backoff).

### 14.4. Turnstile (BR-AUTH-011)

**Khi nào trigger:** từ lần login sai thứ 3 (BR-AUTH-011). Cũng khuyến nghị bật cho:
- Đăng ký tài khoản (BR-AUTH-001)
- Quên mật khẩu (BR-AUTH-015)
- Submit báo cáo ẩn danh (BR-AUTH-014) — tránh lạm dụng

**Setup:**
1. Tạo Turnstile widget ở Cloudflare dashboard → lấy **site key** (public, FE) + **secret key** (private, BE).
2. Thêm vào allowed hostnames: `app.ecoreport.example`, `m.ecoreport.example`. Chặn localhost trong production widget (dùng test sitekey cho dev).
3. Mode: **Managed** (recommended) — Cloudflare tự chọn invisible challenge hay checkbox.

**Client (FE):**
```html
<script src="https://challenges.cloudflare.com/turnstile/v0/api.js" async defer></script>
<form id="login-form">
  <input name="email" />
  <input name="password" type="password" />
  <div class="cf-turnstile" data-sitekey="0xAAAA..." data-action="login"></div>
  <button type="submit">Login</button>
</form>
```

Khi submit, hidden input `cf-turnstile-response` (token) đi cùng form data.

**Backend verify:**
```csharp
// Application/Common/Interfaces/ITurnstileVerifier.cs
public interface ITurnstileVerifier
{
    Task<TurnstileResult> VerifyAsync(string token, string? remoteIp, string? expectedAction, CancellationToken ct);
}

public sealed record TurnstileResult(bool Success, string[] ErrorCodes, string? Action, string? Hostname);

// Infrastructure/Security/TurnstileVerifier.cs
public sealed class TurnstileVerifier(HttpClient http, IOptions<TurnstileOptions> opt) : ITurnstileVerifier
{
    private const string SiteVerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    public async Task<TurnstileResult> VerifyAsync(string token, string? remoteIp, string? expectedAction, CancellationToken ct)
    {
        var payload = new Dictionary<string, string> {
            ["secret"]   = opt.Value.SecretKey,
            ["response"] = token,
        };
        if (!string.IsNullOrEmpty(remoteIp)) payload["remoteip"] = remoteIp;

        // Siteverify CHỈ chấp nhận POST với form-urlencoded hoặc JSON
        var resp = await http.PostAsync(SiteVerifyUrl, new FormUrlEncodedContent(payload), ct);
        var json = await resp.Content.ReadFromJsonAsync<TurnstileSiteverifyResponse>(cancellationToken: ct)
                   ?? new(false, [], null, null, null);

        // Validate hostname & action nếu có
        if (json.Success && expectedAction is not null && json.Action != expectedAction)
            return new(false, new[] { "action-mismatch" }, json.Action, json.Hostname);

        return new(json.Success, json.ErrorCodes ?? [], json.Action, json.Hostname);
    }
}

// Đăng ký HttpClient với resilience
services.AddHttpClient<ITurnstileVerifier, TurnstileVerifier>()
    .AddStandardResilienceHandler();   // .NET 9: built-in Polly preset
```

**Quy tắc bắt buộc (theo Cloudflare docs):**
- BE là **người gọi duy nhất** Siteverify. KHÔNG bao giờ gọi từ FE — secret key sẽ lộ.
- Token sống tối đa 5 phút. Hết hạn → FE refresh widget.
- Token **single-use**. Re-submit → `timeout-or-duplicate` error.
- Verify token **TRƯỚC** khi xử lý nghiệp vụ login. Nếu fail → trả 401 chung chung (đừng leak token cụ thể fail vì sao).
- Validate `action` field khi widget có `data-action` — chống token reuse cross-form.
- Validate `hostname` field — chống widget bị nhúng ở domain khác.
- Dummy keys cho test (xem Cloudflare docs Testing): luôn pass / luôn fail / spam token. KHÔNG dùng prod key trong CI.

### 14.5. Authenticated Origin Pulls (mTLS)

Origin (server .NET 9) chỉ accept TLS connection có client cert do Cloudflare phát hành. Chống bypass Cloudflare bằng cách scan IP origin trực tiếp.

**Setup:**
1. Cloudflare dashboard → SSL/TLS → Origin Server → Authenticated Origin Pulls → tải Cloudflare's CA cert.
2. Reverse proxy (nginx/Caddy/Cloudflare Tunnel) cấu hình require client cert + verify với CA cert đó.
3. Alternative đơn giản hơn: **Cloudflare Tunnel** (cloudflared daemon ở origin) — không cần expose public port nào, traffic đi qua tunnel. Khuyến nghị cho project capstone vì dễ setup.

**Forwarded headers (đọc IP thật từ Cloudflare):**
```csharp
// Program.cs
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardedForHeaderName = "CF-Connecting-IP";   // Cloudflare-specific, đáng tin
    options.ForwardLimit = 1;
    // Chỉ tin Cloudflare IP ranges
    foreach (var cidr in CloudflareIpRanges.V4) options.KnownNetworks.Add(IPNetwork.Parse(cidr));
    foreach (var cidr in CloudflareIpRanges.V6) options.KnownNetworks.Add(IPNetwork.Parse(cidr));
});

// Middleware order matters
app.UseForwardedHeaders();   // PHẢI trước UseAuthentication / UseRateLimiter
```

`CloudflareIpRanges` lấy từ `https://www.cloudflare.com/ips-v4` và `ips-v6`, refresh weekly bằng background job.

### 14.6. Logging & analytics

- **Cloudflare Logs:** push log từ edge sang storage (R2 chính nó / S3 / GCS) qua Logpush. Capstone scope: enable Web Analytics free tier — đủ để xem traffic + WAF events trong dashboard.
- **WAF events** (block/challenge): nếu volume cao → push qua Logpush vào Seq/ELK của project, correlate với app log qua `CF-Ray` header (mỗi request Cloudflare gắn một Ray ID, log ở app cùng RequestId để trace end-to-end).

### 14.7. Cost & limits (capstone-relevant)

| Resource | Free tier | Khi nào trả phí |
|---|---|---|
| R2 storage | 10 GB | > 10 GB ($0.015/GB/tháng) |
| R2 Class A ops (PUT/POST/LIST) | 1M/tháng | rất khó vượt cho capstone |
| R2 Class B ops (GET/HEAD) | 10M/tháng | nếu truyền thông mạnh có thể chạm |
| R2 egress | **0đ vĩnh viễn** | luôn miễn phí |
| Turnstile | unlimited | luôn free |
| Cloudflare Pro plan | $20/tháng | nếu muốn WAF advanced + Image Resizing |
| Workers (nếu dùng) | 100k req/ngày | rất ít project capstone đụng đến |

Capstone scope **không** cần lên Pro plan — Free tier đủ cho 5,000 CCU mục tiêu nếu tận dụng Cache đúng.

### 14.8. Disaster recovery & vendor lock-in

- **R2 → AWS S3 fallback:** code dùng `IAmazonS3` (interface chuẩn), đổi endpoint là chuyển được. Backup hàng đêm: Cloudflare Sippy hoặc rclone từ R2 → S3 cold storage. RPO ≤ 24h (BR-DAT-004) đảm bảo nếu R2 down toàn region (chưa từng xảy ra, nhưng plan vẫn cần).
- **Turnstile fallback:** nếu Cloudflare challenge endpoint down, BE có flag `IsTurnstileMandatory` — set false để bypass tạm thời và tăng cost rate limit thay thế. KHÔNG hardcode skip.
- **DNS fallback:** secondary DNS provider (vd. AWS Route 53) đồng bộ records, nếu Cloudflare DNS down có thể switch trong < 5 phút.

---

## 15. Glossary nhanh (xem Phụ lục B của BR doc)

- **SLA** — Service Level Agreement. BR-OFF-002 (verify 24h), BR-OFF-020 (resolve theo severity).
- **Hotspot** — ≥ 10 báo cáo cùng loại / 500m / 30 ngày (BR-MAP-010).
- **Duplicate** — cùng loại + ≤ 50m + ≤ 24h (BR-REP-030).
- **PII** — email, SĐT, GPS chi tiết, CCCD. KHÔNG log, KHÔNG export trừ admin approval (BR-OFF-022).
- **EXIF** — metadata ảnh; strip GPS nhạy cảm trước khi gửi AI (BR-AI-007).
- **Anonymized** — user xóa tài khoản nhưng báo cáo còn lại, ẩn danh người gửi (BR-AUTH-022).
- **R2** — Cloudflare R2, S3-compatible object storage, zero egress (xem `§14.2`).
- **Turnstile** — Cloudflare CAPTCHA-alternative, replace reCAPTCHA (xem `§14.4`).
- **Siteverify** — endpoint `https://challenges.cloudflare.com/turnstile/v0/siteverify` để BE verify Turnstile token (POST only, không GET).
- **CSP** — Content Security Policy header, ngăn XSS (xem `§13.6`).
- **HSTS** — HTTP Strict Transport Security, force HTTPS (xem `§13.6`).
- **mTLS / Authenticated Origin Pulls** — origin chỉ tin TLS có client cert do Cloudflare ký (`§14.5`).
- **CF-Connecting-IP** — header Cloudflare gắn chứa IP thật của client; tin được vì traffic phải qua Cloudflare.
- **CF-Ray** — request ID Cloudflare gắn để trace end-to-end giữa edge và origin log.

---

**Phiên bản CLAUDE.md:** 1.2
**Đồng bộ với:** `SU26SE049_BusinessRules_v1_0.docx` v1.0 (17/04/2026).
**Changelog:**
- v1.2 (2026-05-09): Thêm `§4.12 Repository & Unit of Work` (hybrid pattern: aggregate-specific repo + UoW, KHÔNG generic repository thuần). Cập nhật `§3` folder structure: thêm `Common/Interfaces/Persistence/`, `UnitOfWork.cs`.
- v1.1 (2026-05-09): Thêm `§13 Security` + `§14 Cloudflare Integration`. Đổi AWS S3 → Cloudflare R2.
- v1.0: Phiên bản đầu.

Khi BR doc cập nhật → cập nhật §5 và `Application/BusinessRules/*.cs` constants tương ứng.
Khi tech stack hoặc Cloudflare config thay đổi → cập nhật `§2`, `§13`, `§14` và bump phiên bản.