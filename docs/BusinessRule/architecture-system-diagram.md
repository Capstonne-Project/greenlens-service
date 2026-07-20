# GreenLens — System Architecture Diagram

> **Dự án:** SU26SE049 — Crowdsourced Application for Reporting Environmental Pollution
> **Stack:** .NET 9 · ASP.NET Core · EF Core 9 · PostgreSQL + PostGIS · Redis · AWS S3 · Hangfire · Firebase

---

## 1. High-Level Architecture

```mermaid
graph TB
    subgraph Clients["🖥️ Clients"]
        direction LR
        MA["📱 Mobile App<br/>(React Native)"]
        WA["🌐 Web Dashboard<br/>(React/Next.js)"]
    end

    subgraph Gateway["🔒 API Gateway / Reverse Proxy"]
        NG["Nginx / AWS ALB"]
    end

    subgraph Backend["⚙️ GreenLens Backend (.NET 9)"]
        direction TB
        API["Greenlens.Api<br/>ASP.NET Core 9<br/>(Controllers + Middleware)"]
        APP["Greenlens.Application<br/>CQRS (MediatR)<br/>Feature Slices"]
        DOM["Greenlens.Domain<br/>Entities + Events<br/>State Machine"]
        INF["Greenlens.Infrastructure<br/>EF Core + Adapters"]
    end

    subgraph DataStores["🗄️ Data Stores"]
        PG["🐘 PostgreSQL 18<br/>+ PostGIS"]
        RD["⚡ Redis<br/>Cache + Rate Limit"]
        S3["📦 AWS S3<br/>Media Storage"]
    end

    subgraph ExternalServices["🌐 External Services"]
        AI["🤖 AI Service<br/>Image Classification"]
        FCM["🔔 Firebase FCM<br/>Push Notifications"]
        SMTP["📧 SMTP<br/>Email Service"]
        MAP["🗺️ Map Tile<br/>(Mapbox/Google)"]
    end

    subgraph BackgroundWorker["⏰ Background Worker"]
        HF["Hangfire Server<br/>Recurring Jobs"]
    end

    MA --> NG
    WA --> NG
    NG --> API
    API --> APP
    APP --> DOM
    APP --> INF
    INF --> PG
    INF --> RD
    INF --> S3
    INF --> AI
    INF --> FCM
    INF --> SMTP
    HF --> INF
    MA -.->|"Presigned URL"| S3
    WA -.->|"Presigned URL"| S3

    classDef clientStyle fill:#4A90D9,stroke:#2C5F8A,color:#fff
    classDef gatewayStyle fill:#F5A623,stroke:#D48B0A,color:#fff
    classDef apiStyle fill:#7B68EE,stroke:#5B4BCF,color:#fff
    classDef appStyle fill:#9B59B6,stroke:#7D3C98,color:#fff
    classDef domStyle fill:#27AE60,stroke:#1E8449,color:#fff
    classDef infStyle fill:#E67E22,stroke:#CA6F1E,color:#fff
    classDef dataStyle fill:#2ECC71,stroke:#229954,color:#fff
    classDef extStyle fill:#E74C3C,stroke:#C0392B,color:#fff
    classDef bgStyle fill:#34495E,stroke:#2C3E50,color:#fff

    class MA,WA clientStyle
    class NG gatewayStyle
    class API apiStyle
    class APP appStyle
    class DOM domStyle
    class INF infStyle
    class PG,RD,S3 dataStyle
    class AI,FCM,SMTP,MAP extStyle
    class HF bgStyle
```

---

## 2. Clean Architecture Layers

