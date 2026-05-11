# Vietnamese Location Seed — Integration Guide

Bộ seeder cho dữ liệu hành chính Việt Nam (sau cải cách 2025): 8 vùng + 5 loại đơn vị + 34 tỉnh/thành + ~3300 phường/xã + boundary URLs.

Adapt từ project `Verendar.Location` để phù hợp **Greenlens Clean Architecture** theo `CLAUDE.md` v1.2.

---

## Cấu trúc file đã tạo

```
src/
├── Greenlens.Domain/
│   └── Entities/Location/
│       ├── AdministrativeRegion.cs       # Aggregate root, factory Seed()
│       ├── AdministrativeUnit.cs
│       ├── Province.cs                   # PK = Code (string, 2 chars)
│       └── Ward.cs                       # PK = Code (string, 5 chars)
│
├── Greenlens.Application/
│   └── Common/Interfaces/Persistence/
│       ├── IAdministrativeCatalogRepositories.cs   # 2 interfaces, body rỗng
│       ├── IProvinceRepository.cs
│       └── IWardRepository.cs
│
├── Greenlens.Infrastructure/
│   └── Persistence/
│       ├── Configurations/Location/
│       │   ├── AdministrativeRegionConfiguration.cs
│       │   ├── AdministrativeUnitConfiguration.cs
│       │   ├── ProvinceConfiguration.cs
│       │   └── WardConfiguration.cs
│       ├── Repositories/Location/
│       │   ├── AdministrativeCatalogRepositories.cs   # 2 internal sealed classes
│       │   ├── ProvinceRepository.cs
│       │   └── WardRepository.cs
│       └── Seeders/Location/
│           ├── seed_data.sql                 # ⚠️ Embedded resource
│           ├── ProvinceRegionMap.cs          # province code → regionId
│           ├── ProvinceBoundaryUrls.cs       # province code → CDN URL
│           ├── LocationSeeder.cs             # internal, parse + bulk insert
│           └── LocationSeederRunner.cs       # PUBLIC entry point
```

---

## Checklist tích hợp (5 bước)

### 1. Mark `seed_data.sql` là embedded resource

Trong `Greenlens.Infrastructure.csproj` thêm:

```xml
<ItemGroup>
  <EmbeddedResource Include="Persistence/Seeders/Location/seed_data.sql" />
</ItemGroup>
```

Verify sau build:

```bash
dotnet build src/Greenlens.Infrastructure
# Mở Greenlens.Infrastructure.dll bằng dotPeek/ILSpy → tab Resources
# phải thấy "Greenlens.Infrastructure.Persistence.Seeders.Location.seed_data.sql"
```

Nếu đường dẫn resource sai, sửa `SeedSqlResourceName` trong `LocationSeeder.cs` cho khớp.

### 2. Đăng ký `DbSet<>` trong `ApplicationDbContext`

```csharp
internal sealed class ApplicationDbContext : DbContext
{
    // ... existing DbSets
    public DbSet<AdministrativeRegion> AdministrativeRegions => Set<AdministrativeRegion>();
    public DbSet<AdministrativeUnit>   AdministrativeUnits   => Set<AdministrativeUnit>();
    public DbSet<Province>             Provinces             => Set<Province>();
    public DbSet<Ward>                 Wards                 => Set<Ward>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ApplyConfigurationsFromAssembly sẽ pick lên 4 IEntityTypeConfiguration ở Configurations/Location/
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

### 3. Đăng ký repository trong DI (`Infrastructure/DependencyInjection.cs`)

Theo §4.12 CLAUDE.md — đăng ký từng repo, KHÔNG open generic:

```csharp
services.AddScoped<IAdministrativeRegionRepository, AdministrativeRegionRepository>();
services.AddScoped<IAdministrativeUnitRepository,   AdministrativeUnitRepository>();
services.AddScoped<IProvinceRepository,             ProvinceRepository>();
services.AddScoped<IWardRepository,                 WardRepository>();
```

### 4. Tạo migration

```bash
cd src/Greenlens.Api
dotnet ef migrations add AddLocationCatalog --project ../Greenlens.Infrastructure --startup-project .
```

Xem migration được generate — phải có 4 bảng `administrative_regions`, `administrative_units`, `provinces`, `wards` với snake_case columns. **Không** được có `migrationBuilder.InsertData(...)` (vì ta seed runtime, không HasData).

Apply lên DB dev:

```bash
dotnet ef database update --project ../Greenlens.Infrastructure --startup-project .
```

### 5. Gọi seeder ở `Program.cs` (chỉ chạy khi DB rỗng)

```csharp
// Greenlens.Api/Program.cs
using Greenlens.Infrastructure.Persistence.Seeders.Location;

