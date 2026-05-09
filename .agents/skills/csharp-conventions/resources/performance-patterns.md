# Performance Patterns — GreenLens

> **Source:** OVERVIEW.md §10 Performance & Scaling, §13.8 Rate Limiting, §14.2 R2+CDN (v1.1)

## Performance Targets (BR-SYS-001)

| Metric | Target |
|--------|--------|
| API p95 latency | < 2 seconds at peak |
| Concurrent users | 5,000 CCU |
| Report scale | 100,000+ |
| Uptime | ≥ 99.5% / month |

## Architecture — Performance Stack

```
Client → Cloudflare Edge (CDN, WAF, Cache) → ASP.NET Core → Redis (L1/L2 Cache) → PostgreSQL
                                                ↓
                                          Hangfire (Background)
                                                ↓
                                    Cloudflare R2 (Media, zero egress)
```

## Response Compression

```csharp
// ✅ Enable Brotli compression (BR-SYS-001)
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
    options.Level = CompressionLevel.Fastest);  // Low CPU, good ratio

// Middleware order: before static files + endpoints
app.UseResponseCompression();
```

## Caching Strategy — 3 Levels

### Level 1: Cloudflare Edge Cache (§14.2)

```
Request → Cloudflare POP (300+ locations) → Cache HIT → Direct response
                                          → Cache MISS → Origin
```

- **Media (R2)**: `Cache-Control: public, max-age=31536000, immutable` — 1 year edge TTL
- **API GET (map/hotspots)**: `Cache-Control: public, max-age=600` — 10 min edge cache
- **Auth endpoints**: `Cache-Control: no-store` — NEVER cache

### Level 2: Redis (Application Cache)

```csharp
// Multi-level cache: L1 Memory (5s) → L2 Redis (10min) → DB
// See caching-patterns.md for full implementation

// Key patterns updated for v1.1
// map:nearby:{lat}:{lng}:{radius} → 10 min
// map:hotspots:{bbox}:{filters} → 10 min
// gamification:leaderboard:{period} → 1 hour
// ratelimit:{userId}:{action} → sliding window (Redis-backed)
// blacklist:{jti} → remaining JWT lifetime
```

### Level 3: EF Core Query Optimization

```csharp
// ✅ AsNoTracking for reads — no change tracking overhead
var reports = await db.Reports
    .AsNoTracking()
    .Where(r => r.Status == status)
    .ProjectToType<ReportDto>()       // SQL-level projection, not hydrate entity
    .ToListAsync(ct)
    .ConfigureAwait(false);

// ✅ Pagination mandatory — max 100, default 20
// Cursor-based preferred over offset for large datasets
var items = await query
    .Where(r => r.CreatedAt < cursor)  // Cursor-based
    .Take(pageSize)
    .ToListAsync(ct);

// ❌ DON'T: Load all then paginate in memory
// ❌ DON'T: Hydrate entity then map to DTO (double work)
```

## Database Performance

```csharp
// ✅ Required indexes (§4.6)
builder.HasIndex(r => new { r.Status, r.CreatedAt });  // Report queries
builder.HasIndex(r => r.GeoPoint).HasMethod("gist");   // PostGIS spatial
builder.HasIndex(u => u.Email).IsUnique();              // User lookup
builder.HasIndex(u => u.PhoneNumber).IsUnique();        // User lookup

// ✅ N+1 prevention — use Include or projection
var reports = await db.Reports
    .Include(r => r.Media)              // Eager load in one query
    .AsNoTracking()
    .ToListAsync(ct);

// ✅ Geo query optimization (PostGIS GIST index)
var nearby = await db.Reports
    .Where(r => r.GeoPoint.IsWithinDistance(
        new Point(lng, lat) { SRID = 4326 }, radiusMeters))
    .OrderBy(r => r.GeoPoint.Distance(
        new Point(lng, lat) { SRID = 4326 }))
    .Take(50)
    .ProjectToType<ReportDto>()
    .ToListAsync(ct);
```

## Image Storage — R2 Zero-Egress (§14.2)

