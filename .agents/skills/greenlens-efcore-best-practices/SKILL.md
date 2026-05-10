---
name: greenlens-efcore-best-practices
description: Apply EF Core 9 best practices for the GreenLens .NET 9 backend (PostgreSQL + PostGIS, project SU26SE049). Use this skill whenever the user touches anything that interacts with EF Core — entity configuration, DbContext, migrations, queries, includes, projections, indexes, geo (PostGIS / NetTopologySuite), soft delete, audit fields, transactions, performance tuning, N+1 problems, AsNoTracking, ProjectToType, raw SQL, or anything in src/GreenLens.Infrastructure/Persistence/. Trigger this even on casual prompts like "this query is slow", "add a column to Reports", "why is my migration generating that", "should I use Include here", "fix the N+1", or any request to add/edit a class under Infrastructure/Persistence/. Loads the right reference doc on demand (configuration, queries, migrations, geo, performance) so context stays small.
---

# GreenLens EF Core 9 Best Practices

The GreenLens backend uses **EF Core 9** on **PostgreSQL 16 + PostGIS**, configured with:
- `Npgsql.EntityFrameworkCore.PostgreSQL`
- `Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite` (for `Point`, `Polygon`, `ST_DWithin`)
- `EFCore.NamingConventions` (snake_case mapping)

This skill is the **gatekeeper** for everything under `src/GreenLens.Infrastructure/Persistence/`. Default to the patterns below; only deviate with a written reason.

## Core principles (always apply)

1. **`IApplicationDbContext` is the boundary.** Application handlers depend on the interface in `GreenLens.Application/Common/Interfaces/`. Never reference concrete `ApplicationDbContext` from Application or Domain.
2. **Reads default to `AsNoTracking()` + projection.** Tracking exists for mutations; reads should never pay for it.
3. **Writes go through the entity, not the `DbSet`.** Domain methods enforce invariants; setting properties from a handler bypasses business rules. `Add` is fine for new aggregates.
4. **One `SaveChangesAsync` per request.** Use the `TransactionBehavior` MediatR pipeline; don't sprinkle saves through a handler.
5. **`CancellationToken` on every async DB call.** Without exception.
6. **Migrations are reviewed.** Never check in a migration without reading the generated SQL.
7. **No `FromSqlRaw` with interpolated user input.** Use `FromSqlInterpolated` (parameterized) or compose via LINQ + NetTopologySuite.

## Decide which reference to load

When the user's task is non-trivial, read **only** the reference(s) that match. Don't preload all of them — they're large and cost context.

| User is doing… | Read |
|---|---|
| Adding/changing an entity, key, index, relationship, or constraint | `references/configuration.md` |
| Writing a query, fixing N+1, deciding `Include` vs projection, paging | `references/queries-and-projection.md` |
| Adding/editing a migration, schema change, rename, data migration | `references/migrations.md` |
| Anything geographic — `Point`, distance, hotspot, radius search | `references/geo-postgis.md` |
| "Slow", "timeout", "spec p95", indexing, batching, bulk insert | `references/performance.md` |
| Soft delete, audit fields, `Created/UpdatedBy`, query filters | `references/auditing-and-soft-delete.md` |

When unsure, ask the user one clarifying question rather than loading three references.

## Quick rules cheat sheet

These cover ~80% of the questions you'll get without needing a reference file:

