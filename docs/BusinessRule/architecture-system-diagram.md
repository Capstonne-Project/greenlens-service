# GreenLens — System Architecture Diagram

> **Dự án:** SU26SE049 — Crowdsourced Application for Reporting Environmental Pollution  
> **Stack:** .NET 9 · ASP.NET Core · EF Core 9 · PostgreSQL + PostGIS · Redis · Cloudflare R2 · Hangfire · Firebase

---

## 1. System Architecture (3 khối chính)

```mermaid
graph TB
    subgraph FrontEnd["📱 FRONT-END"]
        direction LR
        MA["📱 Mobile App<br/>(React Native)<br/>Citizen · Cleaner · Inspector"]
        WA["🌐 Web Dashboard<br/>(React / Next.js)<br/>LEO · DEO · Admin<br/>Company Manager"]
    end

    subgraph BackEnd["⚙️ BACK-END"]
        direction TB

        subgraph APILayer["API Layer"]
            NG["🔒 Reverse Proxy<br/>(Nginx / ALB)"]
            API["ASP.NET Core 9<br/>Controllers · Middleware<br/>JWT Auth · Rate Limit"]
        end

        subgraph AppLayer["Application Layer"]
            PIPE["MediatR Pipeline<br/>Validation · Transaction<br/>Logging · Audit"]
            HANDLER["Feature Handlers<br/>(CQRS Command / Query)"]
        end

        subgraph DomainLayer["Domain Layer"]
            DOM["Entities · Value Objects<br/>Domain Events<br/>State Machines · Business Rules"]
        end

        subgraph InfraLayer["Infrastructure Layer"]
            EF["EF Core 9<br/>(ORM + PostGIS)"]
            ADAPT["Service Adapters<br/>JWT · Bcrypt · Geo"]
            HF["Hangfire Server<br/>Background Jobs"]
        end

        subgraph DataStores["Data Stores"]
            direction LR
            PG[("🐘 PostgreSQL 18<br/>+ PostGIS")]
            RD[("⚡ Redis<br/>Cache · Rate Limit")]
        end

        NG --> API
        API --> PIPE
        PIPE --> HANDLER
        HANDLER --> DOM
        HANDLER --> EF
        HANDLER --> ADAPT
        EF --> PG
        ADAPT --> RD
        HF --> EF
    end

    subgraph ThirdParty["🌐 BÊN THỨ 3 (Third-party Services)"]
        direction LR
        R2["☁️ Cloudflare R2<br/>Object Storage<br/>(Report Images)"]
        AI["🤖 AI Service<br/>Image Classification<br/>Duplicate Detection<br/>(Python / DINOv2)"]
        FCM["🔔 Firebase FCM<br/>Push Notifications"]
        SMTP["📧 SMTP Service<br/>Email (OTP, Alerts)"]
        MAP["🗺️ Map Tiles<br/>(Mapbox / Google)"]
    end

    MA -->|"REST API<br/>(HTTPS)"| NG
    WA -->|"REST API<br/>(HTTPS)"| NG

    MA -.->|"Presigned URL<br/>Upload"| R2
    WA -.->|"Presigned URL<br/>Upload"| R2

    MA -.->|"Map Tiles"| MAP
    WA -.->|"Map Tiles"| MAP

    ADAPT -->|"HTTP Client"| AI
    ADAPT -->|"FCM SDK"| FCM
    ADAPT -->|"SMTP Client"| SMTP
    EF -->|"S3 API"| R2

    classDef feStyle fill:#4A90D9,stroke:#2C5F8A,color:#fff
    classDef proxyStyle fill:#F5A623,stroke:#D48B0A,color:#fff
    classDef apiStyle fill:#7B68EE,stroke:#5B4BCF,color:#fff
    classDef appStyle fill:#9B59B6,stroke:#7D3C98,color:#fff
    classDef domStyle fill:#27AE60,stroke:#1E8449,color:#fff
    classDef infraStyle fill:#E67E22,stroke:#CA6F1E,color:#fff
    classDef dataStyle fill:#2ECC71,stroke:#229954,color:#fff
    classDef tpStyle fill:#E74C3C,stroke:#C0392B,color:#fff

    class MA,WA feStyle
    class NG proxyStyle
    class API apiStyle
    class PIPE,HANDLER appStyle
    class DOM domStyle
    class EF,ADAPT,HF infraStyle
    class PG,RD dataStyle
    class R2,AI,FCM,SMTP,MAP tpStyle
```