```csharp
// ✅ Cloudflare R2 — zero egress cost (vs AWS S3 $0.09/GB)
// Upload: presigned URL → client direct upload to R2
// Serve: media.ecoreport.example (custom domain) → Cloudflare CDN cache

// Cache headers on PUT:
var metadata = new Dictionary<string, string>
{
    ["Cache-Control"] = "public, max-age=31536000, immutable"
};
// Images are immutable (key = content hash), so 1-year cache is safe

// ❌ DON'T: Serve via *.r2.dev (rate-limited, not for production)
// ✅ DO: Custom domain + Cloudflare Cache for all public media
```

## Background Jobs (Heavy Work)

```csharp
// ✅ Offload heavy work to Hangfire, DON'T block HTTP request
// AI classification → queue → AIRetryJob
// Notifications → outbox → NotificationDispatcherJob
// CSV export → queue → ExportJob
// SLA breach check → scheduled → SlaBreachJob

// ❌ DON'T: Synchronous AI call in handler (5s timeout risk)
// ❌ DON'T: Send push notification inside HTTP request
```

## Rate Limiting — 2 Layers (§13.8, §14.3)

```
Layer 1: Cloudflare WAF (edge)
  - /api/auth/login     → 10 req/IP/10s → block 5 min
  - /api/auth/register  → 5 req/IP/1m   → block 10 min
  - /api/* catch-all    → 100 req/IP/1m → block 1 min

Layer 2: ASP.NET RateLimiter (app)
  - anon-ip  → 60 rpm/IP (BR-SYS-004)
  - user     → 300 rpm/userId (BR-SYS-004)
  - submit   → 5/h + 20/24h per citizen (BR-REP-010, Redis-backed)
```

```csharp
// ⚠️ ASP.NET RateLimiter default is in-memory (single instance)
// Production scaling: back with Redis for shared state
// BR-REP-010 (5/h, 20/24h) MUST use Redis backing
```

## Request Size Limits

```csharp
// ✅ Protect against large payload attacks
builder.WebHost.ConfigureKestrel(o =>
{
    o.Limits.MaxRequestBodySize = 10 * 1024 * 1024;  // 10 MB max
    o.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
});

// Per-endpoint override for file upload
[RequestSizeLimit(50 * 1024 * 1024)]  // 50 MB for media
public async Task<IActionResult> UploadMediaAsync(...)
```

## Connection Pooling

```csharp
// ✅ PostgreSQL connection pooling
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, o =>
    {
        o.UseNetTopologySuite();
        o.CommandTimeout(30);          // 30s query timeout
        o.MaxBatchSize(100);           // Batch operations
    })
    .UseSnakeCaseNamingConvention());

// ✅ Redis connection multiplexing (built-in with StackExchange.Redis)
```

## Performance Checklist

- [ ] Response compression (Brotli) enabled
- [ ] Pagination on ALL list endpoints (max pageSize=100)
- [ ] `AsNoTracking()` on all read queries
- [ ] DTO projection (Mapster) at SQL level, not in-memory
- [ ] No N+1 queries — use `.Include()` or projection
- [ ] Required DB indexes present (§4.6)
- [ ] Cache configured for read-heavy endpoints (Redis, 1-10 min TTL)
- [ ] Cloudflare Cache for media (R2 custom domain, 1-year TTL)
- [ ] Background jobs for AI, notifications, exports
- [ ] Rate limiting: Cloudflare edge + ASP.NET app layer
- [ ] Request body size limits configured
- [ ] API p95 < 2s verified under expected load

## Anti-Patterns

```csharp
// ❌ Synchronous I/O in request pipeline
var result = httpClient.GetAsync(url).Result;  // DEADLOCK

// ❌ Loading all records then filtering
var all = await db.Reports.ToListAsync(ct);
return all.Where(r => r.Status == status).Take(20);  // Loads EVERYTHING

// ❌ Missing CancellationToken — can't abort cancelled requests
await db.Reports.ToListAsync();  // No ct = can't cancel

// ❌ Using *.r2.dev for media serving — rate-limited
return $"https://pub-xxx.r2.dev/{key}";  // Will hit rate limit!

// ❌ Single-instance rate limiter in production cluster
// In-memory rate limiter doesn't share state across pods
```
