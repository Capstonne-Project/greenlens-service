# Data Access Patterns — Entity Framework Core 9

> **Source:** OVERVIEW.md §4.2, §4.6, §4.7, §4.12 (v1.2)

## Core Rules

1. **Never mock DbContext** — use Testcontainers in tests.
2. **One `SaveChanges()` per request** — via `TransactionBehavior`.
3. **Snake_case in DB, PascalCase in C#** — via `EFCore.NamingConventions`.
4. **All entities inherit `AuditableEntity`** — auto-set by interceptor.
5. **Soft delete** for User, Report, Comment — global query filter.

## Repository Pattern — Strict (§4.12)

Application layer **NEVER** imports `IApplicationDbContext` or `DbContext`. All data access goes through `IXxxRepository` + `IUnitOfWork`.

```csharp
// Application/Common/Interfaces/Persistence/IGenericRepository.cs
public interface IGenericRepository<T> where T : BaseEntity
{
    IQueryable<T> Query();                        // tracking (for write)
    IQueryable<T> QueryAsNoTracking();            // no-tracking (for read + projection)
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct);
    void Add(T entity);
    void AddRange(IEnumerable<T> entities);
    void Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct);
}

// Specific repo — body empty (CRUD only)
public interface ICategoryRepository : IGenericRepository<PollutionCategory>;

// Specific repo — with business methods
public interface IReportRepository : IGenericRepository<Report>
{
    Task<Report?> GetForVerificationAsync(Guid id, CancellationToken ct);
    Task<List<Report>> FindPotentialDuplicatesAsync(Point location, PollutionType type, CancellationToken ct);
}

// UnitOfWork — commit only
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);
    Task<IDbTransaction> BeginTransactionAsync(CancellationToken ct);
}
```

## Entity Configuration

```csharp
// Infrastructure/Persistence/Configurations/ReportConfiguration.cs
public sealed class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.HasKey(r => r.Id);

        // ── Value Objects ────────────────────────────
        builder.OwnsOne(r => r.Location, loc =>
        {
            loc.Property(l => l.Lat).HasColumnName("latitude");
            loc.Property(l => l.Lng).HasColumnName("longitude");
        });

        // ── Geo column (PostGIS) ─────────────────────
        builder.Property(r => r.GeoPoint)
            .HasColumnType("geography(Point, 4326)");

        // ── Enums as string ──────────────────────────
        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(r => r.PollutionType)
            .HasConversion<string>()
            .HasMaxLength(20);

        // ── Indexes ──────────────────────────────────
        builder.HasIndex(r => new { r.Status, r.CreatedAt });
        builder.HasIndex(r => r.GeoPoint)
            .HasMethod("gist");  // PostGIS GIST index

        // ── Soft delete filter ───────────────────────
        builder.HasQueryFilter(r => r.DeletedAt == null);

        // ── Relationships ────────────────────────────
        builder.HasMany(r => r.Media)
            .WithOne(m => m.Report)
            .HasForeignKey(m => m.ReportId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

## Query Patterns

### ✅ Read Queries — Always AsNoTracking + Projection

```csharp
// Query handler — read-only via repo
public sealed class GetReportQueryHandler(
    IReportRepository reports) : ...
{
    public async Task<Result<ReportDto>> Handle(
        GetReportQuery request, CancellationToken ct)
    {
        var report = await reports.QueryAsNoTracking()
            .Where(r => r.Id == request.Id)
            .ProjectToType<ReportDto>()  // Mapster projection — SQL-level
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return report is null
            ? Errors.Report.NotFound(request.Id)
            : Result.Success(report);
    }
}
```

### ✅ Paginated Queries

```csharp
public sealed class ListReportsQueryHandler(
    IReportRepository reports) : ...
{
    public async Task<Result<PaginatedList<ReportDto>>> Handle(
        ListReportsQuery request, CancellationToken ct)
    {
        var query = reports.QueryAsNoTracking()
            .Where(r => request.Status == null || r.Status == request.Status)
            .OrderByDescending(r => r.CreatedAt);

        var totalItems = await query.CountAsync(ct).ConfigureAwait(false);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectToType<ReportDto>()
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PaginatedList<ReportDto>(
            items, totalItems, request.Page, request.PageSize);
    }
}
```

### ✅ Geo Queries (PostGIS)

```csharp
// BR-MAP-004: Find nearby reports (via specific repo method or inline LINQ)
var nearbyReports = await reports.QueryAsNoTracking()
    .Where(r => r.GeoPoint.IsWithinDistance(
        new Point(lng, lat) { SRID = 4326 },
        radiusMeters))
    .OrderBy(r => r.GeoPoint.Distance(
        new Point(lng, lat) { SRID = 4326 }))
    .Take(50)
    .ProjectToType<ReportDto>()
    .ToListAsync(ct)
    .ConfigureAwait(false);

