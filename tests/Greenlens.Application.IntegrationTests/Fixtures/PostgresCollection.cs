namespace Greenlens.Application.IntegrationTests.Fixtures;

[CollectionDefinition("Postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresContainerFixture>;
