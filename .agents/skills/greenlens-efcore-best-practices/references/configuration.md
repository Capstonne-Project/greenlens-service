# EF Core 9 — Entity Configuration

## Where things live

```
src/EcoReport.Infrastructure/Persistence/
├── ApplicationDbContext.cs
├── Configurations/
│   ├── ReportConfiguration.cs
│   ├── UserConfiguration.cs
│   └── ...
└── Interceptors/
    ├── AuditingSaveChangesInterceptor.cs
    └── SoftDeleteInterceptor.cs
```

One `IEntityTypeConfiguration<T>` per entity, one file each. Apply them in `OnModelCreating` with:

```csharp
modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
```

## Configuration skeleton

```csharp
public sealed class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> b)
    {
        b.ToTable("reports");

        b.HasKey(r => r.Id);
        b.Property(r => r.Id)
            .ValueGeneratedNever();                 // app generates Guid v7

        // Strings — always set MaxLength
        b.Property(r => r.Title).HasMaxLength(200).IsRequired();
        b.Property(r => r.Description).HasMaxLength(1000);

        // Enums as strings (readable in DB dumps; index-friendly)
        b.Property(r => r.Status).HasConversion<string>().HasMaxLength(32);
        b.Property(r => r.Category).HasConversion<string>().HasMaxLength(32);
        b.Property(r => r.Severity).HasConversion<string>().HasMaxLength(16);

        // Timestamps — DateTimeOffset for SLA-sensitive fields
        b.Property(r => r.CreatedAt).IsRequired();
        b.Property(r => r.VerifiedAt);

        // PostGIS Point — see geo-postgis.md for the index
        b.Property(r => r.Location)
            .HasColumnType("geography (Point, 4326)")
            .IsRequired();

        // Owned types — value objects without their own table
        b.OwnsOne(r => r.ReporterContact, oc =>
        {
            oc.Property(c => c.Email).HasMaxLength(254).HasColumnName("reporter_email");
            oc.Property(c => c.Phone).HasMaxLength(20).HasColumnName("reporter_phone");
        });

        // Relationships
        b.HasMany(r => r.Media)
            .WithOne(m => m.Report)
            .HasForeignKey(m => m.ReportId)
            .OnDelete(DeleteBehavior.Cascade);      // media follows report lifetime

        b.HasOne(r => r.AssignedTeam)
            .WithMany()
            .HasForeignKey(r => r.AssignedTeamId)
            .OnDelete(DeleteBehavior.Restrict);     // never cascade-delete a team

        // Indexes
        b.HasIndex(r => new { r.Status, r.CreatedAt });
        b.HasIndex(r => r.ReporterId);
        b.HasIndex(r => r.AssignedTeamId);
        // GIST on Location is added in a migration with raw SQL; see migrations.md.
    }
}
```

## Decisions to know

### Primary keys: `Guid` v7
- Use `Guid.CreateVersion7()` (.NET 9) — sequential, plays well with PostgreSQL's `uuid` index B-trees.
- Do NOT let the DB generate (`ValueGeneratedOnAdd` with `gen_random_uuid()`); generating in app code lets you set IDs before persistence (useful for outbox events).

### Naming convention
- `EFCore.NamingConventions` package + `optionsBuilder.UseSnakeCaseNamingConvention()` in `DependencyInjection.cs` translates `CreatedAt` → `created_at`.
- Override only when names collide with reserved words (e.g. `user` → use `users` table, EF handles plural-singular automatically with the convention).

### Required vs nullable
- Drive nullability from the C# type (`string` vs `string?`, `int` vs `int?`).
- Don't double-declare with `.IsRequired()` unless you're forcing required on a nullable type (rare).

### `MaxLength` defaults
- Always specify. Email = 254 (RFC 5321), Phone = 20, Names = 200, free text = 500–1000, descriptions = up to 4000. Anything bigger → `text` column (`b.Property(...).HasColumnType("text")`).

### Decimal precision
- Money / scores / coordinates that aren't `Point`: `.HasPrecision(18, 2)`. Anything where rounding matters: explicit precision.

### Owned types vs separate tables
- **Owned**: small value object that's part of the parent's identity (Address, Contact, Money). One row per parent.
- **Separate table**: anything with its own lifecycle, multiple-per-parent, or referenced by ID elsewhere.

### Delete behavior
- `Cascade`: child rows MUST go when parent goes (Report → Media, Report → Comments).
- `Restrict`: parent has business meaning, deletion is rare; force the caller to clean children first (Report → AssignedTeam — never delete a team because of a report).
- `SetNull`: only when the FK column is nullable AND the relationship makes sense without the parent (rare in this project).
- **Never** use `ClientCascade` — it does the cascade in app memory, which is silently slow.

### Indexes — what to add
- Foreign keys: PostgreSQL does NOT auto-index FKs. Add an index for every FK you'll query/join on.
- Filter columns used in `WHERE`: `Status`, `Category`, `DistrictCode`, `IsAnonymous`.
- Sort columns paired with filters: composite `(Status, CreatedAt)` for the officer queue.
- Unique constraints: `User.Email`, `User.PhoneNumber` (BR-AUTH-002, BR-AUTH-004) — `b.HasIndex(u => u.Email).IsUnique();`
- Geo: GIST on `Report.Location` (see `geo-postgis.md`).

### Concurrency
- Add `xmin` (PostgreSQL system column) for optimistic concurrency on entities that admins or officers edit:
  ```csharp
  b.UseXminAsConcurrencyToken();
  ```
- For status transitions, the state machine in the entity already prevents double-transitions, but `xmin` catches racing officers.

## Things to NOT do

- Don't put configuration in `OnModelCreating` directly. One file per entity.
- Don't use Data Annotations (`[Required]`, `[MaxLength]`) on entities. The Domain project doesn't reference EF, so annotations there are noise; configuration belongs in Infrastructure.
- Don't expose `DbSet<T>` on `IApplicationDbContext` for entities that should never be queried directly (e.g. junction tables, history snapshots). Keep the interface surface narrow.