var app = builder.Build();

// Migration + seed — chỉ ở dev/staging. Production dùng migration bundle riêng.
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.SeedLocationAsync(app.Lifetime.ApplicationStopping);
}

app.Run();
```

> **Lưu ý cho production:** seeder idempotent (check `AnyAsync()` từng bảng), nên có thể bật ở prod khi deploy lần đầu rồi tắt. Hoặc tạo CLI command riêng (vd. `dotnet run -- seed-location`) gọi `SeedLocationAsync()` rồi exit — sạch hơn.

---

## Verify sau khi chạy

```sql
-- PostgreSQL
SELECT COUNT(*) FROM administrative_regions;  -- 8
SELECT COUNT(*) FROM administrative_units;    -- 5
SELECT COUNT(*) FROM provinces;               -- 34
SELECT COUNT(*) FROM wards;                   -- ~3321
SELECT COUNT(*) FROM provinces WHERE boundary_url IS NOT NULL;  -- 34

-- Sanity check FK
SELECT p.code, p.name, r.name AS region, u.abbreviation AS type
FROM provinces p
JOIN administrative_regions r ON p.administrative_region_id = r.id
JOIN administrative_units u ON p.administrative_unit_id = u.id
ORDER BY p.code;
```

---

## Sử dụng trong Application handlers

Theo Clean Architecture — handler chỉ dùng repo interface:

```csharp
public sealed class GetProvincesQueryHandler(
    IProvinceRepository provinces) : IRequestHandler<GetProvincesQuery, Result<IReadOnlyList<ProvinceDto>>>
{
    public async Task<Result<IReadOnlyList<ProvinceDto>>> Handle(GetProvincesQuery req, CancellationToken ct)
    {
        var list = await provinces.GetAllForListAsync(ct);
        var dtos = list.Adapt<IReadOnlyList<ProvinceDto>>();   // Mapster
        return Result.Success(dtos);
    }
}
```

---

## Notes

- **Vì sao runtime seeder thay vì `HasData()`?** Dataset 3300+ rows → migration file sẽ `~5MB`, EF model snapshot phình to và build chậm. Runtime parse + bulk insert nhanh hơn.
- **Vì sao file SQL embedded thay vì C# data?** Nguồn gốc dataset là dump SQL từ [vietnamese-provinces-database](https://github.com/ThangLeQuoc/vietnamese-provinces-database). Khi VN có cải cách hành chính tiếp theo, chỉ việc thay `seed_data.sql` mới và xóa các bảng để re-seed.
- **Vì sao bulk UPDATE qua VALUES table cho boundary URLs?** 34 round-trip → 1 query. URLs là compile-time constants nên không có rủi ro injection.
- **`ProvinceRegionMap` mapping cứng:** seed_data.sql gốc không lưu region_id ở row provinces, nên phải map ngoài. Khi có cải cách (vd. tách tỉnh) cần update map này thủ công.
- **Boundary URLs trỏ tới CloudFront cũ:** khi go-live, copy 34 file GeoJSON sang Cloudflare R2 của Greenlens (xem §14.2 CLAUDE.md) và update `ProvinceBoundaryUrls.cs`.