---

## 2. Chi tiết từng khối

### 2.1 Front-end

| Component | Technology | Actor sử dụng | Chức năng chính |
|-----------|-----------|---------------|----------------|
| **Mobile App** | React Native | Citizen, Cleaner, Inspector | Submit report (ảnh + GPS), check-in, update progress, view map, gamification |
| **Web Dashboard** | React / Next.js | LEO, DEO, Admin, Company Manager | Verify/reject reports, assign teams, inspection, penalty, KPI dashboard, audit logs |

**Giao tiếp với Back-end:** REST API qua HTTPS (JSON)  
**Giao tiếp với Third-party:** Presigned URL upload trực tiếp lên R2, Map tiles từ Mapbox/Google

---

### 2.2 Back-end

| Layer | Project | Trách nhiệm | Technology |
|-------|---------|-------------|------------|
| **Reverse Proxy** | Nginx / AWS ALB | Load balancing, SSL termination, static files | Nginx |
| **API** | `Greenlens.Api` | Routing, JWT Auth, Rate Limit, Error Handling | ASP.NET Core 9, Kestrel |
| **Application** | `Greenlens.Application` | CQRS Handlers, Validation, Pipeline Behaviors | MediatR, FluentValidation |
| **Domain** | `Greenlens.Domain` | Entities, State Machines, Business Rules, Events | Pure C# (no framework) |
| **Infrastructure** | `Greenlens.Infrastructure` | ORM, Repository, External Adapters, Background Jobs | EF Core 9, Hangfire |
| **Database** | PostgreSQL 18 + PostGIS | Primary data store, spatial queries (ST_DWithin) | PostgreSQL, NetTopologySuite |
| **Cache** | Redis | Map cache (10'), rate limit counters, session | StackExchange.Redis |

---

### 2.3 Bên thứ 3 (Third-party)

| Service | Provider | Mục đích | Giao tiếp |
|---------|----------|---------|-----------|
| **Object Storage** | Cloudflare R2 (S3-compatible) | Lưu ảnh/video báo cáo (presigned URL upload) | S3 API |
| **AI Service** | Self-hosted (Python FastAPI) | Phân loại ảnh ô nhiễm, duplicate detection (DINOv2) | HTTP REST |
| **Push Notification** | Firebase FCM | Thông báo đẩy cho Mobile App | FCM SDK |
| **Email Service** | SMTP (SendGrid / SES) | Gửi OTP, thông báo SLA, digest | SMTP Client |
| **Map Tiles** | Mapbox / Google Maps | Bản đồ nền cho Mobile + Web | SDK / REST |

---

## 3. Request Flow (End-to-End)

```mermaid
sequenceDiagram
    participant FE as 📱 Front-end<br/>(Mobile / Web)
    participant PROXY as 🔒 Reverse Proxy
    participant API as ⚙️ API Layer<br/>(ASP.NET Core)
    participant PIPE as 📋 MediatR Pipeline
    participant HANDLER as 🎯 Handler
    participant DOMAIN as 🏛️ Domain
    participant DB as 🐘 PostgreSQL
    participant TP as 🌐 Third-party

    FE->>PROXY: HTTPS Request (Bearer JWT)
    PROXY->>API: Forward request

    API->>API: Rate Limit check (Redis)
    API->>API: JWT Authentication
    API->>API: Route → Controller

    API->>PIPE: ISender.Send(command)

    PIPE->>PIPE: ① LoggingBehavior
    PIPE->>PIPE: ② ValidationBehavior (FluentValidation)
    PIPE->>PIPE: ③ TransactionBehavior (BEGIN)
    PIPE->>PIPE: ④ AuditLogBehavior

    PIPE->>HANDLER: Handle(command, ct)

    HANDLER->>DOMAIN: Entity.Create() / Entity.Verify()
    DOMAIN-->>HANDLER: Result + Domain Events

    HANDLER->>DB: EF Core SaveChanges()
    DB-->>HANDLER: OK

    opt Side Effects (async)
        HANDLER->>TP: AI classify / FCM push / Email
    end

    PIPE->>PIPE: ③ TransactionBehavior (COMMIT)

    HANDLER-->>API: Result~T~
    API-->>PROXY: ToActionResult() → HTTP Response
    PROXY-->>FE: JSON Response (ProblemDetails if error)
```

---

## 4. Data Flow giữa 3 khối

```mermaid
flowchart LR
    subgraph FE["📱 FRONT-END"]
        M["Mobile App"]
        W["Web Dashboard"]
    end

    subgraph BE["⚙️ BACK-END"]
        API2["API Server"]
        DB2[("PostgreSQL")]
        REDIS2[("Redis")]
    end

    subgraph TP2["🌐 BÊN THỨ 3"]
        R22["Cloudflare R2"]
        AI2["AI Service"]
        FCM2["Firebase FCM"]
        SMTP2["SMTP"]
    end

    M -->|"① Submit report<br/>(JSON + metadata)"| API2
    W -->|"② Verify / Assign<br/>(JSON)"| API2
    API2 -->|"③ Save data"| DB2
    API2 -->|"④ Cache map"| REDIS2

    M -.->|"⑤ Upload ảnh<br/>(Presigned URL)"| R22
    API2 -->|"⑥ Generate<br/>presigned URL"| R22

    API2 -->|"⑦ Classify image"| AI2
    AI2 -.->|"⑧ Result"| API2

    API2 -->|"⑨ Push notification"| FCM2
    FCM2 -.->|"⑩ Push"| M

    API2 -->|"⑪ Send email"| SMTP2

    style FE fill:#e8f4fd,stroke:#2980b9
    style BE fill:#eafaf1,stroke:#27ae60
    style TP2 fill:#fdedec,stroke:#e74c3c
```

---

## 5. Background Jobs

```mermaid
flowchart LR
    subgraph Hangfire["⏰ Hangfire Server (Back-end)"]
        AC["AutoCloseResolvedReportJob<br/>🕐 Hourly"]
        SV["SlaBreachVerificationJob<br/>🕐 Every 15min"]
        SR["SlaBreachResolutionJob<br/>🕐 Every 30min"]
        DD["DuplicateDetectionJob<br/>🔄 On-demand (Tier 2)"]
        LS["LeaderboardSnapshotJob<br/>🕐 Daily"]
        DC["DraftCleanupJob<br/>🕐 Daily"]
    end

    DB3[("🐘 PostgreSQL")]
    AI3["🤖 AI Service<br/>(Third-party)"]
    FCM3["🔔 FCM<br/>(Third-party)"]

    AC -->|"Resolved ≥ 7d → Closed"| DB3
    SV -->|"Submitted > 24h → Escalate"| DB3
    SR -->|"InProgress > SLA → Flag"| DB3
    DD -->|"Compare images"| AI3
    DD -->|"Update duplicate flags"| DB3
    LS -->|"Snapshot points"| DB3
    SV -->|"Notify LEO/DEO"| FCM3

    classDef job fill:#34495E,stroke:#2C3E50,color:#fff
    classDef db fill:#2ECC71,stroke:#229954,color:#fff
    classDef tp fill:#E74C3C,stroke:#C0392B,color:#fff

    class AC,SV,SR,DD,LS,DC job
    class DB3 db
    class AI3,FCM3 tp
```

---

## 6. Authentication & Authorization Flow

```mermaid
sequenceDiagram
    participant FE as 📱 Front-end
    participant BE as ⚙️ Back-end
    participant DB as 🐘 PostgreSQL
    participant RD as ⚡ Redis

    FE->>BE: POST /v1/auth/login (email, password)
    BE->>DB: Find user by email
    DB-->>BE: User record

    alt Account locked (BR-AUTH-011)
        BE-->>FE: 423 Locked (30 min)
    else Password incorrect
        BE->>DB: Increment FailedLoginCount
        BE-->>FE: 401 Unauthorized
    else Banned (BR-AUTH-015)
        BE-->>FE: 403 Banned
    else Success
        BE->>BE: Generate JWT (24h) + Refresh (30d)
        BE->>DB: Store hashed refresh token
        BE-->>FE: 200 { accessToken, refreshToken }
    end

    Note over FE,BE: Subsequent API calls
    FE->>BE: GET /v1/reports (Bearer token)
    BE->>RD: Check rate limit (60rpm anon / 300rpm auth)
    RD-->>BE: OK / 429

    alt Token expired
        FE->>BE: POST /v1/auth/refresh
        BE->>DB: Validate + rotate refresh token
        BE-->>FE: 200 { newAccessToken, newRefreshToken }
    end
```

---

## 7. Module Summary

| Module | Controllers | Feature Slices | Key Entities |
|--------|:-----------:|:--------------:|:-------------|
| **Auth** | 1 | Login, Register, Refresh, ChangePassword, Lockout, Ban | User, PasswordHistory |
| **Reports** | 1 | Submit, Verify, Reject, Assign, Resolve, Close, Escalate | Report, ReportMedia, ReportAssignment |
| **Organization** | 3 | Department CRUD, Office CRUD, Invitation Flow | Department, LocalOffice, StaffInvitation |
| **Teams** | 1 | Team CRUD, Member CRUD, Check-in, Progress | EnvironmentalTeam, TeamMember |
| **Companies** | 1 | Company CRUD, Staff, Dispatch, Service Area | EnvServiceCompany, ContractPeriod |
| **Inspection** | 1 | Create, Assign Team, Issue Penalty, Payment | InspectionReport, ViolatingEntity |
| **Gamification** | 1 | Points, Badges, Leaderboard | UserPoints, Badge, UserBadge |
| **Notifications** | 1 | List, Read, Preferences, FCM Token | Notification, NotificationPreference |
| **Map** | 1 | Nearby, Heatmap, Hotspot | (PostGIS queries on Report) |
| **Catalog** | 1 | Categories, Waste Tags | PollutionCategory, WasteTag |
| **Admin** | 1 | User management, Audit, Config | User, AuditLog |
| **Media** | 1 | Upload, Presigned URL, AI Analyze | ReportMedia |

---

## 8. Deployment Architecture (Target)

```mermaid
graph TB
    subgraph Internet["🌐 Internet"]
        U["Users<br/>(Mobile + Web)"]
    end

    subgraph Cloud["☁️ Cloud Infrastructure"]
        ALB["🔒 Load Balancer<br/>(ALB / Nginx)"]

        subgraph Compute["Compute"]
            A1["API Instance 1"]
            A2["API Instance 2"]
            HF2["Hangfire Worker"]
        end

        subgraph Data["Data Layer"]
            RDS[("PostgreSQL<br/>+ PostGIS")]
            EC[("Redis")]
        end

        subgraph Monitoring["📊 Observability"]
            CW["Serilog → Seq/ELK"]
            OT["OpenTelemetry<br/>→ Jaeger"]
        end
    end

    subgraph ThirdParty2["🌐 Third-party"]
        R2T["Cloudflare R2"]
        AIT["AI Service"]
        FCMT["Firebase FCM"]
        SMTPT["SMTP"]
    end

    U --> ALB
    ALB --> A1
    ALB --> A2
    A1 --> RDS
    A2 --> RDS
    A1 --> EC
    A2 --> EC
    HF2 --> RDS
    A1 --> R2T
    A1 --> AIT
    A1 --> FCMT
    A1 --> SMTPT
    A1 --> CW
    A1 --> OT

    classDef compute fill:#FF9900,stroke:#CC7A00,color:#fff
    classDef data fill:#3B48CC,stroke:#2C3A9E,color:#fff
    classDef tp fill:#E74C3C,stroke:#C0392B,color:#fff

    class ALB,A1,A2,HF2 compute
    class RDS,EC data
    class R2T,AIT,FCMT,SMTPT tp
```
