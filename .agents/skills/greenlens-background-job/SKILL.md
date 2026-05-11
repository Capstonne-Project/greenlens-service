---
name: greenlens-background-job
description: Scaffold a Hangfire background job (recurring or one-off) with proper DI scope, retry policy, and idempotency for the Greenlens .NET 9 backend (project SU26SE049). Use this skill whenever the user mentions "Hangfire", "background job", "scheduled job", "recurring job", "cron", "BackgroundJob.Enqueue", "RecurringJob", or asks to add periodic work — including casual phrasings like "auto-close resolved reports after 7 days", "run cleanup nightly", "retry the AI classification", "send a digest at 8am", or "BR-REP-016 needs a job to do X". Trigger this for ALL the jobs listed in OVERVIEW.md §4.11 (AutoCloseResolvedReportJob, SlaBreachVerificationJob, AiRetryJob, DraftCleanupJob, LeaderboardSnapshotJob, AuditLogRetentionJob, AccountHardDeleteJob, etc.). Produces the job class in Infrastructure/BackgroundJobs/, Hangfire registration, retry attributes, and idempotency guards.
---

# Greenlens Background Jobs (Hangfire)

The Greenlens stack uses **Hangfire** (OVERVIEW.md §2). All scheduled work + delayed work goes through it. Jobs in the project trace back to specific BR rules — they're not free-form cron tasks.

## When to use

Trigger when the user mentions:
- "Hangfire", "background job", "scheduled job", "recurring job", "cron"
- A job from OVERVIEW.md §4.11 table
- "Run X every Y", "schedule X for Z time"
- "Retry the AI service", "send a digest", "auto-close after N days"
- A BR that implies recurring work: BR-REP-016 (auto-close), BR-OFF-002/020 (SLA), BR-AI-006 (retry), BR-AUTH-022 (hard delete day-90), BR-DAT-002 (retention), BR-GAM-005 (leaderboard snapshot)

## Workflow

1. **Confirm with the user:**
   - Job name in PascalCase ending in `Job` (e.g. `AutoCloseResolvedReportJob`).
   - Trigger: recurring (cron expression) or one-off enqueued (delayed/immediate)?
   - Schedule (for recurring): use Hangfire's `Cron.*` helpers or NCRONTAB string.
   - **BR ID(s)** the job implements — required for XML doc.
   - Idempotency strategy: what happens if the job runs twice? (See Step 2)

2. **Pick the template:**
   - `assets/recurring-job.cs.template` — runs on a schedule
   - `assets/one-off-job.cs.template` — fired by a domain event or API call
   - `assets/job-registration.cs.template` — `RecurringJob.AddOrUpdate(...)` lines for `Program.cs`

3. **Generate the job** under `src/Greenlens.Infrastructure/BackgroundJobs/`. Public method = the entry point Hangfire calls.

## Step 2 — Idempotency is mandatory

Hangfire **may run a job twice** (worker crash mid-job, network blip on ack). Every job in Greenlens must be idempotent. Three patterns:

### A) Filter to "still needs the work"

The most common pattern. The query selects only rows that haven't been processed yet:

```csharp
// AutoCloseResolvedReportJob — BR-REP-016, BR-REP-025
var cutoff = clock.UtcNow.AddDays(-7);
await db.Reports
    .Where(r => r.Status == ReportStatus.Resolved && r.ResolvedAt < cutoff)
    .ExecuteUpdateAsync(s => s
        .SetProperty(r => r.Status, ReportStatus.Closed)
        .SetProperty(r => r.ClosedAt, clock.UtcNow), ct);
```

Re-running on the same data is a no-op because the `Status == Resolved` filter no longer matches.

### B) Idempotency key

For one-off jobs that send notifications / call external APIs:

```csharp
public sealed class SendDigestEmailJob(IApplicationDbContext db, IEmailSender email, IDateTimeProvider clock)
{
    public async Task ExecuteAsync(Guid userId, DateOnly digestDate, CancellationToken ct)
    {
        var key = $"digest:{userId}:{digestDate:yyyyMMdd}";

        // SQL-level lock or Redis SETNX
        var inserted = await db.IdempotencyKeys.AddIfMissingAsync(key, clock.UtcNow, ct);
        if (!inserted) return;   // already processed by another worker

        // ... send email
    }
}
```

### C) Compare-and-swap on a status column

