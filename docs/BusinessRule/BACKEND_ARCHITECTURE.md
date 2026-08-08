# GreenLens — Back-end Architecture

> **Dự án:** SU26SE049 — Crowdsourced Application for Reporting Environmental Pollution  
> **Stack:** .NET 9 · ASP.NET Core 9 · EF Core 9 · PostgreSQL 18 + PostGIS · Redis · Cloudflare R2 · Hangfire · Firebase FCM  
> **Pattern:** Clean Architecture · CQRS (MediatR) · Vertical Slice · Result Pattern

---

## 1. Back-end Architecture Overview

```mermaid
graph TB
    subgraph BackendServer["🖥️ Back-end Server (.NET 9)"]
        direction TB

        HTTP["<b>ASP.NET Core 9</b><br/>Kestrel HTTP Server<br/>JWT Bearer Authentication<br/>Rate Limiting Middleware"]

        CTRL["<b>API Controllers</b><br/>AuthController · ReportsController<br/>InspectionsController · CompaniesController<br/>CommentsController · TeamsController<br/>MapController · AdminController<br/>MediaController · NotificationsController"]

        PIPE["<b>MediatR Pipeline</b><br/>① LoggingBehavior<br/>② ValidationBehavior (FluentValidation)<br/>③ TransactionBehavior<br/>④ AuditLogBehavior"]

        HANDLER["<b>Application Handlers</b><br/>Command Handlers (mutate)<br/>Query Handlers (read-only)<br/>Domain Event Handlers"]

        DOMAIN["<b>Domain Layer</b><br/>Entities (Report, User, InspectionReport…)<br/>Value Objects (GeoLocation, Email…)<br/>Domain Events · Enums<br/>State Machines · Business Rules"]

        INFRA["<b>Infrastructure Layer</b><br/>EF Core 9 (ApplicationDbContext)<br/>Repository Implementations<br/>External Service Adapters"]

        HTTP -->|"HTTP Request<br/>→ Route matching"| CTRL
        CTRL -->|"ISender.Send(command/query)"| PIPE
        CTRL -.->|"Result → ToActionResult()<br/>→ HTTP Response"| HTTP
        PIPE -->|"Pipeline chain"| HANDLER
        HANDLER -->|"uses Domain entities<br/>+ business logic"| DOMAIN
        HANDLER -->|"uses interfaces<br/>(DI injected)"| INFRA
        INFRA -.->|"Domain Events<br/>raised by entities"| HANDLER
    end

    subgraph DataStores["🗄️ Data Stores"]
        direction LR
        PG[("🐘 <b>PostgreSQL 18</b><br/>+ PostGIS<br/>Primary Database")]
        RD[("⚡ <b>Redis</b><br/>Cache · Rate Limit<br/>Map Cache 10'")]
    end

    subgraph ObjectStorage["📦 Object Storage"]
        S3[("☁️ <b>Cloudflare R2</b><br/>Report Images<br/>Presigned URL Upload")]
    end

    subgraph ExternalAPIs["🌐 External Services"]
        direction LR
        AI["🤖 <b>AI Service</b><br/>Image Classification<br/>Duplicate Detection<br/>(DINOv2)"]
        FCM["🔔 <b>Firebase FCM</b><br/>Push Notifications"]
        SMTP["📧 <b>SMTP</b><br/>Email (OTP, Alerts)"]
    end

    subgraph BGWorker["⏰ Background Worker"]
        HF["<b>Hangfire Server</b><br/>AutoCloseResolvedReportJob<br/>SlaBreachVerificationJob<br/>DuplicateDetectionJob (AI Tier 2)<br/>DraftCleanupJob<br/>LeaderboardSnapshotJob"]
    end

    INFRA -->|"EF Core ORM<br/>LINQ + PostGIS"| PG
    INFRA -->|"IDistributedCache"| RD
    INFRA -->|"Presigned URL<br/>Upload/Download"| S3
    INFRA -->|"HTTP Client<br/>classify / compare"| AI
    INFRA -->|"FCM SDK"| FCM
    INFRA -->|"SmtpClient"| SMTP

    PG -.->|"query results"| INFRA

    HF -->|"enqueue via<br/>ISender / DbContext"| HANDLER
    HF -->|"direct DB access"| PG

    style HTTP fill:#fce4b2,stroke:#e8a735,color:#333
    style CTRL fill:#f5c6c6,stroke:#e74c3c,color:#333
    style PIPE fill:#d5f5e3,stroke:#27ae60,color:#333
    style HANDLER fill:#d4e6f1,stroke:#2980b9,color:#333
    style DOMAIN fill:#d5f5e3,stroke:#1e8449,color:#333,stroke-width:3px
    style INFRA fill:#d4e6f1,stroke:#2471a3,color:#333

    style PG fill:#f0f0f0,stroke:#336791,color:#333
    style RD fill:#f0f0f0,stroke:#d32f2f,color:#333
    style S3 fill:#f0f0f0,stroke:#ff9800,color:#333
    style AI fill:#fff3e0,stroke:#e65100,color:#333
    style FCM fill:#fff3e0,stroke:#ff6f00,color:#333
    style SMTP fill:#fff3e0,stroke:#ff6f00,color:#333
    style HF fill:#ede7f6,stroke:#7b1fa2,color:#333
```

