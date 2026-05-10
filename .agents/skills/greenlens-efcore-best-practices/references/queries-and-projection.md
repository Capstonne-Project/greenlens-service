# EF Core 9 — Queries and Projection

## The cardinal rule

**Reads project to a DTO. Writes load the entity.** That single decision avoids 80% of EF Core performance traps.

```csharp
// READ — projection, no tracking, only the columns the UI needs
var page = await db.Reports
    .AsNoTracking()
    .Where(r => r.Status == ReportStatus.Submitted)
    .OrderByDescending(r => r.CreatedAt)
    .ProjectToType<ReportListItemDto>()         // Mapster, runs in SQL
    .ToPagedListAsync(page, size, ct);

// WRITE — load the aggregate, mutate via domain method, save
var report = await db.Reports
    .Include(r => r.Media)
    .FirstOrDefaultAsync(r => r.Id == id, ct);
if (report is null) return Result<Guid>.Failure(Errors.Reports.NotFound);

var transition = report.Verify(currentUser.UserId, clock.UtcNow);     // BR-REP-021
if (transition.IsFailure) return Result<Guid>.Failure(transition.Error!);

await db.SaveChangesAsync(ct);
```

## When `Include` is right

- Loading an aggregate to mutate it (the entity owns the children, you'll modify them or read them as part of an invariant).
- Test setup / seed data.

## When `Include` is wrong

- Returning a list to the API. Use projection — Mapster's `ProjectToType<TDto>()` translates the mapping into SQL, fetching only the needed columns.
- Multi-collection includes. EF Core 9 will warn about cartesian explosion. Switch to `AsSplitQuery()` only if you genuinely need the parent + multiple collections in one logical operation; otherwise project.

```csharp
// BAD — cartesian explosion
var reports = await db.Reports
    .Include(r => r.Media)
    .Include(r => r.Comments)
    .Include(r => r.StatusHistory)
    .ToListAsync(ct);

// BETTER — project a flat DTO with counts
var reports = await db.Reports
    .Select(r => new ReportListItemDto(
        r.Id,
        r.Title,
        r.Status.ToString(),
        r.Media.Count,
        r.Comments.Count,
        r.CreatedAt))
    .ToListAsync(ct);
```

## Mapster projection

Configure mapping in `Application/Common/Mappings/`:

```csharp
public sealed class ReportMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Report, ReportListItemDto>()
            .Map(d => d.Status,    s => s.Status.ToString())
            .Map(d => d.MediaCount, s => s.Media.Count);

        config.NewConfig<Report, ReportDetailDto>()
            .Map(d => d.Latitude,  s => s.Location.Y)
            .Map(d => d.Longitude, s => s.Location.X);
    }
}
```

Then `.ProjectToType<TDto>()` on any `IQueryable<Report>` translates the mapping into SQL.

## Pagination

Cursor-based for large feeds (map, leaderboard); offset-based is OK for admin tables.

```csharp
// Offset (simple, fine up to ~10k rows scanned)
var skip = (page - 1) * size;
var items = await query.Skip(skip).Take(size).ToListAsync(ct);
var total = await query.CountAsync(ct);
return new PagedList<T>(items, total, page, size);

// Cursor (preferred for infinite-scroll feeds and APIs hit hard)
var items = await query
    .Where(r => r.CreatedAt < cursor)        // cursor = last seen timestamp
    .OrderByDescending(r => r.CreatedAt)
    .Take(size + 1)                          // +1 to know if there's another page
    .ToListAsync(ct);
```

Always cap `pageSize`. The validator should clamp at 100 (CLAUDE.md §4.1).

## `Find` vs `FirstOrDefault`

- `FindAsync(id)` — uses the change tracker cache, returns immediately if already loaded. Good for write-side load-by-id when the entity may already be tracked.
- `FirstOrDefaultAsync(...)` — always hits the DB. Use for read-side and when filtering by anything other than the PK.

## `Any` vs `Count`

- Existence check → `AnyAsync(predicate, ct)`. Stops at the first match.
- Total count → `CountAsync(predicate, ct)`. Scans the whole filter.
- Never `CountAsync() > 0` — that's a full count for nothing.

## Filtering on enum-as-string

If you mapped enums via `.HasConversion<string>()`, LINQ comparisons translate cleanly:

```csharp
.Where(r => r.Status == ReportStatus.Verified)   // becomes WHERE status = 'Verified'
```

Don't materialize first and `.ToString()` later — that's client evaluation.

## Aggregations

EF Core 9 handles `Sum`, `Avg`, `Max`, `Min`, `GroupBy` server-side for simple shapes. For windowed / complex aggregations, prefer:

- A keyless query type + raw SQL via `FromSqlInterpolated` if it's read-only and shape is stable.
- A materialized view in the DB (refreshed by a Hangfire job) for hotspots/leaderboards.

```csharp
modelBuilder.Entity<HotspotView>().HasNoKey().ToView("vw_hotspots");
// then
var rows = await db.Set<HotspotView>().AsNoTracking().ToListAsync(ct);
```

## Bulk operations (EF 9)

EF Core 9 ships `ExecuteUpdateAsync` and `ExecuteDeleteAsync` — server-side, no entity loading.

```csharp
// Auto-close resolved reports after 7 days (BR-REP-016, BR-REP-025)
var cutoff = DateTimeOffset.UtcNow.AddDays(-7);
var closed = await db.Reports
    .Where(r => r.Status == ReportStatus.Resolved && r.ResolvedAt < cutoff)
    .ExecuteUpdateAsync(s => s
        .SetProperty(r => r.Status, ReportStatus.Closed)
        .SetProperty(r => r.ClosedAt, DateTimeOffset.UtcNow), ct);
```

**Caveat:** these bypass the change tracker, so domain events do NOT fire and audit interceptors do NOT run. Use only for system-driven jobs that handle their own audit logging explicitly. Never use for user-driven mutations.

## Detecting and fixing N+1

Symptom: a list endpoint with one initial query and N follow-up queries (one per row).

**Diagnose:** enable Serilog at Information for `Microsoft.EntityFrameworkCore.Database.Command`. Slow tests = slow prod; add a test that asserts `query count == 1` for list endpoints using EF interceptors.

**Fix:** projection. If the missing data is a count, project a count. If it's a child collection's first item, project that item's columns.

## Compiled queries

For hot paths called thousands of times per minute (auth check, current-user load), consider `EF.CompileAsyncQuery`:

```csharp
private static readonly Func<ApplicationDbContext, Guid, CancellationToken, Task<User?>>
    GetUserById = EF.CompileAsyncQuery((ApplicationDbContext db, Guid id, CancellationToken ct) =>
        db.Users.AsNoTracking().FirstOrDefault(u => u.Id == id));
```

Don't compile every query — measure first. The win is avoiding LINQ-to-SQL translation overhead, ~5-15% on a hot path.

## Things to NOT do

- `.ToList().Where(...)` — pulls the whole table to memory first. Always filter, then materialize.
- `.Where(r => SomeCSharpHelper(r))` — won't translate. EF Core 9 throws by default; don't add `AsEnumerable()` to make it "work".
- `Include` followed by `Select` — confusing. Pick one approach.
- Returning `IQueryable<T>` from any method that crosses an architectural boundary. The query has to be materialized inside the handler so the DbContext lifetime is clear.
- Loading an entity then re-saving it without changes just to "refresh" — issue a fresh query with `AsNoTracking()` instead.