```mermaid
graph LR
    subgraph API["🟣 Greenlens.Api"]
        C["Controllers<br/>(15 controllers)"]
        MW["Middlewares<br/>Exception · RateLimit · Logging"]
        FI["Filters<br/>Validation · Auth"]
    end

    subgraph Application["🟪 Greenlens.Application"]
        FT["Feature Slices<br/>(11 modules)"]
        BH["Behaviors<br/>Validation · Transaction · Logging"]
        IF["Interfaces<br/>ICurrentUser · IFileStorage<br/>IApplicationDbContext"]
    end

    subgraph Domain["🟢 Greenlens.Domain"]
        EN["Entities<br/>Report · User · Team<br/>StaffInvitation · Badge"]
        VO["Value Objects<br/>GeoLocation · Email"]
        EV["Domain Events<br/>ReportVerified<br/>PointsAwarded"]
        SM["State Machine<br/>Report Lifecycle"]
    end

    subgraph Infrastructure["🟠 Greenlens.Infrastructure"]
        PS["Persistence<br/>EF Core · PostgreSQL<br/>PostGIS · Repositories"]
        ID["Identity<br/>JWT · Refresh Token"]
        ST["Storage<br/>AWS S3 · Presigned URL"]
        AI2["AI Adapter<br/>Image Classification"]
        NF["Notifications<br/>FCM · Email"]
        BJ["Background Jobs<br/>Hangfire · SLA · AutoClose"]
    end

    C --> FT
    FT --> EN
    FT --> IF
    PS -.->|"implements"| IF
    ID -.->|"implements"| IF
    ST -.->|"implements"| IF
    BJ --> FT

    classDef api fill:#7B68EE,stroke:#5B4BCF,color:#fff
    classDef app fill:#9B59B6,stroke:#7D3C98,color:#fff
    classDef dom fill:#27AE60,stroke:#1E8449,color:#fff
    classDef inf fill:#E67E22,stroke:#CA6F1E,color:#fff

    class C,MW,FI api
    class FT,BH,IF app
    class EN,VO,EV,SM dom
    class PS,ID,ST,AI2,NF,BJ inf
```

---

## 3. Report Lifecycle & Actor Interactions

```mermaid
flowchart TB
    subgraph Citizens["👤 Citizen"]
        SUB["Submit Report<br/>(ảnh + GPS)"]
    end

    subgraph System["⚙️ System"]
        RG["Reverse Geocode<br/>(Ward lookup)"]
        RT["Auto-Route<br/>(Ward → LocalOffice)"]
        AI3["AI Classification<br/>(waste type, severity)"]
    end

    subgraph LEO["👮 LEO (Phường)"]
        VF["Verify / Reject"]
        ESC["Escalate to DEO<br/>(tuyến cấp TP)"]
        AT["Assign Team"]
        DC["Dispatch to Company"]
    end

    subgraph DEO["🏛️ DEO (Sở)"]
        DQ["Department Queue"]
        DD["Dispatch to<br/>CITENCO / Company"]
    end

    subgraph Cleanup["🧹 Cleanup Team"]
        CI["Check-in (GPS)"]
        UP["Update Progress"]
        RS["Resolve (ảnh after)"]
    end

    subgraph Citizen2["👤 Citizen (Close)"]
        CL["Confirm Close<br/>(hoặc auto 7d)"]
    end

    SUB --> RG
    RG --> RT
    RT -->|"Office found"| VF
    RT -->|"No office<br/>(chưa onboard)"| DQ
    SUB -.-> AI3
    AI3 -.->|"suggest tags"| VF

    VF -->|"Verified"| AT
    VF -->|"Reject"| DQ
    ESC --> DQ

    AT --> CI
    DC --> DD
    DQ --> DD
    DD --> CI

    CI --> UP
    UP --> RS
    RS --> CL

    classDef citizen fill:#3498DB,stroke:#2980B9,color:#fff
    classDef system fill:#95A5A6,stroke:#7F8C8D,color:#fff
    classDef leo fill:#E67E22,stroke:#D35400,color:#fff
    classDef deo fill:#8E44AD,stroke:#7D3C98,color:#fff
    classDef cleanup fill:#27AE60,stroke:#229954,color:#fff

    class SUB,CL citizen
    class RG,RT,AI3 system
    class VF,ESC,AT,DC leo
    class DQ,DD deo
    class CI,UP,RS cleanup
```

---

## 4. Data Model (Core Entities)

