# Data Access Patterns — Entity Framework Core 9

> **Source:** CLAUDE.md §4.2, §4.6, §4.7

## Core Rules

1. **Never mock DbContext** — use Testcontainers in tests.
2. **One `SaveChanges()` per request** — via `TransactionBehavior`.
3. **Snake_case in DB, PascalCase in C#** — via `EFCore.NamingConventions`.
4. **All entities inherit `AuditableEntity`** — auto-set by interceptor.
5. **Soft delete** for User, Report, Comment — global query filter.

## DbContext Interface (Application Layer)

```csharp
// Application/Common/Interfaces/IApplicationDbContext.cs
public interface IApplicationDbContext
{
    DbSet<Report> Reports { get; }
    DbSet<User> Users { get; }
    DbSet<Comment> Comments { get; }
    DbSet<CleanupTask> CleanupTasks { get; }
    DbSet<Badge> Badges { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<OutboxMessage> OutboxMessages { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
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
// Query handler — read-only, no tracking overhead
public async Task<Result<ReportDto>> Handle(
    GetReportQuery request, CancellationToken ct)
{
    var report = await db.Reports
        .AsNoTracking()
        .Where(r => r.Id == request.Id)
        .ProjectToType<ReportDto>()  // Mapster projection — SQL-level
        .FirstOrDefaultAsync(ct)
        .ConfigureAwait(false);

    return report is null
        ? Errors.Report.NotFound(request.Id)
        : Result.Success(report);
}
```

### ✅ Paginated Queries

```csharp
public async Task<Result<PaginatedList<ReportDto>>> Handle(
    ListReportsQuery request, CancellationToken ct)
{
    var query = db.Reports
        .AsNoTracking()
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
```

### ✅ Geo Queries (PostGIS)

```csharp
// BR-MAP-004: Find nearby reports
var nearbyReports = await db.Reports
    .AsNoTracking()
    .Where(r => r.GeoPoint.IsWithinDistance(
        new Point(lng, lat) { SRID = 4326 },
        radiusMeters))
    .OrderBy(r => r.GeoPoint.Distance(
        new Point(lng, lat) { SRID = 4326 }))
    .Take(50)
    .ProjectToType<ReportDto>()
    .ToListAsync(ct)
    .ConfigureAwait(false);

// BR-REP-030: Duplicate detection (50m + same category + 24h)
var duplicates = await db.Reports
    .Where(r =>
        r.GeoPoint.IsWithinDistance(report.GeoPoint, 50) &&
        r.PollutionType == report.PollutionType &&
        r.CreatedAt >= DateTime.UtcNow.AddHours(-24))
    .AnyAsync(ct)
    .ConfigureAwait(false);
```

### ✅ Command — Load Entity, Mutate, Save

```csharp
public async Task<Result> Handle(VerifyReportCommand request, CancellationToken ct)
{
    var report = await db.Reports
        .FirstOrDefaultAsync(r => r.Id == request.ReportId, ct)
        .ConfigureAwait(false);

    if (report is null)
        return Errors.Report.NotFound(request.ReportId);

    report.Verify(request.OfficerId);  // Domain method — state machine

    await db.SaveChangesAsync(ct).ConfigureAwait(false);
    return Result.Success();
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
var reports = await db.Reports.ToListAsync(ct);
foreach (var r in reports)
{
    r.Media = await db.ReportMedia.Where(m => m.ReportId == r.Id).ToListAsync(ct); // N+1!
}

// ✅ Use Include or Projection
var reports = await db.Reports
    .Include(r => r.Media)
    .ToListAsync(ct);

// ❌ Multiple SaveChanges in one request
db.Reports.Add(report);
await db.SaveChangesAsync(ct);     // 1st call
db.AuditLogs.Add(auditLog);
await db.SaveChangesAsync(ct);     // 2nd call — WRONG

// ✅ Single SaveChanges via TransactionBehavior
db.Reports.Add(report);
db.AuditLogs.Add(auditLog);
await db.SaveChangesAsync(ct);     // One call, one transaction

// ❌ Tracking on read queries
var reports = await db.Reports.ToListAsync(ct);  // Tracks everything!

// ✅ AsNoTracking for reads
var reports = await db.Reports.AsNoTracking().ToListAsync(ct);
```
