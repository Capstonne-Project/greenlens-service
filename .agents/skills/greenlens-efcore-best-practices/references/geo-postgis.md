# EF Core 9 — Geo / PostGIS / NetTopologySuite

PostGIS is the project's spatial database. EF Core talks to it via the `Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite` package, which surfaces PostGIS functions as LINQ-friendly methods on `NetTopologySuite.Geometries.*`.

This reference covers the patterns the BR doc demands:
- BR-REP-003 — bounds check (Vietnam lat/lng)
- BR-REP-030 — duplicate detection (≤ 50m, same category, ≤ 24h)
- BR-MAP-002 — nearby reports (5/10/20/50 km)
- BR-MAP-004 — round GPS to ~10m for public display
- BR-MAP-010 — hotspot definition (≥ 10 same-category reports / 500m / 30 days)
- BR-CLN-002 — check-in within 200m

## Setup

In `Infrastructure/DependencyInjection.cs`:

```csharp
services.AddDbContext<ApplicationDbContext>(opt => opt
    .UseNpgsql(cfg.GetConnectionString("Postgres"), o => o
        .UseNetTopologySuite()                            // <— enables Point/Polygon/etc.
        .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
    .UseSnakeCaseNamingConvention());
```

In the migration that introduces the first geo column, also create the extension:

```csharp
migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS postgis;");
```

## Coordinate system: SRID 4326

Use `SRID 4326` (WGS84 — what GPS reports). Configure on the column:

```csharp
b.Property(r => r.Location)
    .HasColumnType("geography (Point, 4326)")
    .IsRequired();
```

`geography` (vs `geometry`):
- **`geography`** — distance and intersection are computed on the WGS84 spheroid (real meters). Use this for Earth-scale features. Slightly slower than `geometry` but no math errors.
- **`geometry`** — planar math, faster, but distances are in degrees. Wrong answers for cross-country queries.

EcoReport uses `geography` for `Report.Location`, `User.LastKnownLocation`, `District.Boundary` (use `geography (Polygon, 4326)`).

## Creating a Point

```csharp
using NetTopologySuite.Geometries;

// Helper — put in EcoReport.Domain.ValueObjects or Infrastructure.Geo
public static class GeometryFactoryEx
{
    private static readonly GeometryFactory Factory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    public static Point CreatePoint(double latitude, double longitude)
        => Factory.CreatePoint(new Coordinate(longitude, latitude));   // X=lng, Y=lat
}
```

⚠️ **Order matters:** `Coordinate(X, Y)` is `(longitude, latitude)`. Get this wrong and Vietnam ends up in the Indian Ocean. Wrap in a helper so the call site is unambiguous.

## Distance (BR-MAP-002, BR-CLN-002, BR-REP-030)

```csharp
var origin = GeometryFactoryEx.CreatePoint(latitude, longitude);

// All reports within 5 km
var nearby = await db.Reports
    .AsNoTracking()
    .Where(r => r.Location.Distance(origin) <= 5_000)        // meters (geography)
    .ProjectToType<ReportListItemDto>()
    .ToListAsync(ct);
```

- `Distance` on `geography` returns meters.
- EF translates this to `ST_Distance(location, origin) <= 5000`.
- For radius queries, prefer `ST_DWithin` — it's index-friendly:

```csharp
.Where(r => r.Location.IsWithinDistance(origin, 5_000))
```

`IsWithinDistance` translates to `ST_DWithin`, which uses the GIST index. **Always** prefer this over `Distance(...) <=` for spatial filtering.

## Duplicate detection (BR-REP-030)

Two reports duplicate if: same category AND ≤ 50m AND submitted within 24h.

```csharp
var since = clock.UtcNow.AddHours(-24);
var duplicates = await db.Reports
    .AsNoTracking()
    .Where(r => r.Category == newReport.Category
             && r.CreatedAt >= since
             && r.Id != newReport.Id
             && r.Location.IsWithinDistance(newReport.Location, 50))
    .ToListAsync(ct);
```

## GIST index — required for fast spatial queries

EF doesn't generate this. Add it explicitly in the migration that creates the column:

