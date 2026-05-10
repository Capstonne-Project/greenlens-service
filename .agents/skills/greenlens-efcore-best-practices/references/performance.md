# EF Core 9 — Performance

EcoReport's NFR target is API p95 < 2s at 5,000 concurrent users (BR-SYS-001). Most slow endpoints are EF problems, not network problems.

## Diagnose first

Don't guess at a fix. Diagnose:

### Enable SQL logging in dev

```csharp
optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information)
              .EnableSensitiveDataLogging()      // dev only — prints param values
              .EnableDetailedErrors();
```

In production, log via Serilog with the `Microsoft.EntityFrameworkCore.Database.Command` source set to Information for the slow path only.

### EXPLAIN (ANALYZE, BUFFERS) the bad query

Copy the SQL EF generates, run in `psql`:

```sql
EXPLAIN (ANALYZE, BUFFERS) SELECT ...;
```

What to look for:
- **Seq Scan on a big table** — missing index. Add it.
- **Nested Loop with high row count** — maybe a missed `JOIN` index. PostgreSQL doesn't auto-index FKs.
- **Sort with big disk spill** — pre-sort via index or reduce the result set.
- **Filter: removing 99.9% of rows** — your `WHERE` is post-scan; rewrite or index.
- **Heap Fetches > 0 in Index Only Scan** — VACUUM the table.

### MiniProfiler / OpenTelemetry traces

Wire `OpenTelemetry.Instrumentation.EntityFrameworkCore` to surface query duration in traces. p95 latency on a single endpoint is the right unit, not aggregate DB time.

## Top 10 fixes, in order of frequency

### 1. Replace `Include` chains with projection
See `queries-and-projection.md`. By far the most common win. Drops cartesian explosions and trims columns to what the UI actually shows.

### 2. Add the missing index
- Foreign keys (PostgreSQL doesn't auto-index them).
- Filter columns paired with sort columns: `(Status, CreatedAt)`.
- GIN/GIST for full-text and geography.

```sql
CREATE INDEX CONCURRENTLY ix_reports_status_created_at
ON reports (status, created_at DESC);
```

`CONCURRENTLY` so production isn't blocked.

### 3. Stop client evaluation
EF Core 9 throws on accidental client eval, but composed predicates (`.Where(r => Helpers.IsRecent(r))`) still bite. Inline the logic so EF sees it.

### 4. Add `AsNoTracking()` to reads
Tracking adds ~20% per row for nothing if you're not mutating. Default reads should be no-tracking.

### 5. Batch with `ExecuteUpdateAsync`/`ExecuteDeleteAsync`
EF 9 server-side bulk ops are 10-100x faster than load-and-save loops. Caveat: bypass interceptors and domain events; see `queries-and-projection.md`.

### 6. Use `Any` instead of `Count > 0`
Stops at the first match.

### 7. Materialized views for analytics
Hotspots, leaderboards, KPI dashboards. Refresh on a schedule, not per request.

### 8. Connection pool sizing
Default is 100. For 5,000 CCU you DON'T need 5,000 connections — connections are expensive on PostgreSQL. PgBouncer in transaction mode + a Npgsql pool of ~50-100 per app instance is the production setup. Tune `Maximum Pool Size` and `Minimum Pool Size` in the connection string.

### 9. Compiled queries on the hottest path
~10% on the auth path and current-user load. Don't compile every query — measure.

### 10. Snapshot caching for hot read-only data
Pollution categories, badge definitions, district list — load once, cache in `IMemoryCache` or Redis with a TTL of hours. Don't re-query EF for each request.

## Anti-patterns to grep out

| Anti-pattern | Replace with |
|---|---|
| `db.X.ToList().Where(...)` | `db.X.Where(...).ToList()` |
| `.CountAsync() > 0` | `.AnyAsync()` |
| `Include` followed by `Select` projecting away most of it | `Select` directly with what you need |
| `await Task.WhenAll(db.X..., db.Y...)` on the same DbContext | sequential awaits — DbContext is not thread-safe |
| `.Result` / `.Wait()` | `await` |
| Looping `db.Y.Where(y => y.XId == x.Id).ToListAsync()` per X | single query joining X and Y, or projection |
| Returning `IQueryable<T>` from a service method | materialize before returning |

## DbContext lifetime

- Scoped per request — the default `AddDbContext` registration. Don't change this.
- One DbContext per logical operation. Don't share across `Task.WhenAll`. If you must parallelize DB work, use `IDbContextFactory<ApplicationDbContext>` and create a context per parallel branch.

## Connection management

- Don't `await using var conn = db.Database.GetDbConnection().OpenAsync()` unless you really need raw ADO.NET. EF manages connections.
- Don't keep transactions open across HTTP requests. Anything beyond a single request is a job, not a query.

## Read replicas

Once write throughput on the primary becomes the bottleneck, route read queries to a replica. Approach:

1. Two DbContexts: `ApplicationDbContext` (primary) and `ReadOnlyDbContext` (replica).
2. Read-only handlers depend on `IReadOnlyApplicationDbContext`.
3. Health-check the replica; fall back to primary if lag > 5s.

Don't add this prematurely — Postgres 16 on decent hardware handles 5,000 CCU without a replica for this workload.

## Caching layers

| Layer | Lifetime | Tool | Use for |
|---|---|---|---|
| Per-request | scope of HTTP req | DbContext change tracker | already there, free |
| Per-process | minutes | `IMemoryCache` | static reference data (categories, badges) |
| Distributed | minutes-to-hours | Redis | session, rate limits, map data (BR-MAP-012), leaderboards |
| HTTP | client-controlled | `Cache-Control` headers | public open data, hotspot tiles |

Cache invalidation: prefer time-based (TTL) over event-based unless the data is small and changes are rare.

## Load testing

Add k6 or NBomber tests for the top 10 endpoints. Track p95 over time so regressions surface in CI, not in production.