```mermaid
erDiagram
    USER ||--o{ REPORT : "submits"
    USER ||--o{ USER_POINTS : "earns"
    USER ||--o{ USER_BADGE : "receives"
    USER ||--o{ NOTIFICATION : "receives"
    USER ||--o{ PASSWORD_HISTORY : "has"
    USER }o--o| LOCAL_OFFICE : "belongs to"

    REPORT ||--o{ REPORT_MEDIA : "has"
    REPORT ||--o{ REPORT_STATUS_HISTORY : "tracks"
    REPORT ||--o{ REPORT_ASSIGNMENT : "assigned to"
    REPORT ||--o{ COMMENT : "has"
    REPORT ||--o{ INSPECTION_REPORT : "inspected by"
    REPORT }o--o| LOCAL_OFFICE : "routed to"
    REPORT }o--|| DEPARTMENT : "belongs to"
    REPORT }o--o| WASTE_CATEGORY : "categorized as"
    REPORT }o--o{ REPORT_WASTE_TAG : "tagged with"

    DEPARTMENT ||--o{ LOCAL_OFFICE : "oversees"
    DEPARTMENT }o--|| PROVINCE : "serves"

    LOCAL_OFFICE ||--o{ TEAM : "has"
    LOCAL_OFFICE }o--|| WARD : "covers"

    TEAM ||--o{ TEAM_MEMBER : "contains"
    TEAM_MEMBER }o--|| USER : "is"

    STAFF_INVITATION }o--|| USER : "invited by"
    STAFF_INVITATION }o--|| USER : "for user"
    STAFF_INVITATION }o--o| LOCAL_OFFICE : "to office"

    ENV_SERVICE_COMPANY ||--o{ COMPANY_SERVICE_AREA : "covers"
    ENV_SERVICE_COMPANY ||--o{ COMPANY_TEAM : "has"
    COMPANY_SERVICE_AREA }o--|| WARD : "serves"

    PROVINCE ||--o{ WARD : "contains"

    BADGE ||--o{ USER_BADGE : "awarded as"

    USER {
        guid Id PK
        string Email UK
        string FullName
        string Role
        bool IsBanned
        guid LocalOfficeId FK
        guid DepartmentId FK
    }

    REPORT {
        guid Id PK
        string Code UK
        string Description
        int Status
        int Severity
        float Latitude
        float Longitude
        guid ReporterId FK
        guid AssignedOfficeId FK
        guid AssignedDepartmentId FK
        guid WasteCategoryId FK
        bool SlaVerifyBreached
        bool SlaResolveBreached
    }

    DEPARTMENT {
        guid Id PK
        string Name
        string ProvinceCode FK
        bool IsActive
    }

    LOCAL_OFFICE {
        guid Id PK
        string Name
        string WardCode FK
        guid DepartmentId FK
        bool IsOnboarded
    }

    TEAM {
        guid Id PK
        string Name
        int TeamType
        guid LocalOfficeId FK
    }

    STAFF_INVITATION {
        guid Id PK
        guid InvitedUserId FK
        guid InvitedByUserId FK
        string TargetRole
        int Status
        datetime ExpiresAt
    }
```

---

## 5. Background Jobs Architecture

```mermaid
flowchart LR
    subgraph Hangfire["⏰ Hangfire Server"]
        AC["AutoCloseResolvedReportJob<br/>🕐 Hourly"]
        SV["SlaBreachVerificationJob<br/>🕐 Every 15min"]
        SR["SlaBreachResolutionJob<br/>🕐 Every 30min"]
        LS["LeaderboardSnapshotJob<br/>🕐 Daily"]
    end

    subgraph DB["🐘 PostgreSQL"]
        RT2["Reports Table"]
        LB["Leaderboard Table"]
    end

    AC -->|"Resolved ≥ 7d → Closed"| RT2
    SV -->|"Submitted > 24h → Escalate"| RT2
    SR -->|"InProgress > SLA → Flag"| RT2
    LS -->|"Snapshot points"| LB

    classDef job fill:#34495E,stroke:#2C3E50,color:#fff
    classDef db fill:#2ECC71,stroke:#229954,color:#fff

    class AC,SV,SR,LS job
    class RT2,LB db
```

---

## 6. Authentication & Authorization Flow

