using Greenlens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

namespace Greenlens.Application.IntegrationTests.Fixtures;

public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgis/postgis:16-3.4")
        .WithDatabase("greenlens_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    private Respawner? _respawner;

    public async Task InitializeAsync()
    {
        await _container.StartAsync().ConfigureAwait(false);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString, o => o.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
            .UseSnakeCaseNamingConvention()
            .Options;

        await using (var ctx = new ApplicationDbContext(options))
        {
            await ctx.Database.MigrateAsync().ConfigureAwait(false);
        }

        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync().ConfigureAwait(false);

        _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = ["__EFMigrationsHistory"],
        }).ConfigureAwait(false);
    }

    public async Task ResetAsync()
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync().ConfigureAwait(false);
        await _respawner!.ResetAsync(conn).ConfigureAwait(false);
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