---

## 2. Layered Detail — Request Flow

```mermaid
graph TB
    subgraph Layer1["Layer 1 — HTTP / Middleware"]
        direction LR
        K["Kestrel<br/>HTTP Server"]
        MW1["ExceptionHandling<br/>Middleware"]
        MW2["RequestLogging<br/>Middleware"]
        MW3["RateLimiter<br/>Middleware<br/>(60 rpm anon /<br/>300 rpm authed)"]
        MW4["JWT Authentication<br/>+ Authorization"]
        K --> MW1 --> MW2 --> MW3 --> MW4
    end

    subgraph Layer2["Layer 2 — API Controllers (Greenlens.Api)"]
        direction LR
        C1["AuthController"]
        C2["ReportsController"]
        C3["InspectionsController"]
        C4["CompaniesController"]
        C5["Others..."]
    end

    subgraph Layer3["Layer 3 — MediatR Pipeline (Greenlens.Application)"]
        direction TB
        B1["① LoggingBehavior<br/>→ Serilog structured log"]
        B2["② ValidationBehavior<br/>→ FluentValidation rules"]
        B3["③ TransactionBehavior<br/>→ Begin / Commit / Rollback"]
        B4["④ AuditLogBehavior<br/>→ Who did what, when"]
        B1 --> B2 --> B3 --> B4
    end

    subgraph Layer4["Layer 4 — Command/Query Handlers (Greenlens.Application)"]
        direction LR
        CMD["Command Handlers<br/>(SubmitReport, VerifyReport,<br/>AssignTeam, IssuePenalty…)"]
        QRY["Query Handlers<br/>(GetNearby, GetAuditLogs,<br/>GetLeaderboard…)"]
        EVT["Domain Event Handlers<br/>(AwardPoints,<br/>SendNotification…)"]
    end

    subgraph Layer5["Layer 5 — Domain (Greenlens.Domain)"]
        direction LR
        ENT["Entities<br/>Report · User<br/>InspectionReport<br/>ReportAssignment<br/>Comment · Badge"]
        SM["State Machines<br/>Report Lifecycle<br/>Inspection Lifecycle<br/>Company Status"]
        VO["Value Objects<br/>GeoLocation<br/>Email · Money"]
        DE["Domain Events<br/>ReportVerifiedEvent<br/>StatusChangedEvent<br/>BadgeEarnedEvent"]
    end

    subgraph Layer6["Layer 6 — Infrastructure (Greenlens.Infrastructure)"]
        direction LR
        EF["EF Core 9<br/>ApplicationDbContext<br/>+ Configurations<br/>+ Interceptors"]
        SVC["Service Adapters<br/>JwtService<br/>BcryptPasswordHasher<br/>R2FileStorageService<br/>PostGisDistanceService"]
        EXT["External Clients<br/>AiClassificationService<br/>AiImageCompareService<br/>FcmPushSender<br/>SmtpEmailSender"]
    end

    Layer1 -->|"route + dispatch"| Layer2
    Layer2 -->|"ISender.Send()"| Layer3
    Layer3 -->|"next()"| Layer4
    Layer4 -->|"domain logic"| Layer5
    Layer4 -->|"via interfaces (DI)"| Layer6
    Layer6 -->|"DB / External calls"| DB[("PostgreSQL<br/>+ PostGIS")]

    Layer2 -.->|"Result~T~ → ToActionResult()<br/>→ ProblemDetails (RFC 7807)"| Layer1

    style Layer1 fill:#fce4b2,stroke:#e8a735
    style Layer2 fill:#f5c6c6,stroke:#e74c3c
    style Layer3 fill:#d5f5e3,stroke:#27ae60
    style Layer4 fill:#d4e6f1,stroke:#2980b9
    style Layer5 fill:#d5f5e3,stroke:#1e8449,stroke-width:3px
    style Layer6 fill:#d4e6f1,stroke:#2471a3
    style DB fill:#f0f0f0,stroke:#336791
```

---

## 3. Dependency Rule (Clean Architecture)