```csharp
var rows = await db.Reports
    .Where(r => r.Id == reportId && r.AiStatus == AiStatus.Pending)
    .ExecuteUpdateAsync(s => s.SetProperty(r => r.AiStatus, AiStatus.Processing), ct);

if (rows == 0) return;   // another worker grabbed it
// ... do the AI call
```

The skill's templates already wire pattern A. For B/C, ask the user explicitly which fits.

## Conventions

- File: `src/Greenlens.Infrastructure/BackgroundJobs/<Name>Job.cs`
- Class: `public sealed class <Name>Job` — Hangfire activates via DI, scope per execution.
- Entry method: `Task ExecuteAsync(CancellationToken ct)` for recurring; `Task ExecuteAsync(<args>, CancellationToken ct)` for one-off.
- **DI scope:** Hangfire creates a new scope per job execution (configured in `Program.cs` with `UseColouredConsoleLogProvider` + `UseActivator(new AspNetCoreJobActivator(...))`). This means `IApplicationDbContext`, `IUnitOfWork`, `ICurrentUser` are per-execution — same as a request scope.
- **`ICurrentUser` for jobs returns `null` UserId.** The auditing interceptor (OVERVIEW.md §4.6) handles `null` — `CreatedBy/UpdatedBy` will be null. Use a sentinel `SystemUserId` if you really need attribution.
- **Retry attribute** required: `[AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]`. Default Hangfire retry of 10 attempts with 30 minutes is too aggressive for capstone scope.
- **Logging** entry + result via `ILogger<TJob>`. Slow jobs log a warning at end if duration > threshold (similar to PerformanceBehavior).
- **CancellationToken**: Hangfire honors it. Always accept and propagate.
- **Return type `Task`** — `Task<T>` works but the result isn't accessible to anyone, so don't bother.

## Common Hangfire pitfalls (project-specific guidance)

| Pitfall | Why bad | Fix |
|---|---|---|
| Calling `BackgroundJob.Enqueue<TJob>(j => j.ExecuteAsync(...))` from inside a handler **before** `SaveChangesAsync` | If save fails, the job runs against a state that was rolled back | Enqueue from a domain event handler that fires AFTER `uow.SaveChangesAsync` (OVERVIEW.md §4.12). |
| Putting business invariants in the job | Job is a worker, not the source of truth | Keep invariants on the entity. Job orchestrates loading + calling entity methods. |
| `RecurringJob.AddOrUpdate` outside `Program.cs` | Drifts between environments, hard to audit | Centralize in `JobRegistration.RegisterAll(IRecurringJobManager, IConfiguration)`. |
| Time zone: writing `Cron.Daily(8)` and assuming Vietnam time | Hangfire uses UTC by default | Pass `TimeZoneInfo` or `RecurringJobOptions { TimeZone = ... }` explicitly. |
| Long-running job (> 5 min) without checkpointing | Worker restart loses progress; idempotency check is on the whole job | Split into smaller jobs that enqueue successors, or process in batches with a "last processed ID" cursor. |

## Self-check

- [ ] File in `Infrastructure/BackgroundJobs/`, class `sealed`, method `ExecuteAsync`
- [ ] BR ID(s) in XML doc on the class
- [ ] `[AutomaticRetry]` attribute set (3 attempts default)
- [ ] `CancellationToken` parameter and propagated
- [ ] Idempotency strategy clear in the code (filter, key, or CAS)
- [ ] Entry/exit logged
- [ ] DI dependencies are scoped (DbContext, UoW) — never singletons unless stateless
- [ ] Time zone explicit if scheduled by clock-time
- [ ] Registration shown to user with cron expression

## Templates

- `assets/recurring-job.cs.template` — schedule-driven job (most common)
- `assets/one-off-job.cs.template` — enqueued with args
- `assets/job-registration.cs.template` — `Program.cs` registration block + cron table for §4.11

## Example interaction

**User:** "Add the AutoCloseResolvedReportJob from BR-REP-016."

**Your response:**
1. Confirm schedule (hourly per OVERVIEW.md §4.11), idempotency = filter-based (Status=Resolved + ResolvedAt < now-7d), retry = default 3.
2. Generate `AutoCloseResolvedReportJob.cs` from `recurring-job.cs.template`.
3. Add the registration line to `JobRegistration.cs`: `RecurringJob.AddOrUpdate<AutoCloseResolvedReportJob>("auto-close-resolved", j => j.ExecuteAsync(default), Cron.Hourly());`
4. Tell the user: "Job will run every hour UTC. ResolvedAt cutoff is 7 days. Re-running is safe — `ExecuteUpdateAsync` only matches rows still in Resolved."