```mermaid
sequenceDiagram
    participant C as Client
    participant API as API Server
    participant JWT as JWT Service
    participant DB as PostgreSQL
    participant Redis as Redis

    C->>API: POST /v1/auth/login (email, password)
    API->>DB: Find user by email
    DB-->>API: User record

    alt Account locked (BR-AUTH-011)
        API-->>C: 423 Locked (30 min)
    else Password incorrect
        API->>DB: Increment FailedLoginCount
        API-->>C: 401 Unauthorized
    else Banned (BR-AUTH-015)
        API-->>C: 403 Banned
    else Success
        API->>JWT: Generate Access (24h) + Refresh (30d)
        JWT-->>API: Tokens
        API->>DB: Store hashed refresh token
        API-->>C: 200 { accessToken, refreshToken }
    end

    Note over C,API: Subsequent requests
    C->>API: GET /v1/reports (Bearer token)
    API->>Redis: Check rate limit (60rpm anon / 300rpm auth)
    Redis-->>API: OK / 429

    alt Token expired
        C->>API: POST /v1/auth/refresh
        API->>DB: Validate refresh token
        API->>JWT: Rotate tokens
        API-->>C: 200 { newAccessToken, newRefreshToken }
    end
```

---

## 7. Deployment Architecture (Target)

```mermaid
graph TB
    subgraph Internet["🌐 Internet"]
        U["Users<br/>(Mobile + Web)"]
    end

    subgraph AWS["☁️ AWS Cloud"]
        ALB["Application<br/>Load Balancer"]

        subgraph ECS["ECS / EC2"]
            A1["API Instance 1"]
            A2["API Instance 2"]
            HF2["Hangfire Worker"]
        end

        subgraph Data["Data Layer"]
            RDS["RDS PostgreSQL<br/>+ PostGIS"]
            EC["ElastiCache<br/>Redis"]
            S3B["S3 Bucket<br/>Media Files"]
        end

        subgraph Monitoring["📊 Observability"]
            CW["CloudWatch<br/>Logs + Metrics"]
            OT["OpenTelemetry<br/>→ Jaeger"]
        end
    end

    subgraph External["External"]
        FCM2["Firebase FCM"]
        AI4["AI Classification<br/>Service"]
    end

    U --> ALB
    ALB --> A1
    ALB --> A2
    A1 --> RDS
    A2 --> RDS
    A1 --> EC
    A2 --> EC
    A1 --> S3B
    HF2 --> RDS
    A1 --> FCM2
    A1 --> AI4
    A1 --> CW
    A1 --> OT

    classDef aws fill:#FF9900,stroke:#CC7A00,color:#fff
    classDef data fill:#3B48CC,stroke:#2C3A9E,color:#fff
    classDef ext fill:#E74C3C,stroke:#C0392B,color:#fff

    class ALB,A1,A2,HF2 aws
    class RDS,EC,S3B data
    class FCM2,AI4 ext
```

---

## 8. Module Summary

| Module            | Controllers |                        Feature Slices                        | Key Entities                             |
| ----------------- | :---------: | :----------------------------------------------------------: | :--------------------------------------- |
| **Auth**          |      1      |    Login, Register, Refresh, ChangePassword, Lockout, Ban    | User, PasswordHistory                    |
| **Reports**       |      1      |   Submit, Verify, Reject, Assign, Resolve, Close, Escalate   | Report, ReportMedia, ReportAssignment    |
| **Organization**  |      3      | Department CRUD, Office CRUD, Invitation Flow, Release Staff | Department, LocalOffice, StaffInvitation |
| **Teams**         |      1      |          Team CRUD, Member CRUD, Check-in, Progress          | Team, TeamMember                         |
| **Companies**     |      1      |         Company CRUD, Staff, Dispatch, Service Area          | EnvServiceCompany, CompanyTeam           |
| **Inspection**    |      1      |               Create, Update, Get inspections                | InspectionReport                         |
| **Gamification**  |      1      |                 Points, Badges, Leaderboard                  | UserPoints, Badge, UserBadge             |
| **Notifications** |      1      |              List, Read, Preferences, FCM Token              | Notification, NotificationPreference     |
| **Map**           |      1      |                   Nearby, Heatmap, Hotspot                   | (PostGIS queries on Report)              |
| **Catalog**       |      1      |                    Categories, Waste Tags                    | WasteCategory, WasteTag                  |
| **Admin**         |      1      |                User management, Audit, Config                | User, AuditLog                           |
| **Media**         |      1      |                    Upload, Presigned URL                     | ReportMedia                              |
