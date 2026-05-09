# Async/Await Patterns — GreenLens

> **Source:** CLAUDE.md §6 — C# 13 / .NET 9 coding standards

## Golden Rules

1. **`async`/`await` throughout** — NO `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()`.
2. **Every I/O method** accepts `CancellationToken` as last parameter.
3. **`ConfigureAwait(false)`** in library projects (Application, Infrastructure).
4. **Skip `ConfigureAwait`** in Api project (needs `HttpContext`).

## Patterns

### ✅ Correct — Pass CancellationToken Everywhere

```csharp
// Handler
public sealed class GetNearbyReportsQueryHandler(
    IApplicationDbContext db,
    ICacheService cache)
    : IRequestHandler<GetNearbyReportsQuery, Result<List<ReportDto>>>
{
    public async Task<Result<List<ReportDto>>> Handle(
        GetNearbyReportsQuery request,
        CancellationToken cancellationToken)
    {
        var cached = await cache.GetAsync<List<ReportDto>>(
            $"nearby:{request.Lat}:{request.Lng}",
            cancellationToken).ConfigureAwait(false);

        if (cached is not null)
            return Result.Success(cached);

        var reports = await db.Reports
            .AsNoTracking()
            .Where(r => r.Location.IsWithinDistance(
                new Point(request.Lng, request.Lat) { SRID = 4326 },
                request.RadiusMeters))
            .ProjectToType<ReportDto>()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        await cache.SetAsync(
            $"nearby:{request.Lat}:{request.Lng}",
            reports,
            TimeSpan.FromMinutes(10),
            cancellationToken).ConfigureAwait(false);

        return Result.Success(reports);
    }
}
```

### ✅ Correct — IAsyncEnumerable for Streaming

```csharp
// BR-OFF-022: Export CSV streaming
public async IAsyncEnumerable<ReportExportDto> StreamReportsAsync(
    ExportFilter filter,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    await foreach (var report in db.Reports
        .AsNoTracking()
        .Where(r => r.CreatedAt >= filter.From && r.CreatedAt <= filter.To)
        .AsAsyncEnumerable()
        .WithCancellation(cancellationToken)
        .ConfigureAwait(false))
    {
        yield return report.Adapt<ReportExportDto>();
    }
}
```

### ✅ Correct — Parallel Independent I/O

```csharp
// When multiple independent calls can run concurrently
public async Task<DashboardDto> GetDashboardAsync(CancellationToken ct)
{
    var reportCountTask = db.Reports.CountAsync(ct).ConfigureAwait(false);
    var pendingTask = db.Reports.CountAsync(r => r.Status == ReportStatus.Submitted, ct).ConfigureAwait(false);
    var hotspotTask = geoService.GetHotspotsAsync(ct).ConfigureAwait(false);

    await Task.WhenAll(
        reportCountTask.AsTask(),
        pendingTask.AsTask(),
        hotspotTask.AsTask());

    return new DashboardDto(
        await reportCountTask,
        await pendingTask,
        await hotspotTask);
}
```

## Anti-Patterns

### ❌ Sync over Async — DEADLOCK RISK

```csharp
// ❌ NEVER DO THIS — CA1849
public Report GetReport(Guid id)
{
    return db.Reports.FindAsync(id).Result;        // ❌ Deadlock!
}

public void SaveReport(Report report)
{
    db.SaveChangesAsync().Wait();                   // ❌ Deadlock!
}

var result = handler.Handle(cmd, ct)
    .GetAwaiter().GetResult();                      // ❌ Deadlock!
```

### ❌ Fire and Forget — LOST EXCEPTIONS

```csharp
// ❌ Fire-and-forget loses exceptions
public async Task SubmitAsync(Report report)
{
    _ = notificationService.SendAsync(report);     // ❌ Exception lost!
    // Use background job or outbox pattern instead
}

// ✅ Correct — Use outbox or Hangfire
public async Task SubmitAsync(Report report, CancellationToken ct)
{
    await db.OutboxMessages.AddAsync(
        new OutboxMessage("ReportSubmitted", report.Id), ct)
        .ConfigureAwait(false);
    await db.SaveChangesAsync(ct).ConfigureAwait(false);
}
```

### ❌ Missing CancellationToken

```csharp
// ❌ No CancellationToken — cannot cancel on client disconnect
public async Task<Result<ReportDto>> Handle(GetReportQuery request)
{
    var report = await db.Reports.FindAsync(request.Id);  // ❌
    return Result.Success(report.Adapt<ReportDto>());
}

// ✅ Always pass CancellationToken
public async Task<Result<ReportDto>> Handle(
    GetReportQuery request,
    CancellationToken cancellationToken)
{
    var report = await db.Reports
        .FindAsync([request.Id], cancellationToken)
        .ConfigureAwait(false);
    return Result.Success(report.Adapt<ReportDto>());
}
```

### ❌ Async Void — UNHANDLED EXCEPTIONS

```csharp
// ❌ async void — only for event handlers
public async void ProcessReport(Report report) { ... }

// ✅ Always return Task
public async Task ProcessReportAsync(Report report, CancellationToken ct) { ... }
```

## ConfigureAwait Decision Table

| Project | Use `ConfigureAwait(false)` | Why |
|---------|---------------------------|-----|
| `Greenlens.Domain` | N/A (no async) | Pure domain logic |
| `Greenlens.Application` | ✅ Yes | Library — no HttpContext needed |
| `Greenlens.Infrastructure` | ✅ Yes | Library — no HttpContext needed |
| `Greenlens.Api` | ❌ No | Needs HttpContext for auth, logging |

## Analyzer Rules to Enable

```editorconfig
# .editorconfig
dotnet_diagnostic.CA1849.severity = error    # Sync-over-async
dotnet_diagnostic.CA2007.severity = warning  # ConfigureAwait
dotnet_diagnostic.CA2016.severity = warning  # Forward CancellationToken
```
