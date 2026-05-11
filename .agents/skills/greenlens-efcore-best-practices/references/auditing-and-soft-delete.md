# EF Core 9 — Auditing and Soft Delete

These two concerns are project-wide and load-bearing for compliance:
- BR-ADM-010 — audit log for sensitive actions, retention 12 months
- BR-AUTH-022 — account soft delete (90-day grace before hard delete)
- BR-DAT-002 — data retention policy
- BR-DAT-003 — user data access rights (GDPR / Nghị định 13/2023/NĐ-CP)

Get them right once via base entities + interceptors; never sprinkle audit code in handlers.

## Base entities

Create these in `EcoReport.Domain.Common/`:

```csharp
public abstract class Entity
{
    public Guid Id { get; protected set; }

    private readonly List<IDomainEvent> _events = [];
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _events.AsReadOnly();
    protected void Raise(IDomainEvent e) => _events.Add(e);
    public void ClearDomainEvents() => _events.Clear();
}

public abstract class AuditableEntity : Entity
{
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}

public abstract class SoftDeletableEntity : AuditableEntity
{
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public bool IsDeleted => DeletedAt.HasValue;
}
```

## AuditingSaveChangesInterceptor

Populates `Created/UpdatedAt/By` automatically. Lives in `Infrastructure/Persistence/Interceptors/`.

```csharp
public sealed class AuditingSaveChangesInterceptor(
    ICurrentUser currentUser,
    IDateTimeProvider clock) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var ctx = eventData.Context;
        if (ctx is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        var now = clock.UtcNow;
        var userId = currentUser.UserId;     // null for system jobs

        foreach (var entry in ctx.ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = userId;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userId;
                    // Don't let callers tamper with Created* on update
                    entry.Property(e => e.CreatedAt).IsModified = false;
                    entry.Property(e => e.CreatedBy).IsModified = false;
                    break;
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
```

## SoftDeleteInterceptor

Converts `Remove()` into `DeletedAt = now` for `SoftDeletableEntity`.

```csharp
public sealed class SoftDeleteInterceptor(
    ICurrentUser currentUser,
    IDateTimeProvider clock) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var ctx = eventData.Context;
        if (ctx is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        foreach (var entry in ctx.ChangeTracker.Entries<SoftDeletableEntity>())
        {
            if (entry.State != EntityState.Deleted) continue;

            entry.State = EntityState.Modified;
            entry.Entity.DeletedAt = clock.UtcNow;
            entry.Entity.DeletedBy = currentUser.UserId;
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
```

Order matters: register `AuditingSaveChangesInterceptor` BEFORE `SoftDeleteInterceptor`, so an update-then-soft-delete still sets `UpdatedBy` correctly.

```csharp
options.AddInterceptors(
    auditingInterceptor,
    softDeleteInterceptor);
```

## Global query filter for soft delete

In `OnModelCreating`:

```csharp
foreach (var entityType in modelBuilder.Model.GetEntityTypes())
{
    if (typeof(SoftDeletableEntity).IsAssignableFrom(entityType.ClrType))
    {
        var parameter = Expression.Parameter(entityType.ClrType, "e");
        var prop = Expression.Property(parameter, nameof(SoftDeletableEntity.DeletedAt));
        var nullConst = Expression.Constant(null, typeof(DateTimeOffset?));
        var body = Expression.Equal(prop, nullConst);
        var lambda = Expression.Lambda(body, parameter);
        modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
    }
}
```

This makes `db.Reports` automatically exclude soft-deleted rows. To **include** them (admin recovery view, hard-delete job):

```csharp
db.Reports.IgnoreQueryFilters().Where(r => r.DeletedAt < cutoff)
```

## Audit log (BR-ADM-010)

The interceptor handles `Created/Updated/Deleted` columns on entities. The **audit log** is a separate concern — a dedicated `audit_logs` table that records:

| Column | Purpose |
|---|---|
| `id` | uuid |
| `actor_id` | user who performed the action (nullable for system) |
| `action` | enum: `RoleChanged`, `ReportDeleted`, `ConfigUpdated`, `LoginAttempt`, ... |
| `target_type` | `User`, `Report`, ... |
| `target_id` | uuid of the affected entity |
| `payload` | jsonb — before/after values, reason, etc. |
| `ip_address` | inet |
| `user_agent` | text |
| `at` | timestamptz |

Write via `IAuditLogger`:

```csharp
public interface IAuditLogger
{
    Task LogAsync(AuditAction action, string targetType, Guid targetId, object? payload, CancellationToken ct);
}
```

Implementation in Infrastructure inserts directly via Dapper or a dedicated `DbContext` (so audit logging survives even if the main transaction rolls back — depending on policy).

**Decision:** for sensitive actions, audit logs are written in their OWN transaction (committed independently). The trade-off is duplicate logs on retry vs. lost logs on rollback — choose duplicates and dedupe on `(actor_id, action, target_id, at)` if needed.

## What to audit

Don't audit everything — the table will explode. Audit:

- Auth: login success / failure, password change, role change (BR-AUTH-021, BR-AUTH-009)
- Account: creation, deactivation, deletion request, hard delete (BR-AUTH-022)
- Roles & permissions: any change to who-can-do-what (BR-ADM-001, BR-ADM-002)
- Config: pollution categories, gamification rules, notification templates (BR-ADM-003, BR-ADM-004, BR-ADM-005)
- Moderation: hide/delete report or comment (BR-ADM-006)
- Sensitive read access: officer pulling private user data, export of personal info (BR-OFF-022)

Don't audit normal reads, normal report submissions (those have their own history table), or list views.

## Retention

Background job runs weekly:

```csharp
var cutoff = clock.UtcNow.AddMonths(-12);                      // BR-DAT-002
await db.AuditLogs
    .Where(a => a.At < cutoff)
    .ExecuteDeleteAsync(ct);
```

For soft-deleted user accounts past 90 days (BR-AUTH-022):

```csharp
var cutoff = clock.UtcNow.AddDays(-90);
var ids = await db.Users
    .IgnoreQueryFilters()
    .Where(u => u.DeletedAt < cutoff)
    .Select(u => u.Id)
    .ToListAsync(ct);

// Anonymize their reports (keep the data, scrub the link)
await db.Reports
    .IgnoreQueryFilters()
    .Where(r => ids.Contains(r.ReporterId))
    .ExecuteUpdateAsync(s => s
        .SetProperty(r => r.ReporterId, (Guid?)null)
        .SetProperty(r => r.IsAnonymized, true), ct);

// Hard-delete the user rows
await db.Users
    .IgnoreQueryFilters()
    .Where(u => ids.Contains(u.Id))
    .ExecuteDeleteAsync(ct);
```

## Data export (BR-DAT-003)

Users can request their data. Build it from:
- `users.*` (their row)
- `reports.*` where `reporter_id = user.id`
- `comments.*` where `author_id = user.id`
- `audit_logs.*` where `actor_id = user.id` (last 12 months)

Stream to a ZIP with `IAsyncEnumerable<T>` so memory stays flat for big histories. Email a download link valid 24h; the file is deleted from S3 after.

## Things to NOT do

- Don't write `Created/UpdatedAt/By` from handlers. The interceptor owns these.
- Don't put audit log writes inline in handlers — use `IAuditLogger`. Centralized, testable, swappable.
- Don't use `Remove()` on a soft-deletable entity expecting a hard delete. The interceptor will redirect. For genuine hard delete, use `IgnoreQueryFilters` + `ExecuteDeleteAsync` in a maintenance job.
- Don't expose `DeletedAt` on public DTOs.
