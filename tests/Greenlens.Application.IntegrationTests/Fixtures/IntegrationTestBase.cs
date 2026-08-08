using Greenlens.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Greenlens.Application.IntegrationTests.Fixtures;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    protected ServiceProvider Services { get; private set; } = default!;
    protected TestCurrentUser CurrentUser { get; } = new();

    protected IntegrationTestBase(PostgresContainerFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();

        CurrentUser.UserId = Guid.NewGuid();
        CurrentUser.Email = "admin@test.local";
        CurrentUser.Role = "Admin";

        var services = new ServiceCollection();
        services.AddIntegrationTestServices(_fixture.ConnectionString, CurrentUser);
        Services = services.BuildServiceProvider();
    }

    public Task DisposeAsync()
    {
        Services.Dispose();
        return Task.CompletedTask;
    }

    protected ISender Mediator => Services.GetRequiredService<ISender>();

    protected async Task<T> WithDbAsync<T>(Func<IApplicationDbContext, Task<T>> action)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        return await action(db);
    }

    protected Task WithDbAsync(Func<IApplicationDbContext, Task> action) =>
        WithDbAsync(async db =>
        {
            await action(db);
            return true;
        });
}