- **Always** configure entities via `IEntityTypeConfiguration<T>` in `Infrastructure/Persistence/Configurations/`. Never via Fluent API in `OnModelCreating` directly (it gets unmanageable past 5 entities).
- **Primary keys:** `Guid` (sequential via `Guid.CreateVersion7()` for ordered inserts on PostgreSQL — better than v4 for index locality).
- **`required` keyword on non-nullable navigation refs** in entities so the compiler enforces what EF expects.
- **Use `string?` vs `string`** to drive nullability in the schema. Don't override with `.IsRequired()` unless you must.
- **Include is a smell in lists.** Project to a DTO with Mapster `.ProjectToType<TDto>()`. `Include` is for the write side (loading an aggregate to mutate it).
- **No lazy loading.** Do not enable `UseLazyLoadingProxies()`. Every relationship load must be intentional.
- **No client evaluation.** Configure `optionsBuilder.ConfigureWarnings(w => w.Throw(RelationalEventId.MultipleCollectionIncludeWarning))` and treat client-eval warnings as errors.
- **Strings:** set explicit `MaxLength`. Unbounded `varchar` is a footgun on Postgres for indexing and replication.
- **Decimals:** always set precision: `.HasPrecision(18, 2)`. Money is `decimal`, never `double`.
- **Enums:** map to `string` columns via `.HasConversion<string>()` so DB dumps are readable. Indexes still work fine on text.
- **DateTime:** use `DateTimeOffset` for anything timezone-sensitive (audit, SLA, scheduled jobs). Use `DateOnly` / `TimeOnly` for calendar-only fields.
- **Booleans defaulting:** never rely on driver default; set `.HasDefaultValue(false)` explicitly so the migration is deterministic.
- **`SaveChangesAsync` is `await`ed.** No `.Result`, no `.Wait()`. The interceptors (auditing, outbox) depend on the async path.
- **Transactions:** wrap multi-aggregate writes via `TransactionBehavior` (MediatR) or `db.Database.BeginTransactionAsync(ct)` if you must control savepoints.

## Soft delete + audit (BR-AUTH-022, BR-ADM-010, BR-DAT-002)

These are project-specific and non-negotiable. Detail in `references/auditing-and-soft-delete.md`, but the headline rules:

- Entities subject to soft delete (`User`, `Report`, `Comment`) inherit `SoftDeletableEntity` and have a global query filter on `DeletedAt == null`.
- All entities inherit `AuditableEntity` (`CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`). The `AuditingSaveChangesInterceptor` populates them — handlers do not touch these fields.
- Audit log writes go to `audit_logs` table via `IAuditLogger`, retention 12 months (BR-DAT-002).

## When the user asks for something that breaks these rules

Push back with the specific rule. Examples:

> "Lazy loading would make `report.Comments` load behind your back — that hides N+1 problems. Let's project the comments into the DTO instead. Want me to show that pattern?"

> "Wrapping `SaveChangesAsync` in a `try/catch` here would swallow the audit interceptor's failure. Bubble it up — `ExceptionHandlingMiddleware` will turn it into a 500 with a correlation ID."

If the user has a real reason to deviate (perf workaround, third-party constraint), document it with `// EF-Deviation: <reason>` near the code so reviewers see it.

## Self-check before handing back any EF code

- [ ] Entity config in its own `IEntityTypeConfiguration<T>` file
- [ ] Reads use `AsNoTracking()` (or projection, which is no-tracking by default)
- [ ] Writes mutate through entity methods, not by setting properties from outside
- [ ] No `Include` chains deeper than 2 levels — switch to projection at 3+
- [ ] All async DB calls take `CancellationToken`
- [ ] Migration was reviewed — no unintended drops, renames produce `Sql("ALTER ... RENAME ...")` (not drop+create)
- [ ] PII columns are marked for the audit interceptor (see auditing reference)
- [ ] If the change touches `Reports`, the GIST index on `Location` is preserved

## References (load on demand)

- `references/configuration.md` — entity / key / index / relationship setup
- `references/queries-and-projection.md` — read patterns, Mapster, paging, no-N+1 recipes
- `references/migrations.md` — naming, rollback, data migrations, prod safety
- `references/geo-postgis.md` — NetTopologySuite, distance, hotspots, GIST
- `references/performance.md` — diagnosing slow queries, batching, compiled queries
- `references/auditing-and-soft-delete.md` — interceptors, query filters, retention

## Templates

- `assets/entity-configuration.cs.template` — full `IEntityTypeConfiguration<T>` skeleton
- `assets/db-context.cs.template` — `ApplicationDbContext` + `IApplicationDbContext` pair, with interceptors wired
- `assets/auditing-interceptor.cs.template` — populates `CreatedAt/By` and `UpdatedAt/By`
- `assets/soft-delete-interceptor.cs.template` — converts `Remove()` into `DeletedAt = now`