```csharp
migrationBuilder.Sql("""
    CREATE INDEX IF NOT EXISTS ix_reports_location_gist
    ON reports USING GIST (location);
""");
```

Without GIST, every spatial query is a full table scan. With GIST and `ST_DWithin`, the planner narrows to candidates first.

## Polygon containment (district lookup, BR-OFF-001)

If you need "which district does this point belong to" — used to auto-assign reports to officers:

```csharp
var districtCode = await db.Districts
    .AsNoTracking()
    .Where(d => d.Boundary.Contains(report.Location))     // ST_Contains
    .Select(d => d.Code)
    .FirstOrDefaultAsync(ct);
```

Index districts' `Boundary` with GIST too. With ~700 districts in Vietnam, this is fast even without index — but add it anyway.

## Bounding box for map viewport

```csharp
var envelope = new Envelope(minLng, maxLng, minLat, maxLat);
var box = GeometryFactoryEx.Factory.ToGeometry(envelope);   // Polygon

var inView = await db.Reports
    .AsNoTracking()
    .Where(r => box.Contains(r.Location))                  // ST_Contains
    .Take(1000)
    .ToListAsync(ct);
```

## Hotspots (BR-MAP-010)

Definition: ≥ 10 reports, same category, within 500m, within 30 days.

This is heavy — don't compute on every map request. Two strategies:

### A) Materialized view, refreshed by a Hangfire job

```sql
CREATE MATERIALIZED VIEW vw_hotspots AS
SELECT
    category,
    ST_ClusterKMeans(location::geometry, 50) OVER (PARTITION BY category) AS cluster_id,
    ST_Centroid(ST_Collect(location::geometry)) AS centroid,
    COUNT(*) AS report_count,
    MAX(severity) AS max_severity
FROM reports
WHERE created_at > now() - interval '30 days'
  AND status NOT IN ('Rejected', 'Duplicate')
GROUP BY category, cluster_id
HAVING COUNT(*) >= 10;
```

Refresh hourly:

```csharp
await db.Database.ExecuteSqlRawAsync(
    "REFRESH MATERIALIZED VIEW CONCURRENTLY vw_hotspots", ct);
```

(Concurrent refresh requires a unique index on the view.)

### B) On-the-fly with caching

For smaller datasets — compute the cluster on demand, cache in Redis for 10 minutes (BR-MAP-012). Use `ST_ClusterDBSCAN` for density-based clustering.

## Privacy: round to ~10m for public display (BR-MAP-004)

```csharp
public sealed record PublicReportDto(double Latitude, double Longitude, ...)
{
    public static PublicReportDto From(Report r) => new(
        Math.Round(r.Location.Y, 4),    // ~11m precision
        Math.Round(r.Location.X, 4),
        ...);
}
```

Do this in projection, never store rounded coordinates as the source of truth. Officers and Cleanup Teams need the precise location for assignment and check-in (BR-CLN-002).

## Vietnam bounds check (BR-REP-003)

Validate at the application boundary; the DB column also gets a check constraint:

```csharp
// In ReportConfiguration
b.ToTable("reports", t => t.HasCheckConstraint(
    "ck_reports_location_vietnam",
    "ST_Y(location::geometry) BETWEEN 8.0 AND 24.0 AND ST_X(location::geometry) BETWEEN 102.0 AND 110.0"));
```

Belt and suspenders — validator catches bad input fast, the DB constraint catches anything that bypasses validation (legacy imports, direct SQL, bugs).

## Common pitfalls

- **Confused X/Y.** `Coordinate(X, Y)` is `(lng, lat)`. The factory helper above prevents 90% of these.
- **Forgetting SRID.** A `Point` with SRID 0 mixed with SRID 4326 throws at the database. Always use the factory.
- **Distance on `geometry` instead of `geography`.** Returns degrees, not meters. Use `geography`, or cast: `r.Location::geography`.
- **No GIST index.** Spatial queries quietly do full scans. EXPLAIN your queries.
- **Storing as JSON.** Don't. Use the proper PostGIS column type.
