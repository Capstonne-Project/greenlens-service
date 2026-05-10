# EF Core 9 — Migrations

## Naming

Format: `yyyyMMddHHmm_VerbNoun` — verb in present tense, descriptive.

```
202605091200_AddReportSlaColumns
202605101415_RenameUserPhoneToPhoneNumber
202605120930_CreateAuditLogsTable
202605151000_BackfillReporterDistrict
```

The timestamp prefix is what `dotnet ef` uses for ordering. The body is for humans grepping git history.

## Generating

From the solution root:

```bash
dotnet ef migrations add AddReportSlaColumns \
  --project src/EcoReport.Infrastructure \
  --startup-project src/EcoReport.Api \
  --output-dir Persistence/Migrations
```

Always pass `--startup-project` — Infrastructure has no entry point.

## Reviewing — non-negotiable

Before committing a migration:

1. Read `<MigrationName>.cs`. Confirm `Up()` and `Down()` are mirror images.
2. Generate the SQL and read it:
   ```bash
   dotnet ef migrations script <PreviousMigration> <NewMigration> \
     --project src/EcoReport.Infrastructure \
     --startup-project src/EcoReport.Api \
     --output migration.sql
   ```
3. Look for these red flags:
   - **`DROP COLUMN`** — was the rename detected? If you renamed a property, EF often generates drop+add (data loss). Replace with `migrationBuilder.RenameColumn(...)` manually.
   - **`DROP TABLE`** — same story for `RenameTable`.
   - **`ALTER COLUMN ... NOT NULL`** without a `DEFAULT` clause — will fail on a non-empty table. Add `DefaultValue` or split into 3 migrations: add nullable → backfill → set not null.
   - **Index drops on production tables** — an `ALTER TABLE` re-creating an index can lock the table. Use `CREATE INDEX CONCURRENTLY` via `Sql()` for production.
4. If the migration touches data, use `migrationBuilder.Sql("...")` for the data step. EF won't generate it.

## Rename safely

EF Core's "rename detection" is brittle. When in doubt, take control:

```csharp
public partial class RenameUserPhoneToPhoneNumber : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "phone",
            table: "users",
            newName: "phone_number");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "phone_number",
            table: "users",
            newName: "phone");
    }
}
```

## Data migrations

Two patterns:

### A) Inline in the same migration as the schema change (small data)

```csharp
migrationBuilder.AddColumn<string>(
    name: "district_code",
    table: "reports",
    type: "varchar(16)",
    nullable: true);

// Backfill before NOT NULL
migrationBuilder.Sql("""
    UPDATE reports
    SET district_code = (
        SELECT code FROM districts d
        WHERE ST_Contains(d.geom, reports.location)
        LIMIT 1
    );
""");

migrationBuilder.AlterColumn<string>(
    name: "district_code",
    table: "reports",
    type: "varchar(16)",
    nullable: false,
    oldClrType: typeof(string),
    oldType: "varchar(16)",
    oldNullable: true);
```

### B) Separate migration runs out-of-band (large data)

For backfills > 100k rows, split:
1. Migration A: add nullable column.
2. Run a one-off background job (Hangfire) that backfills in batches.
3. Migration B: set NOT NULL.

This avoids long table locks during deployment.

## Production safety

- Never run migrations from the API at startup in production. CLAUDE.md §4.7 — production uses `dotnet ef migrations bundle` or a dedicated migration job.
- Keep migrations forward-only in production. If you need to revert, write a new migration that undoes — don't `dotnet ef database update <previous>` against prod.
- Long-running migrations (index creation, large backfills) run in a maintenance window or use `CONCURRENTLY` for indexes.

## PostGIS-specific

EF doesn't generate the GIST index for `Point` columns automatically. Add it manually:

```csharp
migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_reports_location_gist ON reports USING GIST (location);");
```

Mirror in `Down()` with `DROP INDEX IF EXISTS`.

For the PostGIS extension itself, ensure the Postgres image has it installed (`postgis/postgis` image), and create the extension once:

```csharp
migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS postgis;");
```

## Snapshot drift

If two developers run `migrations add` in parallel, the `ApplicationDbContextModelSnapshot.cs` will conflict. Resolution:

1. Delete one developer's migration files (keep the snapshot).
2. Re-run `dotnet ef migrations add` so the snapshot matches.
3. Don't try to hand-merge the snapshot — regenerate it.

## Bundles for deployment

```bash
dotnet ef migrations bundle \
  --project src/EcoReport.Infrastructure \
  --startup-project src/EcoReport.Api \
  --self-contained -r linux-x64 \
  -o efbundle
```

Ship `efbundle` to the deploy environment. Run with the connection string env var set. Idempotent — safe to re-run.
