namespace Greenlens.Infrastructure.Persistence.Seeders.Location;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Public entry point để Api/Program.cs gọi seeder mà KHÔNG phải tham chiếu trực tiếp
/// <see cref="ApplicationDbContext"/> (DbContext là internal).
/// </summary>
public static class LocationSeederRunner
{
    /// <summary>
    /// Chạy seed cho các bảng location (regions, units, provinces, wards, boundary URLs).
    /// Idempotent — an toàn để gọi mọi lần startup.
    /// </summary>
    /// <param name="services">Scoped service provider từ <c>app.Services.CreateScope()</c>.</param>
    public static async Task SeedLocationAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(LocationSeeder).FullName!);

        // EnsureCreated/migrations đã chạy ở Program.cs trước khi gọi đây.
        // Seeder chỉ check AnyAsync() và bulk insert nếu rỗng.
        await LocationSeeder.SeedAsync(db, logger, ct);
    }
}