// BR-REP-030: Duplicate detection — in IReportRepository
public async Task<List<Report>> FindPotentialDuplicatesAsync(
    Point location, PollutionType type, CancellationToken ct)
{
    return await Query()
        .Where(r =>
            r.GeoPoint.IsWithinDistance(location, 50) &&
            r.PollutionType == type &&
            r.CreatedAt >= DateTime.UtcNow.AddHours(-24))
        .ToListAsync(ct);
}
```

### ✅ Command — Load Entity, Mutate, Save

```csharp
public sealed class VerifyReportCommandHandler(
    IReportRepository reports,
    IUserRepository users,
    IAuditLogRepository auditLogs,
    IUnitOfWork uow,
    ICurrentUser currentUser) : ...
{
    public async Task<Result> Handle(VerifyReportCommand request, CancellationToken ct)
    {
        var report = await reports.GetByIdAsync(request.ReportId, ct);
        if (report is null)
            return Errors.Report.NotFound(request.ReportId);

        report.Verify(currentUser.UserId);  // Domain method — state machine

        var citizen = await users.GetByIdAsync(report.ReporterId, ct);
        citizen!.AwardPoints(10);

        auditLogs.Add(new AuditLog("VerifyReport", report.Id, currentUser.UserId));

        await uow.SaveChangesAsync(ct);  // Single commit — all or nothing
        return Result.Success();
    }
}
```

## Auditable Entity Interceptor

```csharp
public sealed class AuditableEntityInterceptor(ICurrentUser currentUser, IDateTime dateTime)
    : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        var context = eventData.Context!;
        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = dateTime.UtcNow;
                    entry.Entity.CreatedBy = currentUser.UserId;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = dateTime.UtcNow;
                    entry.Entity.UpdatedBy = currentUser.UserId;
                    break;
            }
        }
        return base.SavingChangesAsync(eventData, result, ct);
    }
}
```

## Migration Naming

Format: `yyyyMMddHHmm_VerbNoun`

```bash
dotnet ef migrations add 202605091200_AddReportSlaColumns
dotnet ef migrations add 202605101430_CreateAuditLogTable
dotnet ef migrations add 202605111000_AddGistIndexOnReportLocation
```

## Anti-Patterns

```csharp
// ❌ N+1 Query
var reports = await reportRepo.QueryAsNoTracking().ToListAsync(ct);
foreach (var r in reports)
{
    r.Media = await mediaRepo.QueryAsNoTracking()
        .Where(m => m.ReportId == r.Id).ToListAsync(ct); // N+1!
}

// ✅ Use Include or Projection
var reports = await reportRepo.Query()
    .Include(r => r.Media)
    .ToListAsync(ct);

// ❌ Multiple SaveChanges in one request
reports.Add(report);
await uow.SaveChangesAsync(ct);     // 1st call
auditLogs.Add(auditLog);
await uow.SaveChangesAsync(ct);     // 2nd call — WRONG

// ✅ Single SaveChanges via UnitOfWork
reports.Add(report);
auditLogs.Add(auditLog);
await uow.SaveChangesAsync(ct);     // One call, one transaction

// ❌ Handler imports DbContext / IApplicationDbContext
var report = await db.Reports.FindAsync(id, ct);  // VIOLATION — Application sees EF

// ✅ Handler uses repo
var report = await reports.GetByIdAsync(id, ct);

// ❌ Tracking on read queries
var reports = await reportRepo.Query().ToListAsync(ct);  // Tracks everything!

// ✅ QueryAsNoTracking for reads
var reports = await reportRepo.QueryAsNoTracking().ToListAsync(ct);
```
