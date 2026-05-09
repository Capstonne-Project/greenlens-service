# Folder Structure — GreenLens Clean Architecture

> **Source:** OVERVIEW.md §3 — Solution Structure (v1.1)

## Full Solution Tree

```
greenlens-service/
│
├── src/
│   ├── Greenlens.Domain/                    # 🔴 Core — NO framework dependencies
│   │   ├── Common/
│   │   │   ├── BaseEntity.cs               # Id (Guid), DomainEvents list
│   │   │   ├── AuditableEntity.cs          # CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
│   │   │   ├── ValueObject.cs              # Equality by value
│   │   │   ├── Result.cs                   # Result<T> pattern
│   │   │   └── Error.cs                    # Error record (Code, Message, ErrorType)
│   │   ├── Entities/
│   │   │   ├── User.cs                     # Identity + soft delete
│   │   │   ├── Report.cs                   # State machine via methods
│   │   │   ├── ReportMedia.cs
│   │   │   ├── Comment.cs                  # Soft delete
│   │   │   ├── Badge.cs
│   │   │   ├── CleanupTask.cs
│   │   │   └── AuditLog.cs
│   │   ├── Enums/
│   │   │   ├── ReportStatus.cs             # Submitted|Verified|InProgress|Resolved|Closed|Rejected|Duplicate
│   │   │   ├── PollutionType.cs            # Trash|Wastewater|Chemical|Other
│   │   │   ├── Severity.cs                 # Low|Medium|High|Critical
│   │   │   └── UserRole.cs                 # Citizen|Officer|CleanupTeam|Admin
│   │   ├── ValueObjects/
│   │   │   ├── GeoLocation.cs              # record struct (Lat, Lng) SRID 4326
│   │   │   ├── Email.cs
│   │   │   ├── PhoneNumber.cs
│   │   │   └── Money.cs                    # record struct
│   │   ├── Events/
│   │   │   ├── ReportSubmittedEvent.cs
│   │   │   ├── ReportVerifiedEvent.cs
│   │   │   ├── StatusChangedEvent.cs
│   │   │   └── PointsAwardedEvent.cs
│   │   ├── Exceptions/
│   │   │   ├── DomainException.cs
│   │   │   └── BusinessRuleViolationException.cs
│   │   └── Specifications/                 # Spec pattern for complex queries
│   │
│   ├── Greenlens.Application/               # 🟡 Use cases — depends on Domain only
│   │   ├── Common/
│   │   │   ├── Behaviors/
│   │   │   │   ├── ValidationBehavior.cs   # FluentValidation pipeline
│   │   │   │   ├── LoggingBehavior.cs
│   │   │   │   ├── TransactionBehavior.cs  # Wraps commands in transaction
│   │   │   │   └── CachingBehavior.cs
│   │   │   ├── Interfaces/
│   │   │   │   ├── ICurrentUser.cs         # Wraps IHttpContextAccessor
│   │   │   │   ├── IDateTimeProvider.cs
│   │   │   │   ├── IFileStorage.cs         # R2/S3 adapter interface
│   │   │   │   ├── ICacheService.cs        # Redis adapter interface
│   │   │   │   ├── ITurnstileVerifier.cs   # Cloudflare Turnstile verify
│   │   │   │   ├── IAuditLogger.cs
│   │   │   │   └── Persistence/            # Strict repo pattern (§4.12)
│   │   │   │       ├── IGenericRepository.cs  # Base: Query, GetByIdAsync, Add, Remove
│   │   │   │       ├── IUnitOfWork.cs         # SaveChangesAsync, BeginTransactionAsync
│   │   │   │       ├── IDbTransaction.cs
│   │   │   │       ├── IReportRepository.cs   # : IGenericRepository<Report> + business methods
│   │   │   │       ├── IUserRepository.cs
│   │   │   │       ├── ICategoryRepository.cs # : IGenericRepository<Category> (body rỗng)
│   │   │   │       └── ...                    # 1 entity = 1 interface
│   │   │   └── Mappings/
│   │   │       └── MapsterConfig.cs        # Global Mapster configuration
│   │   ├── Features/                       # VERTICAL SLICES
│   │   │   ├── Auth/
│   │   │   │   ├── Register/
│   │   │   │   │   ├── RegisterCommand.cs
│   │   │   │   │   ├── RegisterCommandHandler.cs
│   │   │   │   │   ├── RegisterCommandValidator.cs
│   │   │   │   │   └── RegisterResponse.cs
│   │   │   │   ├── Login/
│   │   │   │   ├── RefreshToken/
│   │   │   │   └── ResetPassword/
│   │   │   ├── Reports/
│   │   │   │   ├── SubmitReport/
│   │   │   │   ├── VerifyReport/
│   │   │   │   ├── AssignReport/
│   │   │   │   ├── ResolveReport/
│   │   │   │   ├── CloseReport/
│   │   │   │   └── FlagDuplicate/
│   │   │   ├── Map/
│   │   │   │   ├── GetNearby/
│   │   │   │   ├── GetHotspots/
│   │   │   │   └── GetHeatmap/
│   │   │   ├── Officer/
│   │   │   ├── Cleanup/
│   │   │   ├── Notifications/
│   │   │   ├── Comments/
│   │   │   ├── Gamification/
│   │   │   ├── Admin/
│   │   │   └── Analytics/
│   │   └── BusinessRules/
│   │       └── BrConstants.cs              # BR-*-NNN string constants
│   │
│   ├── Greenlens.Infrastructure/            # 🟢 Adapters — framework-specific
│   │   ├── Persistence/
│   │   │   ├── ApplicationDbContext.cs
│   │   │   ├── Configurations/             # IEntityTypeConfiguration<>
│   │   │   │   ├── ReportConfiguration.cs
│   │   │   │   ├── UserConfiguration.cs
│   │   │   │   └── ...
│   │   │   ├── Migrations/
│   │   │   ├── Repositories/               # Strict: every entity has a repo
│   │   │   │   ├── GenericRepository.cs    # internal abstract — base impl
│   │   │   │   ├── ReportRepository.cs     # internal sealed : GenericRepository<Report>
│   │   │   │   ├── UserRepository.cs
│   │   │   │   ├── CategoryRepository.cs   # body rỗng, kế thừa base
│   │   │   │   └── ...
│   │   │   └── Interceptors/
│   │   │       ├── AuditingSaveChangesInterceptor.cs
│   │   │       └── SoftDeleteInterceptor.cs
│   │   ├── Identity/
│   │   │   ├── JwtService.cs
│   │   │   ├── CurrentUser.cs              # Implements ICurrentUser
│   │   │   └── IdentityUserExtensions.cs
│   │   ├── Storage/
│   │   │   ├── R2FileStorage.cs         # Implements IFileStorage (S3-compatible → R2)
│   │   │   └── ImageProcessor.cs        # EXIF strip, re-encode via ImageSharp
│   │   ├── AI/
│   │   │   └── AiClassificationService.cs
│   │   ├── Geo/
│   │   │   └── PostGisQueryHelper.cs
│   │   ├── Caching/
│   │   │   └── RedisCacheService.cs      # Implements ICacheService
│   │   ├── Notifications/
│   │   │   ├── EmailSender.cs
│   │   │   └── PushNotifier.cs           # FCM
│   │   ├── Security/                     # Cloudflare + auth services
│   │   │   ├── TurnstileVerifier.cs      # Implements ITurnstileVerifier
│   │   │   ├── IpReputationCheck.cs      # Cloudflare IP range validation
│   │   │   ├── BcryptPasswordHasher.cs   # bcrypt ≥12, replaces Identity PBKDF2
│   │   │   └── SecretsRotator.cs         # Key rotation helper
│   │   ├── BackgroundJobs/
│   │   │   ├── AutoCloseResolvedReportJob.cs
│   │   │   ├── SlaBreachVerificationJob.cs
│   │   │   ├── AiRetryJob.cs
│   │   │   └── ...
│   │   └── DependencyInjection.cs        # All infra registrations
│   │
│   ├── Greenlens.Api/                       # 🔵 Composition root — HTTP entry
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs
│   │   │   ├── ReportsController.cs
│   │   │   ├── MapController.cs
│   │   │   └── ...
│   │   ├── Middlewares/
│   │   │   ├── ExceptionHandlingMiddleware.cs
│   │   │   ├── RequestLoggingMiddleware.cs
│   │   │   └── RateLimitMiddleware.cs
│   │   ├── Filters/
│   │   │   ├── AuthorizationFilter.cs
│   │   │   └── ValidationFilter.cs
│   │   ├── Extensions/
│   │   │   └── ResultExtensions.cs         # .ToHttp() mapping
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json
│   │   └── Program.cs
│   │
│   └── Greenlens.Shared/                    # Optional shared kernel
│       ├── ErrorCodes.cs
│       └── ApiResponse.cs                  # Response envelope DTO
│
├── tests/
│   ├── Greenlens.Domain.UnitTests/
│   ├── Greenlens.Application.UnitTests/
│   ├── Greenlens.Application.IntegrationTests/   # Testcontainers
│   └── Greenlens.Api.FunctionalTests/            # WebApplicationFactory
│
├── OVERVIEW.md
├── CLAUDE.md                                # → symlink / copy of OVERVIEW.md
├── 00_API_CONVENTIONS.md
├── .editorconfig
├── .gitignore
├── Directory.Build.props                    # Shared MSBuild properties
└── GreenLens.sln
```