```mermaid
graph TB
    subgraph Outer["Outermost — Frameworks & Drivers"]
        API["<b>Greenlens.Api</b><br/>Controllers · Middleware<br/>Filters · Program.cs<br/><i>Composition Root</i>"]
        INFRA2["<b>Greenlens.Infrastructure</b><br/>EF Core · PostgreSQL · PostGIS<br/>Redis · R2/S3 · FCM · SMTP<br/>AI Client · Hangfire Jobs<br/>JWT · Bcrypt · Serilog"]
    end

    subgraph Middle["Middle — Application Business Rules"]
        APP2["<b>Greenlens.Application</b><br/>Features/ (Vertical Slices)<br/>Commands · Queries · Handlers<br/>Validators · Behaviors<br/>Interfaces (contracts)"]
    end

    subgraph Core["Innermost — Enterprise Business Rules"]
        DOM2["<b>Greenlens.Domain</b><br/>Entities · Value Objects<br/>Domain Events · Enums<br/>State Machines · Exceptions<br/>Specifications"]
    end

    API -->|"depends on"| APP2
    API -.->|"DI registration only"| INFRA2
    APP2 -->|"depends on"| DOM2
    INFRA2 -->|"implements interfaces from"| APP2
    INFRA2 -->|"depends on"| DOM2

    DOM2 -.-x|"❌ KHÔNG phụ thuộc<br/>bất kỳ layer nào"| API
    DOM2 -.-x|"❌ KHÔNG phụ thuộc<br/>bất kỳ layer nào"| INFRA2

    style Core fill:#2d5016,stroke:#4a8c2a,color:#fff,stroke-width:3px
    style Middle fill:#1a3a5c,stroke:#2980b9,color:#fff
    style API fill:#3a1a5c,stroke:#8e44ad,color:#fff
    style INFRA2 fill:#5c3a1a,stroke:#b97829,color:#fff
```

---

## 4. Component Mapping

| Layer | Project | Trách nhiệm | Tech |
|-------|---------|-------------|------|
| **HTTP** | `Greenlens.Api` | Routing, Auth, Rate Limit, Error Handling, DI | ASP.NET Core 9, Kestrel |
| **Controller** | `Greenlens.Api/Controllers/` | Nhận HTTP request → dispatch MediatR → trả HTTP response | API Controllers, `ISender` |
| **Pipeline** | `Greenlens.Application/Common/Behaviors/` | Cross-cutting: Logging, Validation, Transaction, Audit | MediatR `IPipelineBehavior` |
| **Business Logic** | `Greenlens.Application/Features/` | Use case handlers (CQRS), vertical slices | MediatR `IRequestHandler` |
| **Domain** | `Greenlens.Domain/` | Entities, state machines, domain events, business rules | Pure C# (no framework deps) |
| **ORM / Adapters** | `Greenlens.Infrastructure/` | DB access, external service adapters, background jobs | EF Core 9, Hangfire |
| **Database** | External | Primary data store + spatial queries | PostgreSQL 18 + PostGIS |
| **Cache** | External | Map cache (10'), rate limit counters, session | Redis |
| **Object Storage** | External | Report images, videos (presigned URL upload) | Cloudflare R2 (S3-compatible) |
| **AI** | External | Image classification, duplicate detection (DINOv2) | Python FastAPI |
| **Push** | External | Mobile push notifications | Firebase FCM |
| **Email** | External | OTP, alerts, notification digest | SMTP |
| **Background** | `Greenlens.Infrastructure/BackgroundJobs/` | Recurring/scheduled jobs | Hangfire |

---

## 5. Key Design Decisions

### 5.1 Tại sao Clean Architecture?

```
✅ Domain KHÔNG phụ thuộc framework → dễ unit test
✅ Application chỉ biết interfaces → swap implementation dễ dàng
✅ Infrastructure bọc tất cả external dependency → isolate thay đổi
✅ Api layer mỏng → chỉ routing + DI
```

### 5.2 Tại sao CQRS (MediatR)?

```
✅ Command (write) tách khỏi Query (read) → optimize riêng
✅ Pipeline Behaviors xử lý cross-cutting (validation, logging, tx) tự động
✅ Vertical Slice: 1 feature = 1 folder = 4 files (Command, Handler, Validator, Response)
✅ Domain Events → decouple side effects (gamification, notification)
```

### 5.3 Result Pattern (không dùng Exception cho business logic)

```
✅ Business error → Result.Failure(error) → ToActionResult() → HTTP 4xx
✅ Infrastructure error → Exception → ExceptionHandlingMiddleware → HTTP 500
✅ Luồng rõ ràng, không try/catch rải rác trong controllers
```
