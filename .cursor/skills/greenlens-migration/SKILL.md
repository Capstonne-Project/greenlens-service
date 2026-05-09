---
name: greenlens-migration
description: >
  Guides creation of EF Core 9 migrations for the GreenLens PostgreSQL + PostGIS database. Enforces
  naming `yyyyMMddHHmm_VerbNoun`, snake_case columns, GIST indexes for geo, required indexes from
  CLAUDE.md §4.6, soft-delete columns, AuditableEntity columns, and reversibility. Use when adding
  or altering schema. Triggers: "migration", "add column", "alter table", "create index",
  "schema change", "ef migrations add".
---

# GreenLens — EF Core 9 Migration

## Naming

`yyyyMMddHHmm_VerbNoun`

- ✅ `202605091200_AddReportSlaColumns`
- ✅ `202605091215_CreateAuditLogsTable`
- ✅ `202605091230_AddIndexOnAuditLogActor`
- ❌ `Initial`, `Update1`, `Fix`, `Tmp`

Use UTC timestamp.

## Create command

```powershell
dotnet ef migrations add 202605091200_AddReportSlaColumns `
  --project src/Greenlens.Infrastructure `
  --startup-project src/Greenlens.Api `
  --context ApplicationDbContext
```

## Required for every entity

| Concern | Implementation |
|---------|---------------|
| Primary key | `Id uuid PRIMARY KEY` |
| Audit | `created_at timestamptz, created_by uuid, updated_at timestamptz, updated_by uuid` |
| Soft delete (User/Report/Comment) | `deleted_at timestamptz NULL` + global query filter in `OnModelCreating` |
| Concurrency | `xmin` (Postgres) or `row_version bytea` |

## PostGIS (Report.Location)

```csharp
builder.Property(r => r.Location)
    .HasColumnType("geometry(Point, 4326)")
    .IsRequired();

builder.HasIndex(r => r.Location)
    .HasMethod("GIST");
```

In the migration `Up`:

```csharp
migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS postgis;");
```

In the migration `Down`:
```csharp
// Do NOT drop the extension if other tables use it.
```

## Required indexes (CLAUDE.md §4.6)

| Table | Columns | Method | Purpose |
|-------|---------|--------|---------|
| `reports` | `(status, created_at)` | btree | filter+sort hot path |
| `reports` | `location` | GIST | geo queries (BR-MAP-*, BR-REP-030) |
| `users` | `email` | unique btree | login + uniqueness |
| `users` | `phone_number` | unique btree | uniqueness |
| `audit_logs` | `(actor_id, timestamp)` | btree | dashboard query |
| `notifications` | `(user_id, created_at)` | btree | user feed |

## Reversibility — every `Up` must have a `Down`

```csharp
protected override void Up(MigrationBuilder mb)
{
    mb.AddColumn<DateTime?>("verified_at", "reports", type: "timestamptz", nullable: true);
    mb.CreateIndex("ix_reports_status_created_at", "reports", new[] { "status", "created_at" });
}

protected override void Down(MigrationBuilder mb)
{
    mb.DropIndex("ix_reports_status_created_at", "reports");
    mb.DropColumn("verified_at", "reports");
}
```

## Migration safety checklist

- [ ] Self-contained — does NOT depend on a later migration.
- [ ] Reversible — `Down` undoes everything `Up` did.
- [ ] No `DROP TABLE` without team approval.
- [ ] No `ALTER COLUMN type` on a non-empty column without explicit `USING` cast.
- [ ] New `NOT NULL` columns have a default OR are added in two steps (add nullable → backfill → set NOT NULL).
- [ ] Concurrency: large index creation uses `CONCURRENTLY` in production via raw SQL.

## Production deployment

- **Dev:** auto-migrate at startup is OK.
- **Staging/Prod:** use `dotnet ef migrations bundle`:

  ```powershell
  dotnet ef migrations bundle `
    --project src/Greenlens.Infrastructure `
    --startup-project src/Greenlens.Api `
    --context ApplicationDbContext `
    --output ./deploy/migrate.exe
  ```

- Run the bundle as a separate deploy step, NOT inside the API process.

## Never

- ❌ Delete a merged migration. Always add a new revert migration.
- ❌ Use `Database.EnsureCreated()`. Always use migrations.
- ❌ Hand-edit a migration after it's been applied to staging/prod (only edit before merge).

## Hand-off

After creating the migration:
1. `dotnet ef database update` against a fresh local DB to verify `Up`.
2. `dotnet ef database update <previous-migration>` to verify `Down`.
3. Hand to `test` agent — integration tests must pass against the new schema.