## Dependency Rule (Hard Constraint)

```
Api ──► Application ──► Domain
 │           │
 └──► Infrastructure ──► Application (interfaces) ──► Domain
```

| Layer | References | MUST NOT Reference |
|-------|-----------|-------------------|
| **Domain** | Nothing | `Microsoft.*`, `EntityFrameworkCore`, any other project |
| **Application** | Domain | Infrastructure, Api, `IHttpContextAccessor`, `DbContext` |
| **Infrastructure** | Application, Domain | Api |
| **Api** | Application, Infrastructure (DI only) | — |

## Where Things Go — Decision Table

| I need to create... | Put it in... | File pattern |
|---------------------|-------------|-------------|
| Entity with behavior | `Domain/Entities/` | `Report.cs` (class, sealed) |
| Value object (small) | `Domain/ValueObjects/` | `GeoLocation.cs` (record struct) |
| Domain event | `Domain/Events/` | `ReportVerifiedEvent.cs` (record) |
| Domain exception | `Domain/Exceptions/` | `DomainException.cs` |
| Command/Query | `Application/Features/<Module>/<UseCase>/` | `SubmitReportCommand.cs` (record) |
| Handler | `Application/Features/<Module>/<UseCase>/` | `SubmitReportCommandHandler.cs` (sealed class) |
| Validator | `Application/Features/<Module>/<UseCase>/` | `SubmitReportCommandValidator.cs` |
| Application interface | `Application/Common/Interfaces/` | `IFileStorage.cs` |
| Repo interface | `Application/Common/Interfaces/Persistence/` | `IReportRepository.cs : IGenericRepository<Report>` |
| Pipeline behavior | `Application/Common/Behaviors/` | `ValidationBehavior.cs` |
| DB configuration | `Infrastructure/Persistence/Configurations/` | `ReportConfiguration.cs` |
| Repo implementation | `Infrastructure/Persistence/Repositories/` | `ReportRepository.cs : GenericRepository<Report>` |
| UnitOfWork | `Infrastructure/Persistence/` | `UnitOfWork.cs` |
| External adapter | `Infrastructure/<Service>/` | `R2FileStorage.cs` |
| Security adapter | `Infrastructure/Security/` | `TurnstileVerifier.cs`, `BcryptPasswordHasher.cs` |
| Background job | `Infrastructure/BackgroundJobs/` | `AutoCloseResolvedReportJob.cs` |
| API controller | `Api/Controllers/` | `ReportsController.cs` (sealed class) |
| Middleware | `Api/Middlewares/` | `ExceptionHandlingMiddleware.cs` |

## Vertical Slice Structure

Each use case is a **self-contained folder**:

```
Features/Reports/SubmitReport/
├── SubmitReportCommand.cs           # record : IRequest<Result<Guid>>
├── SubmitReportCommandHandler.cs    # sealed class, BR IDs in XML doc
├── SubmitReportCommandValidator.cs  # FluentValidation rules
└── SubmitReportResponse.cs          # Optional custom DTO shape
```

> **Rule:** Change 1 feature = touch 1 folder. Never create a monolithic "Service" class.
